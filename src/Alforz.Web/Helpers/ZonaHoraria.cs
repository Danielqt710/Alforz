namespace Alforz.Web.Helpers;

public static class ZonaHoraria
{
    /// <summary>
    /// Busca la zona IANA (ej. "America/Argentina/Buenos_Aires"). Si el sistema no tiene datos de zonas,
    /// usa UTC-3 fijo: Argentina no tiene horario de verano.
    /// </summary>
    public static TimeZoneInfo Buscar(string id)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.CreateCustomTimeZone(id, TimeSpan.FromHours(-3), id, id);
        }
    }
}
