using Alforz.Web.Configuration;
using Alforz.Web.Helpers;
using Alforz.Web.Repositories;
using Alforz.Web.Repositories.Interfaces;
using Alforz.Web.Services;
using Alforz.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// En Render (y otros hosts con Docker) el puerto llega en la variable PORT.
if (Environment.GetEnvironmentVariable("PORT") is { Length: > 0 } puerto)
    builder.WebHost.UseUrls($"http://0.0.0.0:{puerto}");

// Detrás del proxy del host la app recibe HTTP: estos encabezados le dicen que el visitante usa HTTPS,
// así funcionan la redirección a HTTPS y la cookie segura.
builder.Services.Configure<ForwardedHeadersOptions>(opciones =>
{
    opciones.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    opciones.KnownIPNetworks.Clear();
    opciones.KnownProxies.Clear();
});

// Configuración tipada (los secretos van en user-secrets o variables de entorno)
builder.Services.Configure<FirebaseSettings>(builder.Configuration.GetSection(FirebaseSettings.Seccion));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(EmailSettings.Seccion));
builder.Services.Configure<SeguridadSettings>(builder.Configuration.GetSection(SeguridadSettings.Seccion));
builder.Services.Configure<AdminInicialSettings>(builder.Configuration.GetSection(AdminInicialSettings.Seccion));
builder.Services.Configure<TurnosSettings>(builder.Configuration.GetSection(TurnosSettings.Seccion));

// Helpers, repositorios y servicios
builder.Services.AddSingleton<PasswordHelper>();
builder.Services.AddSingleton<FirestoreProveedor>();
builder.Services.AddSingleton<IUsuarioRepository, FirestoreUsuarioRepository>();
builder.Services.AddSingleton<ITokenRepository, FirestoreTokenRepository>();
builder.Services.AddTransient<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<ICuentaService, CuentaService>();
builder.Services.AddSingleton<ITurnoRepository, FirestoreTurnoRepository>();
builder.Services.AddScoped<ITurnoService, TurnoService>();
builder.Services.AddScoped<ValidadorCookie>();

// Claves que firman cookies y antiforgery: en Firestore, para que no se pierdan en cada deploy.
// Sin Firebase configurado quedan en disco, como antes.
builder.Services.AddDataProtection().SetApplicationName("Alforz");
builder.Services.AddOptions<KeyManagementOptions>()
    .Configure<FirestoreProveedor, IOptions<FirebaseSettings>>((opciones, firestore, firebase) =>
    {
        if (firebase.Value.EstaConfigurado)
            opciones.XmlRepository = new FirestoreXmlRepository(firestore);
    });

// Límite de pedidos por IP en las páginas de cuenta (frena fuerza bruta y abuso del cupo de correos).
builder.Services.AddRateLimiter(opciones =>
{
    opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opciones.OnRejected = async (contexto, cancelacion) =>
    {
        contexto.HttpContext.Response.ContentType = "text/html; charset=utf-8";
        await contexto.HttpContext.Response.WriteAsync(
            "<p style=\"font-family:sans-serif;padding:2rem\">Demasiados intentos seguidos. " +
            "Espera unos minutos y vuelve a probar. <a href=\"/\">Volver al inicio</a></p>", cancelacion);
    };

    opciones.AddPolicy(LimitesCuenta.Intentos, contexto => RateLimitPartition.GetFixedWindowLimiter(
        LimitesCuenta.ClaveIp(contexto),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(5) }));

    opciones.AddPolicy(LimitesCuenta.Correos, contexto => RateLimitPartition.GetFixedWindowLimiter(
        LimitesCuenta.ClaveIp(contexto),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(15) }));
});

// Autenticación por cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opciones =>
    {
        opciones.LoginPath = "/Cuenta/Ingresar";
        opciones.LogoutPath = "/Cuenta/Salir";
        opciones.AccessDeniedPath = "/Cuenta/AccesoDenegado";
        opciones.Cookie.Name = "alforz.auth";
        opciones.Cookie.HttpOnly = true;
        opciones.Cookie.SameSite = SameSiteMode.Lax;
        opciones.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        opciones.ExpireTimeSpan = TimeSpan.FromHours(8);
        opciones.SlidingExpiration = true;
        opciones.EventsType = typeof(ValidadorCookie);
    });
builder.Services.AddAuthorization();

builder.Services.AddControllersWithViews(opciones =>
{
    // Todo POST exige el token antiforgery (los formularios con asp-action lo agregan solos).
    opciones.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

var app = builder.Build();

try
{
    await SeedAdmin.EjecutarAsync(app.Services);
}
catch (Exception ex)
{
    // Un problema de conexión con Firestore no debe impedir que la app levante.
    app.Logger.LogError(ex, "No se pudo revisar/crear el Admin inicial en Firestore");
}

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
