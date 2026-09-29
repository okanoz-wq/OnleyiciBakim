using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnleyiciBakim.Contracts.Risk;
using OnleyiciBakim.Data;
using OnleyiciBakim.Data.BakimYonetimi;
using OnleyiciBakim.Options;
using OnleyiciBakim.Services;

namespace OnleyiciBakim.BackgroundJobs;

public sealed class RiskRecalculationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<RiskAutomationOptions> options,
    ILogger<RiskRecalculationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("Otomatik risk yeniden hesaplama görevi devre dışı.");
            return;
        }

        await RunOnceAsync(settings.BatchSize, stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromHours(settings.IntervalHours));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RunOnceAsync(settings.BatchSize, stoppingToken);
    }

    private async Task RunOnceAsync(int batchSize, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var localRiskService = scope.ServiceProvider.GetService<ILocalRiskAssessmentService>();
            IReadOnlyList<string> machineIds;
            if (localRiskService is not null)
            {
                var localDb = scope.ServiceProvider.GetRequiredService<BakimYonetimiDbContext>();
                machineIds = await localDb.Makineler.AsNoTracking()
                    .Where(x => x.Aktif)
                    .OrderBy(x => x.MakineId)
                    .Select(x => x.MakineId)
                    .ToListAsync(cancellationToken);
            }
            else
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                machineIds = await dbContext.Machines.AsNoTracking()
                    .Where(x => x.Status == "Aktif")
                    .OrderBy(x => x.Id)
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken);
            }
            var riskService = scope.ServiceProvider.GetRequiredService<IRiskAssessmentService>();
            var runDate = DateTimeOffset.UtcNow.ToString("yyyyMMdd");
            var processedCount = 0;
            foreach (var batch in machineIds.Chunk(Math.Max(1, batchSize)))
            {
                foreach (var machineId in batch)
                {
                    try
                    {
                        if (localRiskService is not null)
                            await localRiskService.EvaluateMachineAsync(
                                machineId, $"Zamanlanmış otomatik analiz {runDate}", cancellationToken);
                        else
                            await riskService.EvaluateMachineAsync(
                                machineId,
                                new EvaluateMachineRiskRequest
                                {
                                    CorrelationId = $"auto-{runDate}-{machineId}"
                                },
                                cancellationToken);
                        processedCount++;
                    }
                    catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
                    {
                        logger.LogError(exception,
                            "Otomatik risk hesaplama başarısız. MachineId={MachineId}", machineId);
                    }
                }
            }
            logger.LogInformation("Otomatik risk hesaplama tamamlandı. MachineCount={MachineCount}",
                processedCount);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "Otomatik risk hesaplama turu başlatılamadı.");
        }
    }
}
