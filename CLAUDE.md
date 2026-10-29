# Alforz — guía para agentes

App web en ASP.NET Core MVC (.NET 10) con Firestore. Inventario de robots, turnos con premios,
usuarios con roles y logs. Se publica en Render con Docker. El README tiene el detalle de cada sección.

## Comandos

```bash
dotnet build src/Alforz.Web/Alforz.Web.csproj      # lo mismo que corre el CI en cada push
dotnet run --project src/Alforz.Web                # local (necesita user-secrets, ver README)
```

No hay tests todavía. Antes de dar algo por terminado, que compile sin warnings nuevos.

## Cómo está armado

- `Controllers/` → reciben el pedido, validan, llaman a servicios o repositorios y devuelven la vista.
- `Services/` → lógica de negocio (cuenta, turnos, correo). Interfaces en `Services/Interfaces/`.
- `Repositories/` → único lugar que habla con Firestore. Interfaces en `Repositories/Interfaces/`.
- `Models/Entities/` → documentos de Firestore (`[FirestoreData]`, `[FirestoreProperty]`).
- `Models/ViewModels/` → lo que reciben y muestran las vistas. Nunca pasar una entidad directo a un formulario.
- `Configuration/Settings.cs` → configuración tipada; cada sección con su `const string Seccion`.
- `Helpers/` → utilidades sin estado (formato, hash, imágenes).
- Todo se registra en `Program.cs`.

## Convenciones

- **Todo en español:** clases, métodos, variables, comentarios y textos de pantalla.
- Textos que ve el usuario: español neutro con "tú" ("Espera unos minutos y vuelve a probar").
- Métodos async terminan en `Async`. Constructores primarios para inyectar dependencias.
- Comentarios `/// <summary>` cortos que explican el porqué, no el qué.
- Logs estructurados con quién hizo qué: `logger.LogInformation("{Usuario} agregó el robot {RobotId}", ...)`.
- Permisos con `[Authorize]`, `[Authorize(Roles = Roles.PuedenCrear)]` o `[Authorize(Roles = Roles.Admin)]`.
  Usar siempre las constantes de `Roles`, nunca el texto suelto.
- Valores ajustables (horarios, límites) van en `appsettings.json` + su clase en `Settings.cs`, no hardcodeados.
- Los datos no se borran: se ocultan o desactivan (`Activo = false`).

## Estilo visual

- Solo la paleta de `wwwroot/css/site.css`: celeste, lavanda y menta + gris pizarra para texto.
  No agregar colores nuevos; las variantes salen de `color-mix` sobre esos.
- Bootstrap 5 + Bootstrap Icons. Tiene que verse bien en el celular.

## Reglas

- **Secretos nunca al repo** (credenciales de Firebase, SMTP, claves). Van en user-secrets o variables de entorno.
- Firestore plan gratis: nada de Firebase Storage; un documento pesa como máximo 1 MB.
- Todo POST lleva antiforgery (ya está como filtro global; no desactivarlo).
- Cambios grandes o que tocan seguridad, login o roles: explicar el plan antes de escribir código.
- Al agregar o cambiar una sección, actualizar el README.
- Trabajar en una rama y abrir pull request; no commitear directo a `main` (cada push a `main` publica en Render).
