using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnleyiciBakim.Data;
using OnleyiciBakim.Domain.Entities;
using OnleyiciBakim.Domain.Enums;
using OnleyiciBakim.Integrations.Erp;
using OnleyiciBakim.Options;

namespace OnleyiciBakim.BackgroundJobs;

public sealed class ErpOutboxWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ErpOptions> options,
    ILogger<ErpOutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.EnableBackgroundSync)
        {
            logger.LogInformation("ERP outbox worker yapılandırma ile kapalı.");
            return;
        }

        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(Math.Clamp(options.Value.OutboxPollingSeconds, 5, 3600)));
        do
        {
            await ProcessBatchAsync(stoppingToken);
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var client = scope.ServiceProvider.GetRequiredService<IErpClient>();
        var now = DateTimeOffset.UtcNow;
        var messages = await db.OutboxMessages
            .Where(x => x.Status == SyncStatus.Pending &&
                        (!x.NextAttemptAtUtc.HasValue || x.NextAttemptAtUtc <= now))
            .OrderBy(x => x.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);
        foreach (var message in messages)
        {
            message.Status = SyncStatus.Processing;
            await db.SaveChangesAsync(cancellationToken);
            try
            {
                if (message.Type != "ErpCreateWorkOrder")
                    throw new InvalidOperationException($"Bilinmeyen outbox türü: {message.Type}");
                var request = JsonSerializer.Deserialize<ErpWorkOrderRequest>(message.PayloadJson)
                    ?? throw new InvalidOperationException("Outbox payload çözümlenemedi.");
                var result = await client.CreateWorkOrderAsync(request, cancellationToken);
                message.AttemptCount++;
                if (result.IsSuccess)
                {
                    message.Status = SyncStatus.Processed;
                    message.ProcessedAtUtc = DateTimeOffset.UtcNow;
                    db.ErpWorkOrderMappings.Add(new ErpWorkOrderMapping
                    {
                        Id = $"ERP-{Guid.NewGuid():N}"[..28],
                        MaintenancePlanId = request.MaintenancePlanId,
                        ErpWorkOrderId = result.ExternalWorkOrderId!,
                        IdempotencyKey = request.IdempotencyKey,
                        Status = result.Status
                    });
                }
                else
                {
                    Reschedule(message, result.ErrorMessage);
                }
            }
            catch (Exception exception)
            {
                message.AttemptCount++;
                Reschedule(message, exception.Message);
                logger.LogWarning(exception, "Outbox mesajı işlenemedi. Id={OutboxId}", message.Id);
            }
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static void Reschedule(OutboxMessage message, string? error)
    {
        message.LastError = error;
        message.Status = message.AttemptCount >= 5 ? SyncStatus.DeadLetter : SyncStatus.Pending;
        message.NextAttemptAtUtc = message.Status == SyncStatus.Pending
            ? DateTimeOffset.UtcNow.AddMinutes(Math.Pow(2, message.AttemptCount))
            : null;
    }
}
