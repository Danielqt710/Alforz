using Alforz.Web.Configuration;
using Alforz.Web.Models.Entities;
using Alforz.Web.Repositories;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Options;

namespace Alforz.Web.Logging;

/// <summary>
/// Saca los logs de <see cref="ColaLogs"/> y los guarda en Firestore de a lotes (hasta 100, cada 2 segundos).
/// Una vez por día borra los más viejos que <see cref="LogsSettings.DiasRetencion"/>.
/// Sus propios errores van a la consola, nunca al logger, para no entrar en bucle.
/// </summary>
public class EscritorLogsFirestore(
    ColaLogs cola,
    FirestoreProveedor firestore,
    IOptions<LogsSettings> opciones,
    IOptions<FirebaseSettings> firebase) : BackgroundService
{
    public const string Coleccion = "logs";

    private const int TamanoLote = 100;
    private static readonly TimeSpan Espera = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan IntervaloLimpieza = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken detener)
    {
        if (!opciones.Value.GuardarEnFirestore || !firebase.Value.EstaConfigurado)
        {
            // Igual hay que vaciar la cola para que no crezca.
            await foreach (var _ in cola.Lector.ReadAllAsync(detener)) { }
            return;
        }

        var proximaLimpieza = DateTime.UtcNow.AddMinutes(1);
        var lote = new List<RegistroLog>(TamanoLote);

        while (!detener.IsCancellationRequested)
        {
            try
            {
                using var espera = CancellationTokenSource.CreateLinkedTokenSource(detener);
                espera.CancelAfter(Espera);
                try
                {
                    while (lote.Count < TamanoLote && await cola.Lector.WaitToReadAsync(espera.Token))
                        while (lote.Count < TamanoLote && cola.Lector.TryRead(out var registro))
                            lote.Add(registro);
                }
                catch (OperationCanceledException) when (!detener.IsCancellationRequested)
                {
                    // Pasaron los 2 segundos: se guarda lo que haya.
                }

                if (lote.Count > 0)
                {
                    await GuardarAsync(lote);
                    lote.Clear();
                }

                if (DateTime.UtcNow >= proximaLimpieza)
                {
                    await BorrarViejosAsync(detener);
                    proximaLimpieza = DateTime.UtcNow.Add(IntervaloLimpieza);
                }
            }
            catch (OperationCanceledException) when (detener.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[EscritorLogsFirestore] No se pudieron guardar {lote.Count} logs: {ex.Message}");
                lote.Clear();
                await Task.Delay(TimeSpan.FromSeconds(10), detener).ContinueWith(_ => { });
            }
        }

        // Al apagar, guardar lo que quedó en cola.
        while (cola.Lector.TryRead(out var resto))
            lote.Add(resto);
        if (lote.Count > 0)
        {
            try { await GuardarAsync(lote); }
            catch (Exception ex) { Console.Error.WriteLine($"[EscritorLogsFirestore] Al apagar: {ex.Message}"); }
        }
    }

    private async Task GuardarAsync(List<RegistroLog> registros)
    {
        var db = firestore.Db;
        var escritura = db.StartBatch();
        foreach (var registro in registros)
            escritura.Create(db.Collection(Coleccion).Document(), registro);
        await escritura.CommitAsync();
    }

    private async Task BorrarViejosAsync(CancellationToken detener)
    {
        var limite = DateTime.UtcNow.AddDays(-Math.Max(1, opciones.Value.DiasRetencion));
        var db = firestore.Db;
        while (!detener.IsCancellationRequested)
        {
            var viejos = await db.Collection(Coleccion).WhereLessThan(nameof(RegistroLog.Fecha), limite).Limit(400)
                .GetSnapshotAsync(detener);
            if (viejos.Count == 0)
                return;

            var borrado = db.StartBatch();
            foreach (var doc in viejos.Documents)
                borrado.Delete(doc.Reference);
            await borrado.CommitAsync(detener);
        }
    }
}
