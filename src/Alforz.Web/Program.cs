using Alforz.Web.Configuration;
using Alforz.Web.Helpers;

var builder = WebApplication.CreateBuilder(args);

// Configuración tipada (los secretos van en user-secrets o variables de entorno)
builder.Services.Configure<FirebaseSettings>(builder.Configuration.GetSection(FirebaseSettings.Seccion));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(EmailSettings.Seccion));
builder.Services.Configure<SeguridadSettings>(builder.Configuration.GetSection(SeguridadSettings.Seccion));

// Helpers
builder.Services.AddSingleton<PasswordHelper>();

// Pendiente del paso de login: autenticación por cookie, repositorios Firestore,
// servicio de correo y seed del primer Admin.

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
