using Google.Cloud.Firestore;

namespace Alforz.Web.Models.Entities;

/// <summary>
/// Un cambio de cantidad o precio de un robot (colección "movimientosStock").
/// Se guarda al agregar un robot y cada vez que se edita su cantidad o su precio.
/// </summary>
[FirestoreData]
public class MovimientoStock
{
    public const string TipoAlta = "Alta";
    public const string TipoCambio = "Cambio";

    [FirestoreDocumentId]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty]
    public string RobotId { get; set; } = string.Empty;

    /// <summary>Nombre del robot en ese momento, por si después lo renombran.</summary>
    [FirestoreProperty]
    public string RobotNombre { get; set; } = string.Empty;

    /// <summary>Nombre de usuario de quien hizo el cambio.</summary>
    [FirestoreProperty]
    public string Usuario { get; set; } = string.Empty;

    [FirestoreProperty]
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    /// <summary>"Alta" (robot nuevo) o "Cambio" (edición).</summary>
    [FirestoreProperty]
    public string Tipo { get; set; } = TipoCambio;

    /// <summary>En un alta no hay valor anterior: queda en 0 y no se muestra.</summary>
    [FirestoreProperty]
    public int CantidadAnterior { get; set; }

    [FirestoreProperty]
    public int CantidadNueva { get; set; }

    [FirestoreProperty]
    public double PrecioAnterior { get; set; }

    [FirestoreProperty]
    public double PrecioNuevo { get; set; }

    public bool EsAlta => Tipo == TipoAlta;
    public bool CambioCantidad => !EsAlta && CantidadAnterior != CantidadNueva;
    public bool CambioPrecio => !EsAlta && PrecioAnterior != PrecioNuevo;
    public int DiferenciaCantidad => CantidadNueva - CantidadAnterior;
}
