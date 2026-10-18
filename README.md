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
```

## Correr

```bash
dotnet run --project src/Alforz.Web
```
