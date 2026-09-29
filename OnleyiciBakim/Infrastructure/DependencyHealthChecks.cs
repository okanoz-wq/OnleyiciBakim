using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OnleyiciBakim.Data;
using OnleyiciBakim.Data.BakimYonetimi;
using OnleyiciBakim.Integrations.Erp;
using OnleyiciBakim.Integrations.Ml;

namespace OnleyiciBakim.Infrastructure;

public sealed class DatabaseReadinessHealthCheck(
    IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("Veritabanı erişilebilir.")
                : HealthCheckResult.Unhealthy("Veritabanına bağlanılamadı.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Veritabanı readiness kontrolü başarısız.", exception);
        }
    }
}

public sealed class BakimYonetimiDatabaseReadinessHealthCheck(
    IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<BakimYonetimiDbContext>();
            if (!await db.Database.CanConnectAsync(cancellationToken))
                return HealthCheckResult.Unhealthy("BakimYonetimiDb veritabanına bağlanılamadı.");

            var machineCount = await db.Makineler.AsNoTracking().CountAsync(cancellationToken);
            return HealthCheckResult.Healthy(
                $"BakimYonetimiDb erişilebilir ({machineCount} makine).",
                new Dictionary<string, object> { ["machineCount"] = machineCount });
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "BakimYonetimiDb readiness kontrolü başarısız.", exception);
        }
    }
}

public sealed class MlReadinessHealthCheck(IMlPredictionClient client) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) =>
        await client.IsReadyAsync(cancellationToken)
            ? HealthCheckResult.Healthy("ML servisi erişilebilir.")
            : HealthCheckResult.Degraded("ML servisi erişilemiyor.");
}

public sealed class ErpReadinessHealthCheck(IErpClient client) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) =>
        await client.IsReadyAsync(cancellationToken)
            ? HealthCheckResult.Healthy("ERP servisi erişilebilir.")
            : HealthCheckResult.Degraded("ERP servisi erişilemiyor.");
}
