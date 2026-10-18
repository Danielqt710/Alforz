using Alforz.Web.Configuration;
using Alforz.Web.Helpers;
using Alforz.Web.Logging;
using Alforz.Web.Models;
using Alforz.Web.Models.Entities;
using Alforz.Web.Models.ViewModels;
using Alforz.Web.Repositories;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Alforz.Web.Controllers;

/// <summary>Últimos logs guardados en Firestore. Solo Admin.</summary>
[Authorize(Roles = Roles.Admin)]
public class LogsController(
    FirestoreProveedor firestore,
    IOptions<TurnosSettings> turnos,
    IOptions<LogsSettings> logs) : Controller
{
    private static readonly int[] CantidadesPermitidas = [100, 300, 1000];

    [HttpGet]
    public async Task<IActionResult> Index(string? nivel = null, string? buscar = null, int cantidad = 300)
    {
        if (!CantidadesPermitidas.Contains(cantidad))
            cantidad = 300;

        // Solo se ordena por fecha (índice simple, sin índices compuestos); el filtro se hace en memoria.
        var snapshot = await firestore.Db.Collection(EscritorLogsFirestore.Coleccion)
            .OrderByDescending(nameof(RegistroLog.Fecha))
            .Limit(cantidad)
            .GetSnapshotAsync();
        var registros = snapshot.Documents.Select(d => d.ConvertTo<RegistroLog>()).ToList();

        var filtrados = registros.Where(r => nivel switch
        {
            "errores" => r.Nivel is "Error" or "Critical",
            "avisos" => r.Nivel == "Warning",
            "info" => r.Nivel == "Information",
            _ => true
        });
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = buscar.Trim();
            filtrados = filtrados.Where(r =>
                r.Mensaje.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                r.Categoria.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                r.Excepcion.Contains(texto, StringComparison.OrdinalIgnoreCase));
        }

        return View(new LogsViewModel
        {
            Registros = filtrados.ToList(),
            Revisados = registros.Count,
            Nivel = nivel,
            Buscar = buscar,
            Cantidad = cantidad,
            Zona = ZonaHoraria.Buscar(turnos.Value.ZonaHoraria),
            DiasRetencion = logs.Value.DiasRetencion,
            GuardadoActivo = logs.Value.GuardarEnFirestore
        });
    }
}
