using System.Security.Claims;
using Alforz.Web.Helpers;
using Alforz.Web.Models;
using Alforz.Web.Models.ViewModels;
using Alforz.Web.Repositories.Interfaces;
using Alforz.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Alforz.Web.Controllers;

/// <summary>Agendar turnos (cualquier usuario logueado) y panel de turnos (solo Admin).</summary>
[Authorize]
public class TurnosController(ITurnoService turnos, IRobotRepository robots) : Controller
{
    private string UsuarioId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    [HttpGet]
    public async Task<IActionResult> Index(string? fecha = null)
    {
        var dia = DateOnly.TryParseExact(fecha, "yyyy-MM-dd", out var elegido)
                  && elegido >= turnos.Hoy && elegido <= turnos.UltimoDiaAgendable
            ? elegido
            : turnos.PrimerDiaDisponible();

        return View(new AgendarTurnoViewModel
        {
            Dia = dia,
            DiaMinimo = turnos.Hoy,
            DiaMaximo = turnos.UltimoDiaAgendable,
            EsDiaLaborable = turnos.EsDiaLaborable(dia),
            Horarios = await turnos.HorariosDelDiaAsync(dia),
            MisTurnos = await turnos.TurnosDeUsuarioAsync(UsuarioId),
            Progreso = await turnos.ProgresoPremioAsync(UsuarioId),
            Robots = (await robots.ListarAsync()).Where(r => r.Activo).ToList()
        });
    }

    [HttpPost]
    [EnableRateLimiting(LimitesCuenta.Correos)]
    public async Task<IActionResult> Reservar(string fecha, int hora, string? robotId, string? comentario)
    {
        if (!DateOnly.TryParseExact(fecha, "yyyy-MM-dd", out var dia))
            return RedirectToAction(nameof(Index));

        var resultado = await turnos.ReservarAsync(
            UsuarioId,
            User.Identity?.Name ?? string.Empty,
            User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            dia, hora, robotId ?? string.Empty, comentario);

        if (!resultado.Exito)
        {
            TempData["Aviso"] = resultado.Error;
            return RedirectToAction(nameof(Index), new { fecha });
        }

        var cuando = $"{FormatoTurno.Dia(dia)}, de {FormatoTurno.Horario(hora, turnos.DuracionHoras)}";
        TempData["Exito"] = resultado.CorreoEnviado
            ? $"¡Listo! Tu turno quedó agendado para el {cuando}. Te enviamos la confirmación por correo."
            : $"Tu turno quedó agendado para el {cuando}, pero no pudimos enviarte el correo de confirmación.";
        return RedirectToAction(nameof(Index), new { fecha });
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Panel(FiltroTurnos filtro = FiltroTurnos.Proximos)
    {
        var lista = await turnos.ListarAsync(filtro);
        return View(new PanelTurnosViewModel
        {
            Filtro = filtro,
            Turnos = lista,
            Pasados = lista.Where(turnos.YaPaso).Select(t => t.Id).ToHashSet()
        });
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Cancelar(string id, FiltroTurnos filtro = FiltroTurnos.Proximos)
    {
        var resultado = await turnos.CancelarAsync(id, User.Identity?.Name ?? "Admin");
        if (!resultado.Exito)
            TempData["Aviso"] = resultado.Error;
        else
        {
            var turno = resultado.Turno!;
            TempData["Exito"] = $"Turno de {turno.NombreUsuario} del {FormatoTurno.DiaCorto(turno.Dia)} cancelado." +
                (resultado.CorreoEnviado ? " Le avisamos por correo." : "");
        }

        return RedirectToAction(nameof(Panel), new { filtro });
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> MarcarAsistencia(string id, bool asistio, FiltroTurnos filtro = FiltroTurnos.Pasados)
    {
        var resultado = await turnos.MarcarAsistenciaAsync(id, asistio, User.Identity?.Name ?? "Admin");
        if (!resultado.Exito)
            TempData["Aviso"] = resultado.Error;
        else
        {
            var turno = resultado.Turno!;
            TempData["Exito"] = $"{turno.NombreUsuario} ({FormatoTurno.DiaCorto(turno.Dia)}): " +
                (asistio ? "marcado como que asistió." : "marcado como que no asistió.") +
                (resultado.PremioCompletado
                    ? " 🎁 ¡Completó su camino de premios!" +
                      (resultado.CorreoEnviado ? " Le mandamos un correo de felicitación." : " No pudimos mandarle el correo de felicitación.")
                    : "");
        }

        return RedirectToAction(nameof(Panel), new { filtro });
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Premios() => View(await turnos.ListarPremiosAsync());

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> EntregarPremio(string usuarioId)
    {
        var resultado = await turnos.EntregarPremioAsync(usuarioId, User.Identity?.Name ?? "Admin");
        if (resultado.Exito)
            TempData["Exito"] = "Premio marcado como entregado. 🎁";
        else
            TempData["Aviso"] = resultado.Error;

        return RedirectToAction(nameof(Premios));
    }
}
