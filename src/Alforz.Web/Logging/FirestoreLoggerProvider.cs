using System.Threading.Channels;
using Alforz.Web.Models.Entities;

namespace Alforz.Web.Logging;

/// <summary>
/// Cola en memoria entre los loggers (que no pueden esperar a Firestore) y
/// <see cref="EscritorLogsFirestore"/>, que los guarda por lotes en segundo plano.
/// Si se llena (Firestore caído mucho tiempo), se descartan los más viejos.
/// </summary>
public class ColaLogs
{
    private readonly Channel<RegistroLog> _canal = Channel.CreateBounded<RegistroLog>(
        new BoundedChannelOptions(5000) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    public ChannelReader<RegistroLog> Lector => _canal.Reader;

    public void Agregar(RegistroLog registro) => _canal.Writer.TryWrite(registro);
}

/// <summary>
/// Manda a Firestore los logs propios de la app (Information o más) y los del framework solo si son
/// Warning o más. Los de Google/gRPC se excluyen: si Firestore falla, loguear ese fallo en Firestore haría un bucle.
/// </summary>
[ProviderAlias("Firestore")]
public class FirestoreLoggerProvider(ColaLogs cola, IWebHostEnvironment entorno) : ILoggerProvider
{
    private const int LargoMaxMensaje = 4000;

    public ILogger CreateLogger(string categoria) => new FirestoreLogger(categoria, this);

    public void Dispose() { }

    private bool DebeGuardar(string categoria, LogLevel nivel)
    {
        if (nivel == LogLevel.None || categoria.StartsWith("Google", StringComparison.Ordinal) ||
            categoria.StartsWith("Grpc", StringComparison.Ordinal))
            return false;

        return categoria.StartsWith("Alforz", StringComparison.Ordinal) ? nivel >= LogLevel.Information : nivel >= LogLevel.Warning;
    }

    private void Guardar(string categoria, LogLevel nivel, string mensaje, Exception? excepcion) =>
        cola.Agregar(new RegistroLog
        {
            Fecha = DateTime.UtcNow,
            Nivel = nivel.ToString(),
            Categoria = categoria,
            Mensaje = Recortar(mensaje),
            Excepcion = excepcion is null ? string.Empty : Recortar(excepcion.ToString()),
            Origen = entorno.EnvironmentName
        });

    private static string Recortar(string texto) => texto.Length <= LargoMaxMensaje ? texto : texto[..LargoMaxMensaje] + "…";

    private sealed class FirestoreLogger(string categoria, FirestoreLoggerProvider proveedor) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel nivel) => proveedor.DebeGuardar(categoria, nivel);

        public void Log<TState>(LogLevel nivel, EventId eventId, TState state, Exception? excepcion,
            Func<TState, Exception?, string> formatear)
        {
            if (IsEnabled(nivel))
                proveedor.Guardar(categoria, nivel, formatear(state, excepcion), excepcion);
        }
    }
}
