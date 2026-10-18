namespace Alforz.Web.Helpers;

/// <summary>
/// Textos de fecha y horario en español. Se arman a mano para no depender de la
/// cultura instalada en el servidor (el contenedor de Render puede no tener "es").
/// </summary>
public static class FormatoTurno
{
    private static readonly string[] Dias = ["domingo", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado"];

    private static readonly string[] Meses =
        ["enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"];

    /// <summary>Ej.: "lunes 20 de octubre de 2026".</summary>
    public static string Dia(DateOnly dia) =>
        $"{Dias[(int)dia.DayOfWeek]} {dia.Day} de {Meses[dia.Month - 1]} de {dia.Year}";

    /// <summary>Ej.: "lun 20/10".</summary>
    public static string DiaCorto(DateOnly dia) => $"{Dias[(int)dia.DayOfWeek][..3]} {dia:dd/MM}";

    /// <summary>Ej.: "10:00 a 12:00".</summary>
    public static string Horario(int horaInicio, int duracionHoras) =>
        $"{horaInicio:00}:00 a {horaInicio + duracionHoras:00}:00";

    public static string Fecha(DateOnly dia) => dia.ToString("yyyy-MM-dd");
}
