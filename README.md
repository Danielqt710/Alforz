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
- Cada 5 min la cookie se revisa contra Firestore: si desactivan al usuario o cambia su clave (sello de seguridad), se le cierra la sesión, y si le cambian el rol, la cookie se actualiza.
- Todos los POST exigen token antiforgery.
- Límite por IP: 10 intentos cada 5 min para ingresar y validar códigos; 5 cada 15 min para lo que manda correos.
- Las claves que firman las cookies se guardan en la colección `dataProtectionKeys`, así un deploy no cierra las sesiones.

Para restringir una página por rol:

```csharp
[Authorize]                              // cualquier usuario logueado
[Authorize(Roles = Roles.PuedenCrear)]   // Admin o Coordinador
[Authorize(Roles = Roles.Admin)]         // solo Admin
```

## Turnos

- `/Turnos`: cualquier usuario logueado elige día, horario (turnos de 2 h, de 8 a 18, lunes a viernes)
  y qué robot quiere ver con su precio (se guarda el precio del momento en el turno)
  y recibe la confirmación por correo. Los horarios tomados aparecen como **Ocupado**.
- `/Turnos/Panel`: solo Admin. Ve todos los turnos (próximos, pasados, cancelados) y puede cancelar
  cualquiera; a quien lo agendó le llega un aviso por correo. En los turnos que ya empezaron marca
  si la persona **asistió** o **no asistió** (se puede corregir).
- Dos personas no pueden quedarse con el mismo horario: la reserva se hace en una transacción de Firestore.
- Camino de premios: en `/Turnos` cada usuario ve 12 círculos; cada turno marcado como "Asistió" llena uno
  y al completar los 12 gana un premio sorpresa (luego el camino vuelve a empezar). Al completarlo le llega
  un correo de felicitación (una sola vez por premio).
- `/Turnos/Premios`: solo Admin. Avance de cada usuario y premios por entregar; "Entregar" lo registra.
- Cada usuario puede tener hasta 3 turnos próximos. Todo se ajusta en la sección `Turnos` de `appsettings.json`
  (los días de atención, en `Turnos:DiasLaborables`, con 0 = domingo ... 6 = sábado).

## Inventario (robots)

- `/Inventario`: cualquier usuario logueado ve los robots con su foto, precio y cuántos hay.
- **Admin y Coordinador** agregan robots y editan nombre, descripción, precio, cantidad y foto.
  Ven también los ocultos. **Solo Admin** los oculta o muestra (los ocultos no los ven las visitas
  ni aparecen al agendar). `/Robots` redirige acá.
- Fotos: el navegador las achica antes de subirlas (lado mayor 1200 px, JPEG, ~200 KB). Se guardan en
  Firestore, en la colección `imagenesRobots` (un documento por robot, con el mismo id), porque un
  documento admite hasta 1 MB y así no hace falta Firebase Storage (que pide el plan pago).
  El servidor acepta solo JPG, PNG o WebP de hasta 900 KB y revisa el contenido real del archivo.
- La foto se sirve en `/Inventario/Imagen/{id}?v=...`; el navegador la guarda un mes y, como cada foto
  nueva cambia la `v`, nunca se ve una vieja.
- **Historial** (`/Inventario/Historial/{id}`, Admin y Coordinador): cada robot guarda quién cambió su cantidad
  o su precio y cuándo, con el valor anterior y el nuevo, además del alta. Se guarda en la colección
  `movimientosStock`; cambiar solo el nombre, la descripción o la foto no deja registro.
  Los robots que ya existían empiezan con el historial vacío.
- Si la colección `robots` está vacía, al arrancar se cargan las 4 series: Hogar USD 5, Logística USD 8,
  Médica USD 11 e Industrial USD 14 (de 5 dólares, subiendo 3 por categoría).

## Logs

- `/Logs`: solo Admin. Últimos logs con búsqueda y filtro (errores, avisos, información).
- Se guardan en la colección `logs`: los de la app desde Information y los del framework desde Warning.
  Se borran solos a los 30 días (`Logs:DiasRetencion`). En Development no se guardan (`Logs:GuardarEnFirestore`).

## Publicar en Render

El `Dockerfile` de la raíz arma la app. En Render: **New → Web Service**, conectar este repo,
runtime **Docker**, plan **Free**. Cada push a `main` se publica solo.

Variables de entorno (el `__` reemplaza a `:`):

| Variable | Valor |
| --- | --- |
| `Firebase__ProjectId` | id del proyecto |
| `Firebase__CredentialsPath` | `/etc/secrets/alforz-firebase.json` |
| `Email__SmtpUser` | login SMTP de Brevo |
| `Email__SmtpPassword` | clave SMTP de Brevo |
| `Email__FromAddress` | remitente verificado en Brevo |
| `Email__SmtpPort` | `2525` (Render gratis bloquea el 587) |

Y un **Secret File** llamado `alforz-firebase.json` con el contenido del JSON de la cuenta de servicio.

En el plan gratis el servicio se duerme tras 15 min sin visitas (la primera carga tarda ~1 min)
y cada nueva publicación cierra las sesiones abiertas.

## Correr

```bash
dotnet run --project src/Alforz.Web
```
