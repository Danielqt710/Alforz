using Alforz.Web.Models.Entities;

namespace Alforz.Web.Models.ViewModels;

public class LogsViewModel
{
    public IReadOnlyList<RegistroLog> Registros { get; set; } = [];

    /// <summary>Cuántos logs recientes se trajeron antes de filtrar.</summary>
    public int Revisados { get; set; }

    public string? Nivel { get; set; }
    public string? Buscar { get; set; }
    public int Cantidad { get; set; }
    public TimeZoneInfo Zona { get; set; } = TimeZoneInfo.Utc;
    public int DiasRetencion { get; set; }
    public bool GuardadoActivo { get; set; }

    public DateTime HoraLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zona);

    /// <summary>"Alforz.Web.Services.TurnoService" → "TurnoService".</summary>
    public static string CategoriaCorta(string categoria) =>
        categoria.Contains('.') ? categoria[(categoria.LastIndexOf('.') + 1)..] : categoria;
}
