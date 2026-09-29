using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnleyiciBakim.Data.BakimYonetimi;
using OnleyiciBakim.Infrastructure;
using OnleyiciBakim.Models.Ai;
using OnleyiciBakim.Options;

namespace OnleyiciBakim.Services.Features;

public sealed class BakimYonetimiMachineFeatureService(
    BakimYonetimiDbContext dbContext,
    IOptions<AlgorithmicRiskOptions> options) : IMachineFeatureService
{
    public async Task<MachineFeatureDto> BuildFeaturesAsync(
        string machineId,
        DateTime calculationDate,
        CancellationToken cancellationToken = default)
    {
        var requestedAt = calculationDate.Kind == DateTimeKind.Utc
            ? calculationDate
            : calculationDate.ToUniversalTime();
        var at = await ResolveDataDateAsync(machineId, requestedAt, cancellationToken);
        var ago7 = at.AddDays(-7);
        var ago30 = at.AddDays(-30);
        var ago90 = at.AddDays(-90);

        var machine = await dbContext.Makineler.AsNoTracking()
            .Where(x => x.MakineId == machineId)
            .Select(x => new
            {
                x.MakineId, x.Yas, x.KurulumYili, x.KurulumTarihi, x.Model, x.KritikSeviye,
                x.SonBakimTarihi
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ResourceNotFoundException($"'{machineId}' kimlikli makine bulunamadı.");

        var lastMaintenance = await dbContext.BakimKayitlari.AsNoTracking()
            .Where(x => x.MakineId == machineId && x.BakimTarihi <= at)
            .MaxAsync(x => (DateTime?)x.BakimTarihi, cancellationToken);
        if (!lastMaintenance.HasValue && machine.SonBakimTarihi != default)
            lastMaintenance = machine.SonBakimTarihi;
        var maintenanceCounts = await dbContext.BakimKayitlari.AsNoTracking()
            .Where(x => x.MakineId == machineId && x.BakimTarihi > ago90 && x.BakimTarihi <= at)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Count30 = group.Count(x => x.BakimTarihi > ago30),
                Count90 = group.Count()
            })
            .FirstOrDefaultAsync(cancellationToken);
        var latestNextMaintenance = await dbContext.BakimKayitlari.AsNoTracking()
            .Where(x => x.MakineId == machineId && x.BakimTarihi <= at)
            .OrderByDescending(x => x.BakimTarihi)
            .Select(x => x.SonrakiBakimTarihi)
            .FirstOrDefaultAsync(cancellationToken);
        var mandatoryOverdue = latestNextMaintenance.HasValue && latestNextMaintenance.Value < at;

        var failureWindow = dbContext.ArizaKartlari.AsNoTracking()
            .Where(x => x.MakineId == machineId && x.ArizaTarihi > ago90 && x.ArizaTarihi <= at);
        var failureAggregates = await failureWindow
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Count30 = group.Count(x => x.ArizaTarihi > ago30),
                Count90 = group.Count(),
                Downtime30 = group.Where(x => x.ArizaTarihi > ago30).Sum(x => x.DurusSuresiDk),
                AverageIntervention90 = group.Average(x => (double?)x.MudahaleSuresiDk)
            })
            .FirstOrDefaultAsync(cancellationToken);
        var totalFailures = await dbContext.ArizaKartlari.AsNoTracking()
            .CountAsync(x => x.MakineId == machineId && x.ArizaTarihi <= at, cancellationToken);
        var failureTypeCounts = await failureWindow
            .GroupBy(x => x.ArizaTuru)
            .Select(group => group.Count())
            .ToListAsync(cancellationToken);
        var repeatedFailures = failureTypeCounts.Sum(count => Math.Max(0, count - 1));
        var repeatedCritical7Days = await dbContext.ArizaKartlari.AsNoTracking()
            .Where(x => x.MakineId == machineId && x.ArizaTarihi > ago7 && x.ArizaTarihi <= at &&
                        (x.OnemSeviyesi == "Kritik" || x.OnemSeviyesi == "KRITIK" ||
                         x.OnemSeviyesi == "Critical"))
            .GroupBy(x => x.ArizaTuru)
            .AnyAsync(group => group.Count() >= 2, cancellationToken);

        var errorCounts = await dbContext.HataKayitlari.AsNoTracking()
            .Where(x => x.MakineId == machineId && x.HataTarihi > ago30 && x.HataTarihi <= at)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Count7 = group.Count(x => x.HataTarihi > ago7),
                Count30 = group.Count()
            })
            .FirstOrDefaultAsync(cancellationToken);
        var telemetry = await dbContext.TelemetriGunluk.AsNoTracking()
            .Where(x => x.MakineId == machineId && x.Tarih > ago7 && x.Tarih <= at)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Voltage = group.Average(x => (double)x.VoltOrt),
                Rotation = group.Average(x => (double)x.DevirOrt),
                Pressure = group.Average(x => (double)x.BasincOrt),
                Vibration = group.Average(x => (double)x.TitresimOrt),
                Anomaly = group.Average(x => (double)x.AnomaliPuani),
                Production = group.Average(x => (double)x.UretimYogunluguYuzde)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var age = machine.KurulumTarihi.HasValue
            ? Math.Max(0, at.Year - machine.KurulumTarihi.Value.Year -
                (DateOnly.FromDateTime(at) < machine.KurulumTarihi.Value.AddYears(
                    Math.Max(0, at.Year - machine.KurulumTarihi.Value.Year)) ? 1 : 0))
            : machine.KurulumYili is >= 1900 and <= 2200
                ? Math.Max(0, at.Year - machine.KurulumYili)
                : Math.Max(0, machine.Yas);
        return new MachineFeatureDto
        {
            MachineId = machine.MakineId,
            Yas = age,
            SonBakimdanGecenGun = lastMaintenance.HasValue
                ? Math.Max(0, (at - lastMaintenance.Value).TotalDays)
                : null,
            BakimSayisi30G = maintenanceCounts?.Count30 ?? 0,
            BakimSayisi90G = maintenanceCounts?.Count90 ?? 0,
            ArizaSayisi30G = failureAggregates?.Count30 ?? 0,
            ArizaSayisi90G = failureAggregates?.Count90 ?? 0,
            ToplamGecmisAriza = totalFailures,
            HataSayisi7G = errorCounts?.Count7 ?? 0,
            HataSayisi30G = errorCounts?.Count30 ?? 0,
            DurusSuresi30G = failureAggregates?.Downtime30 ?? 0,
            OrtMudahaleSuresi90G = failureAggregates?.AverageIntervention90,
            TekrarArizaSayisi90G = repeatedFailures,
            VoltOrt7G = telemetry?.Voltage,
            DevirOrt7G = telemetry?.Rotation,
            BasincOrt7G = telemetry?.Pressure,
            TitresimOrt7G = telemetry?.Vibration,
            AnomaliPuani7G = telemetry?.Anomaly,
            UretimYogunlugu7G = telemetry?.Production,
            Model = NullIfWhiteSpace(machine.Model),
            KritikSeviye = NullIfWhiteSpace(machine.KritikSeviye),
            HasCriticalTelemetry = telemetry?.Anomaly >= (double)options.Value.CriticalAnomalyThreshold,
            IsMandatoryMaintenanceOverdue = mandatoryOverdue,
            HasRepeatedCriticalFailure7Days = repeatedCritical7Days
        };
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<DateTime> ResolveDataDateAsync(
        string machineId,
        DateTime requestedAt,
        CancellationToken cancellationToken)
    {
        var latestMaintenance = await dbContext.BakimKayitlari.AsNoTracking()
            .Where(x => x.MakineId == machineId)
            .MaxAsync(x => (DateTime?)x.BakimTarihi, cancellationToken);
        var latestFailure = await dbContext.ArizaKartlari.AsNoTracking()
            .Where(x => x.MakineId == machineId)
            .MaxAsync(x => (DateTime?)x.ArizaTarihi, cancellationToken);
        var latestError = await dbContext.HataKayitlari.AsNoTracking()
            .Where(x => x.MakineId == machineId)
            .MaxAsync(x => (DateTime?)x.HataTarihi, cancellationToken);
        var latestTelemetry = await dbContext.TelemetriGunluk.AsNoTracking()
            .Where(x => x.MakineId == machineId)
            .MaxAsync(x => (DateTime?)x.Tarih, cancellationToken);
        var latestObservation = new DateTime?[]
        {
            latestMaintenance, latestFailure, latestError, latestTelemetry
        }.Max();

        // Aynı veritabanında tarihsel makineler ile yeni/güncel makineler birlikte
        // bulunabilir. Referans tarihini sistem genelinden değil makine özelinden seç;
        // yeni bir güncel kayıt eski makinelerin tüm 30/90 günlük sinyallerini sıfırlamasın.
        return latestObservation.HasValue && latestObservation.Value < requestedAt.AddDays(-90)
            ? latestObservation.Value
            : requestedAt;
    }
}
