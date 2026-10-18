using Alforz.Web.Models.Entities;
using Alforz.Web.Services.Interfaces;

namespace Alforz.Web.Models.ViewModels;

public class AgendarTurnoViewModel
{
    public DateOnly Dia { get; set; }
    public DateOnly DiaMinimo { get; set; }
    public DateOnly DiaMaximo { get; set; }
    public bool EsDiaLaborable { get; set; }
    public IReadOnlyList<Horario> Horarios { get; set; } = [];
    public IReadOnlyList<Turno> MisTurnos { get; set; } = [];

    public DateOnly? DiaAnterior => Dia > DiaMinimo ? Dia.AddDays(-1) : null;
    public DateOnly? DiaSiguiente => Dia < DiaMaximo ? Dia.AddDays(1) : null;
}

public class PanelTurnosViewModel
{
    public FiltroTurnos Filtro { get; set; }
    public IReadOnlyList<Turno> Turnos { get; set; } = [];

    /// <summary>Ids de los turnos activos que ya pasaron (no se muestran como cancelables).</summary>
    public IReadOnlySet<string> Pasados { get; set; } = new HashSet<string>();
}
