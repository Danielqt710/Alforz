using Alforz.Web.Configuration;
using Alforz.Web.Helpers;
using Alforz.Web.Repositories;
using Alforz.Web.Repositories.Interfaces;
using Alforz.Web.Services;
using Alforz.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Configuración tipada (los secretos van en user-secrets o variables de entorno)
builder.Services.Configure<FirebaseSettings>(builder.Configuration.GetSection(FirebaseSettings.Seccion));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(EmailSettings.Seccion));
builder.Services.Configure<SeguridadSettings>(builder.Configuration.GetSection(SeguridadSettings.Seccion));
builder.Services.Configure<AdminInicialSettings>(builder.Configuration.GetSection(AdminInicialSettings.Seccion));

// Helpers, repositorios y servicios
builder.Services.AddSingleton<PasswordHelper>();
builder.Services.AddSingleton<FirestoreProveedor>();
builder.Services.AddSingleton<IUsuarioRepository, FirestoreUsuarioRepository>();
builder.Services.AddSingleton<ITokenRepository, FirestoreTokenRepository>();
builder.Services.AddTransient<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<ICuentaService, CuentaService>();
builder.Services.AddScoped<ValidadorCookie>();

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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
