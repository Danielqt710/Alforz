using Google.Cloud.Firestore;

namespace Alforz.Web.Models.Entities;

public static class EstadoTurno
{
    public const string Activo = "Activo";
    public const string Cancelado = "Cancelado";
}

/// <summary>
/// Turno guardado en la colección "turnos". La fecha y la hora se guardan "de pared"
/// (hora local del negocio) para no depender de zonas horarias al mostrarlas.
/// </summary>
[FirestoreData]
public class Turno
{
    [FirestoreDocumentId]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty]
    public string UsuarioId { get; set; } = string.Empty;

    [FirestoreProperty]
    public string NombreUsuario { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Correo { get; set; } = string.Empty;

    /// <summary>Día del turno en formato yyyy-MM-dd.</summary>
    [FirestoreProperty]
    public string Fecha { get; set; } = string.Empty;

    /// <summary>Hora de inicio (0-23). El fin es HoraInicio + duración configurada.</summary>
    [FirestoreProperty]
    public int HoraInicio { get; set; }

    [FirestoreProperty]
    public int DuracionHoras { get; set; }

    [FirestoreProperty]
    public string Comentario { get; set; } = string.Empty;

    /// <summary>Robot que la persona quiere ver. Nombre y precio se copian al agendar, por si después cambian.</summary>
    [FirestoreProperty]
    public string RobotId { get; set; } = string.Empty;

    [FirestoreProperty]
    public string RobotNombre { get; set; } = string.Empty;

    [FirestoreProperty]
    public double RobotPrecio { get; set; }

    public bool TieneRobot => !string.IsNullOrEmpty(RobotNombre);

    /// <summary>Uno de los valores de <see cref="EstadoTurno"/>.</summary>
    [FirestoreProperty]
    public string Estado { get; set; } = EstadoTurno.Activo;

    [FirestoreProperty]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    [FirestoreProperty]
    public DateTime? FechaCancelacion { get; set; }

    [FirestoreProperty]
    public string CanceladoPor { get; set; } = string.Empty;

    /// <summary>Si la persona vino al turno: null = todavía sin marcar. Lo marca el Admin.</summary>
    [FirestoreProperty]
    public bool? Asistio { get; set; }

    [FirestoreProperty]
    public string AsistenciaMarcadaPor { get; set; } = string.Empty;

    [FirestoreProperty]
    public DateTime? FechaAsistencia { get; set; }

    public bool EstaActivo => Estado == EstadoTurno.Activo;

    public DateOnly Dia => DateOnly.ParseExact(Fecha, "yyyy-MM-dd");

    /// <summary>Momento de inicio en hora local del negocio, para ordenar y comparar.</summary>
    public DateTime Inicio => Dia.ToDateTime(new TimeOnly(HoraInicio, 0));
}
