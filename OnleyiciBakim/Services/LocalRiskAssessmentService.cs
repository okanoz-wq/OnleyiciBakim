using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using OnleyiciBakim.Contracts.Common;
using OnleyiciBakim.Contracts.Risk;
using OnleyiciBakim.Data.BakimYonetimi;
using OnleyiciBakim.Data.BakimYonetimi.Entities;
using OnleyiciBakim.Domain.Enums;
using OnleyiciBakim.Infrastructure;
using OnleyiciBakim.Integrations.Ml;
using OnleyiciBakim.Models.Ai;
using OnleyiciBakim.Options;
using OnleyiciBakim.Services.Features;
using OnleyiciBakim.Services.Risk;

namespace OnleyiciBakim.Services;

public sealed class LocalRiskAssessmentService(
    BakimYonetimiDbContext dbContext,
    BakimYonetimiMachineFeatureService featureService,
    IRuleBasedRiskService ruleService,
    IHybridRiskService hybridService,
    IRiskClassificationService classificationService,
    IMaintenanceRecommendationService recommendationService,
    IMlPredictionClient mlClient,
    IOptions<HybridRiskOptions> hybridOptions,
    ILogger<LocalRiskAssessmentService> logger) : ILocalRiskAssessmentService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> MachineLocks =
        new(StringComparer.OrdinalIgnoreCase);

    public async Task<RiskAssessmentResponse> EvaluateMachineAsync(
        string machineId,
        string calculationReason,
        CancellationToken cancellationToken)
    {
        var gate = MachineLocks.GetOrAdd(machineId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            return await EvaluateCoreAsync(machineId, calculationReason, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<RiskAssessmentResponse> EvaluateCoreAsync(
        string machineId,
        string calculationReason,
        CancellationToken cancellationToken)
    {
        var machine = await dbContext.Makineler.AsNoTracking()
            .SingleOrDefaultAsync(x => x.MakineId == machineId, cancellationToken)
            ?? throw new ResourceNotFoundException($"'{machineId}' kimlikli makine bulunamadı.");
        if (!machine.Aktif)
            throw new DomainValidationException("Pasif makine için yeni risk analizi oluşturulamaz.");

        var calculatedAt = DateTimeOffset.UtcNow;
        var features = await featureService.BuildFeaturesAsync(
            machineId, calculatedAt.UtcDateTime, cancellationToken);
        var rule = ruleService.Calculate(features);
        var prediction = await GetPredictionAsync(machineId, features, cancellationToken);
        var hybrid = hybridService.Calculate(
            rule.Score,
            prediction.IsSuccess ? prediction.RiskScore : null,
            features,
            calculatedAt);
        var level = classificationService.Classify(hybrid.FinalRiskScore);
        var recommendation = recommendationService.Get(level);
        var reasons = rule.Reasons
            .Concat(prediction.Warnings)
            .Concat(string.IsNullOrWhiteSpace(prediction.ErrorMessage) ? [] : [prediction.ErrorMessage])
            .Concat(hybrid.AppliedOverrides)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        IDbContextTransaction? transaction = null;
        if (dbContext.Database.IsRelational())
            transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var ruleSetId = await dbContext.RiskKuralSetleri.AsNoTracking()
                .Where(x => x.Aktif)
                .OrderByDescending(x => x.GecerlilikBaslangic)
                .Select(x => (int?)x.KuralSetiId)
                .FirstOrDefaultAsync(cancellationToken);
            var hybridSetId = await dbContext.HibritKararParametreleri.AsNoTracking()
                .Where(x => x.Aktif)
                .OrderByDescending(x => x.HibritSetId)
                .Select(x => (int?)x.HibritSetId)
                .FirstOrDefaultAsync(cancellationToken);
            var dueDate = DateOnly.FromDateTime(calculatedAt.UtcDateTime.Date.AddDays(
                recommendation.RecommendedDays));
            var assessment = new RiskAnalizleri
            {
                AnalizNo = NewStringId("ANL"),
                MakineId = machineId,
                AnalizTarihi = calculatedAt.UtcDateTime,
                KuralSetiId = ruleSetId,
                HibritSetId = hybridSetId,
                KuralPuani = rule.Score,
                MlOlasiligi = prediction.FailureProbability ?? prediction.RiskScore / 100m,
                AiRiskSkoru = prediction.RiskScore,
                ModelVersiyonu = prediction.ModelVersion,
                MlAgirligi = Decimal.ToInt32(hybrid.AiWeight * 100m),
                KuralAgirligi = Decimal.ToInt32(hybrid.RuleWeight * 100m),
                NihaiPuan = hybrid.FinalRiskScore,
                RiskSeviyesi = classificationService.GetLabel(level),
                VeriYeterliligi = GetCompletenessLabel(features.DataCompletenessRate),
                VeriTamlikOrani = features.DataCompletenessRate,
                GuvenSeviyesi = prediction.Confidence,
                OnerilenBakimTarihi = dueDate,
                GirdiSnapshotJson = JsonSerializer.Serialize(features),
                UyarilarJson = JsonSerializer.Serialize(prediction.Warnings),
                HesaplamaNedeni = string.IsNullOrWhiteSpace(calculationReason)
                    ? "Otomatik veri güncellemesi"
                    : calculationReason.Trim(),
                Durum = "Tamamlandı",
                VeriKaynagi = "LOCAL"
            };
            dbContext.RiskAnalizleri.Add(assessment);
            await dbContext.SaveChangesAsync(cancellationToken);

            var order = 0;
            dbContext.RiskAnalizFaktorleri.AddRange(rule.Factors.Select(factor =>
                new RiskAnalizFaktorleri
                {
                    AnalizId = assessment.AnalizId,
                    SiraNo = ++order,
                    Faktor = factor.DisplayName,
                    Deger = factor.RawValue,
                    Etki = factor.Contribution.ToString("0.##", CultureInfo.InvariantCulture),
                    Aciklama = factor.Description
                }));

            var current = await dbContext.GuncelRiskler
                .SingleOrDefaultAsync(x => x.MakineId == machineId, cancellationToken);
            if (current is null)
            {
                current = new GuncelRiskler { RiskId = NewStringId("RSK"), MakineId = machineId };
                dbContext.GuncelRiskler.Add(current);
            }
            current.DegerlendirmeTarihi = calculatedAt.UtcDateTime;
            current.RiskPuani = hybrid.FinalRiskScore;
            current.RiskSeviyesi = classificationService.GetLabel(level);
            current.SonBakimdanGecenGun = Math.Max(0, ToInt(features.SonBakimdanGecenGun));
            current.ArizaSayisi30g = Math.Max(0, ToInt(features.ArizaSayisi30G));
            current.HataSayisi7g = Math.Max(0, ToInt(features.HataSayisi7G));
            current.AnomaliPuani7g = Convert.ToDecimal(features.AnomaliPuani7G ?? 0d, CultureInfo.InvariantCulture);
            current.RiskNedenleri = string.Join("; ", reasons);
            current.OnerilenAksiyon = recommendation.Action;
            current.Durum = "Aktif";
            current.VeriKaynagi = "LOCAL";
            current.KuralPuani = rule.Score;
            current.IstatistikPuani = prediction.RiskScore;
            current.MlOlasiligi = assessment.MlOlasiligi;
            current.ModelDevrede = prediction.IsSuccess && hybrid.AiWeight > 0m;
            current.ModelVersiyonu = prediction.ModelVersion;
            current.VeriYeterliligi = assessment.VeriYeterliligi;
            current.GuvenSeviyesi = prediction.Confidence?.ToString("0.##", CultureInfo.InvariantCulture);

            var trackedMachine = await dbContext.Makineler.SingleAsync(
                x => x.MakineId == machineId, cancellationToken);
            trackedMachine.GuncelRiskPuani = hybrid.FinalRiskScore;
            trackedMachine.GuncelRiskSeviyesi = classificationService.GetLabel(level);
            trackedMachine.GuncellemeTarihi = calculatedAt.UtcDateTime;

            var parameter = await FindRecommendationAsync(hybrid.FinalRiskScore, cancellationToken);
            Uyarilar? warning = null;
            if (parameter?.OtomatikUyariOlustur == true)
            {
                warning = await UpsertWarningAsync(
                    machine, assessment, current, level, dueDate, reasons, recommendation.Action,
                    cancellationToken);
                // Uyarı ve uyarı geçmişini önce yazarak Uyarilar <-> BakimPlanlari
                // döngüsel nullable FK'lerinde güvenli ekleme sırasını koru.
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            BakimPlanlari? plan = null;
            if (hybridOptions.Value.AutoCreateMaintenanceDraft &&
                (parameter?.OtomatikPlanTaslagi ?? true))
            {
                plan = await UpsertPlanAsync(
                    machine, assessment, warning, recommendation, level, dueDate, reasons,
                    cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                if (warning is not null && warning.PlanId != plan.PlanId)
                    warning.PlanId = plan.PlanId;
            }

            dbContext.KararGecmisi.Add(new KararGecmisi
            {
                KararNo = NewStringId("KAR"),
                AnalizId = assessment.AnalizId,
                MakineId = machineId,
                KararTarihi = calculatedAt.UtcDateTime,
                SistemOnerisi = recommendation.Action,
                KullaniciKarari = "Sistem önerisi oluşturuldu",
                Uyumlu = true,
                Aciklama = hybrid.AiWeight == 0m
                    ? "AI kullanılamadı; kural tabanlı skorla devam edildi."
                    : "Hibrit risk sonucu otomatik işlendi."
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            return new RiskAssessmentResponse(
                assessment.AnalizId.ToString(CultureInfo.InvariantCulture), machineId, calculatedAt,
                rule.Score, prediction.RiskScore, hybrid.RuleWeight, hybrid.AiWeight,
                new RiskPresentation(hybrid.FinalRiskScore, classificationService.GetLabel(level),
                    classificationService.GetColorName(level), classificationService.GetColorHex(level)),
                rule.Breakdown, reasons, recommendation.Action, prediction.ModelName,
                prediction.ModelVersion, prediction.Confidence, assessment.AnalizNo,
                features.DataCompletenessRate, rule.MissingFactors, rule.Factors,
                hybrid.AiWeight == 0m, prediction.Status, hybrid.AppliedOverrides,
                hybrid.CalculationDetails);
        }
        catch
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }

    private async Task<MlPredictionResult> GetPredictionAsync(
        string machineId,
        MachineFeatureDto features,
        CancellationToken cancellationToken)
    {
        if (features.DataCompletenessRate < hybridOptions.Value.MinimumCompletenessThreshold)
            return new MlPredictionResult(false, null, null, null, null, null, null, [],
                "Skipped", "INSUFFICIENT_DATA", "ML için veri tamlığı %50'nin altında.",
                DataCompletenessRate: features.DataCompletenessRate);
        var prediction = await mlClient.PredictAsync(
            new MlPredictionRequest(machineId, features), cancellationToken);
        if (!prediction.IsSuccess)
            logger.LogWarning("ML tahmini kullanılamadı; kural tabanlı devam ediliyor. MachineId={MachineId} Error={Error}",
                machineId, prediction.ErrorCode);
        return prediction;
    }

    private async Task<BakimOneriParametreleri?> FindRecommendationAsync(
        decimal score,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.BakimOneriParametreleri.AsNoTracking()
            .Where(x => x.Aktif && x.MinPuan <= score)
            .OrderBy(x => x.MinPuan)
            .ToListAsync(cancellationToken);
        return rows.LastOrDefault(x => score < x.MaxPuan || (x.MaxDahil && score <= x.MaxPuan));
    }

    private async Task<Uyarilar> UpsertWarningAsync(
        Makineler machine,
        RiskAnalizleri assessment,
        GuncelRiskler current,
        RiskLevel level,
        DateOnly dueDate,
        IReadOnlyList<string> reasons,
        string action,
        CancellationToken cancellationToken)
    {
        var warning = await dbContext.Uyarilar.FirstOrDefaultAsync(x =>
            x.MakineId == machine.MakineId &&
            (x.Durum == "Açık" || x.Durum == "Atandı" || x.Durum == "İşlemde"), cancellationToken);
        var oldStatus = warning?.Durum;
        if (warning is null)
        {
            warning = new Uyarilar
            {
                UyariId = NewStringId("UYR"),
                MakineId = machine.MakineId,
                OlusturmaTarihi = assessment.AnalizTarihi,
                Durum = "Açık",
                VeriKaynagi = "LOCAL"
            };
            dbContext.Uyarilar.Add(warning);
        }
        warning.RiskId = current.RiskId;
        warning.AnalizId = assessment.AnalizId;
        warning.UyariSeviyesi = classificationService.GetLabel(level);
        warning.Baslik = $"{machine.MakineAdi} için {warning.UyariSeviyesi.ToLowerInvariant()} risk uyarısı";
        warning.Aciklama = string.Join("; ", reasons);
        warning.OnerilenAksiyon = action;
        warning.SonTarih = dueDate.ToDateTime(TimeOnly.MinValue);
        dbContext.UyariIslemGecmisi.Add(new UyariIslemGecmisi
        {
            UyariId = warning.UyariId,
            IslemTuru = oldStatus is null ? "Otomatik oluşturma" : "Otomatik güncelleme",
            EskiDurum = oldStatus,
            YeniDurum = warning.Durum,
            IslemTarihi = assessment.AnalizTarihi,
            Aciklama = $"Analiz {assessment.AnalizNo} sonucuna göre işlendi."
        });
        return warning;
    }

    private async Task<BakimPlanlari> UpsertPlanAsync(
        Makineler machine,
        RiskAnalizleri assessment,
        Uyarilar? warning,
        MaintenanceRecommendation recommendation,
        RiskLevel level,
        DateOnly dueDate,
        IReadOnlyList<string> reasons,
        CancellationToken cancellationToken)
    {
        var activeStatuses = new[] { "Taslak", "Planlandı", "Onaylandı", "Devam Ediyor" };
        var plan = await dbContext.BakimPlanlari.FirstOrDefaultAsync(x =>
            x.MakineId == machine.MakineId && x.AnalizId != null && activeStatuses.Contains(x.Durum!),
            cancellationToken);
        var oldStatus = plan?.Durum;
        if (plan is null)
        {
            plan = new BakimPlanlari
            {
                PlanId = NewStringId("PLN"),
                MakineId = machine.MakineId,
                Durum = "Taslak",
                OnayDurumu = recommendation.RequiresApproval ? "Onay Bekliyor" : "Onay Gerekmiyor",
                VeriKaynagi = "LOCAL",
                OlusturmaTarihi = assessment.AnalizTarihi,
                TahminiSureDk = 0
            };
            dbContext.BakimPlanlari.Add(plan);
        }
        plan.AnalizId = assessment.AnalizId;
        plan.UyariId = warning?.UyariId;
        plan.OnerilenTarih = dueDate;
        plan.PlanlananTarih = dueDate.ToDateTime(TimeOnly.MinValue);
        plan.BakimTuru = recommendation.MaintenanceType;
        plan.Oncelik = classificationService.GetLabel(level);
        plan.Gerekce = string.Join("; ", reasons);
        plan.GuncellemeTarihi = assessment.AnalizTarihi;
        dbContext.BakimPlanDurumGecmisi.Add(new BakimPlanDurumGecmisi
        {
            PlanId = plan.PlanId,
            EskiDurum = oldStatus,
            YeniDurum = plan.Durum ?? "Taslak",
            DegisiklikTarihi = assessment.AnalizTarihi,
            Aciklama = $"Analiz {assessment.AnalizNo} üzerinden hibrit plan güncellendi."
        });
        return plan;
    }

    private static string GetCompletenessLabel(decimal completeness) => completeness switch
    {
        >= 80m => "Yüksek",
        >= 50m => "Orta",
        _ => "Yetersiz"
    };

    private static int ToInt(double? value) =>
        value.HasValue ? (int)Math.Round(value.Value, MidpointRounding.AwayFromZero) : 0;

    private static string NewStringId(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..20];
}
