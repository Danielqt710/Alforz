# Alforz

Aplicación web en **ASP.NET Core MVC (.NET 10)** con **Firestore** como base de datos.

## Stack

- ASP.NET Core MVC, .NET 10
- Razor + Bootstrap 5 (paleta propia) + Bootstrap Icons
- Firestore (Firebase)
- Correos por SMTP con Brevo

## Roles

| Rol | Puede |
| --- | --- |
| Admin | Todo: usuarios, roles, configuración y contenido |
| Coordinador | Crear y editar contenido |
| Visita | Solo ver (rol de toda cuenta nueva) |

## Estructura

```
src/Alforz.Web/
├── Configuration/     Clases de configuración tipada (Firebase, Email, Seguridad)
├── Controllers/
├── Helpers/           PasswordHelper, TokenHelper
├── Models/
│   ├── Entities/      Usuario, TokenSeguridad (documentos de Firestore)
│   └── Roles.cs
├── Repositories/      Acceso a Firestore (interfaces + implementación)
├── Services/          Lógica de negocio y correo
├── Views/
└── wwwroot/
```

## Configuración local

Los secretos **nunca** van al repo. Se cargan con user-secrets:

```bash
cd src/Alforz.Web
dotnet user-secrets set "Firebase:ProjectId" "tu-project-id"
dotnet user-secrets set "Firebase:CredentialsPath" "C:/ruta/a/alforz-firebase.json"
dotnet user-secrets set "Email:SmtpUser" "tu-usuario-brevo"
dotnet user-secrets set "Email:SmtpPassword" "tu-clave-smtp-brevo"
dotnet user-secrets set "Email:FromAddress" "no-reply@tu-dominio.com"

# Primer Admin: se crea al arrancar si en Firestore no hay ningún Admin
dotnet user-secrets set "AdminInicial:NombreUsuario" "admin"
dotnet user-secrets set "AdminInicial:Correo" "tu-correo@dominio.com"
dotnet user-secrets set "AdminInicial:Clave" "una-clave-segura-123"
```

- Si falta `Firebase:ProjectId`, las páginas cargan pero ingresar o registrarse da error.
- Si faltan los datos SMTP, en Development los correos (con el código) se escriben en la consola en vez de enviarse.
- `FromAddress` tiene que ser un remitente verificado en Brevo.
- Una vez creado el Admin, `AdminInicial:Clave` se puede borrar de los secretos.

## Login

| Ruta | Qué hace |
| --- | --- |
| `/Cuenta/Registrar` | Crea una cuenta **Visita** y manda un código de 6 dígitos al correo |
| `/Cuenta/VerificarCorreo` | Confirma el correo con el código (hay que hacerlo antes de entrar) |
| `/Cuenta/Ingresar` | Entra con usuario **o** correo y clave (la sesión dura 8 h; con "Recordarme", 30 días aunque cierres el navegador) |
| `/Cuenta/OlvideClave` → `/Cuenta/RestablecerClave` | Recuperar la clave con un código por correo |
| `/Usuarios` | Solo Admin: cambiar roles y activar o desactivar cuentas |

Seguridad:

- Claves con hash PBKDF2. Los códigos se guardan hasheados, vencen a los 15 min y se aceptan hasta 5 intentos.
- La cuenta se bloquea 15 min después de 5 claves incorrectas seguidas.
- Cada 5 min la cookie se revisa contra Firestore: si desactivan al usuario, se le cierra la sesión, y si le cambian el rol, la cookie se actualiza.
- Todos los POST exigen token antiforgery.

Para restringir una página por rol:

```csharp
[Authorize]                              // cualquier usuario logueado
[Authorize(Roles = Roles.PuedenCrear)]   // Admin o Coordinador
[Authorize(Roles = Roles.Admin)]         // solo Admin
```

## Correr

```bash
dotnet run --project src/Alforz.Web
```
