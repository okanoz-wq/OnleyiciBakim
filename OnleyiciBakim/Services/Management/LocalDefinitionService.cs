using System.Globalization;
using Microsoft.EntityFrameworkCore;
using OnleyiciBakim.Contracts.Management;
using OnleyiciBakim.Data.BakimYonetimi;
using OnleyiciBakim.Data.BakimYonetimi.Entities;
using OnleyiciBakim.Infrastructure;

namespace OnleyiciBakim.Services.Management;

public interface ILocalDefinitionService
{
    IReadOnlyList<LocalDefinitionMetadata> GetModules();
    Task<IReadOnlyList<LocalDefinitionRow>> ListAsync(string module, bool includeInactive, CancellationToken token);
    Task<LocalDefinitionRow> GetAsync(string module, string id, CancellationToken token);
    Task<LocalDefinitionRow> SaveAsync(string module, string? id, LocalDefinitionUpsertRequest request, CancellationToken token);
    Task DeactivateAsync(string module, string id, CancellationToken token);
}

public sealed class LocalDefinitionService(BakimYonetimiDbContext db) : ILocalDefinitionService
{
    private static readonly LocalDefinitionMetadata[] Modules =
    [
        new("companies", "Firmalar", "Firma Kodu", "Firma Adı", null, null),
        new("branches", "Şubeler", "Şube Kodu", "Şube Adı", "companies", "Firma"),
        new("departments", "Departmanlar", "Departman Kodu", "Departman Adı", "branches", "Şube"),
        new("work-centers", "İş Merkezleri", "İş Merkezi Kodu", "İş Merkezi Adı", "departments", "Departman"),
        new("components", "Makine Bileşenleri", "Bileşen Kodu", "Bileşen Adı", "machines", "Makine"),
        new("failure-codes", "Arıza Kodları", "Arıza Kodu", "Arıza Adı", null, null),
        new("maintenance-types", "Bakım Türleri", "Bakım Türü Kodu", "Bakım Türü Adı", null, null),
        new("checklists", "Bakım Kontrol Listeleri", "Liste Kodu", "Liste Adı", "maintenance-types", "Bakım Türü", true),
        new("risk-rules", "Risk Kural Parametreleri", "Set Kodu", "Set Adı", null, null),
        new("hybrid-parameters", "Hibrit Karar Parametreleri", "Set Kodu", "Set Adı", "risk-rules", "Kural Seti"),
        new("maintenance-recommendations", "Bakım Öneri Parametreleri", "Öneri Kodu", "Risk Seviyesi", null, null),
        new("counters", "Sayaç Tanımları", "Sayaç Kodu", "Sayaç Adı", null, null),
        new("shifts", "Vardiyalar", "Vardiya Kodu", "Vardiya Adı", null, null),
        new("teams", "Ekipler", "Ekip Kodu", "Ekip Adı", null, null),
        new("personnel", "Personeller", "Personel Kodu", "Ad Soyad", null, null)
    ];

    public IReadOnlyList<LocalDefinitionMetadata> GetModules() => Modules;

    public async Task<IReadOnlyList<LocalDefinitionRow>> ListAsync(string module, bool includeInactive, CancellationToken token)
    {
        // Makine bileşeni formu, üst kayıt seçimi için makinelere ihtiyaç duyar.
        // "machines" yalnızca lookup olarak listelenir; Modules dizisine eklenmediği
        // için tanım ekranından oluşturma/güncelleme/silme işlemlerine açılamaz.
        if (module == "machines")
        {
            return await db.Makineler.AsNoTracking()
                .Where(x => includeInactive || x.Aktif)
                .OrderBy(x => x.MakineAdi)
                .Select(x => Row(
                    x.MakineId,
                    x.MakineKodu,
                    x.MakineAdi,
                    x.Aktif,
                    null,
                    null,
                    x.Model,
                    x.Aciklama,
                    null))
                .ToListAsync(token);
        }

        if (module == "warnings")
        {
            return await db.Uyarilar.AsNoTracking()
                .Where(x => includeInactive || x.Durum != "Kapalı")
                .OrderByDescending(x => x.OlusturmaTarihi)
                .Select(x => Row(
                    x.UyariId,
                    x.UyariId,
                    x.Baslik ?? "Uyarı",
                    x.Durum != "Kapalı",
                    x.MakineId,
                    x.Makine.MakineAdi,
                    x.UyariSeviyesi,
                    x.Aciklama,
                    null))
                .ToListAsync(token);
        }

        if (module == "parts")
        {
            return await db.Parcalar.AsNoTracking()
                .Where(x => includeInactive || x.Aktif)
                .OrderBy(x => x.ParcaAdi)
                .Select(x => Row(
                    x.ParcaKodu,
                    x.ParcaKodu,
                    x.ParcaAdi,
                    x.Aktif,
                    null,
                    null,
                    x.BilesenTuru,
                    null,
                    null))
                .ToListAsync(token);
        }

        ValidateModule(module);
        return module switch
        {
            "companies" => await db.Firmalar.AsNoTracking().Where(x => includeInactive || x.AktifMi)
                .OrderBy(x => x.FirmaAdi).Select(x => Row(x.FirmaId, x.FirmaKodu, x.FirmaAdi, x.AktifMi, null, null, null, x.Aciklama, null)).ToListAsync(token),
            "branches" => await db.Subeler.AsNoTracking().Where(x => includeInactive || x.AktifMi)
                .OrderBy(x => x.SubeAdi).Select(x => Row(x.SubeId, x.SubeKodu, x.SubeAdi, x.AktifMi, x.FirmaId, x.Firma.FirmaAdi, null, x.Aciklama, null)).ToListAsync(token),
            "departments" => await db.Departmanlar.AsNoTracking().Where(x => includeInactive || x.AktifMi)
                .OrderBy(x => x.DepartmanAdi).Select(x => Row(x.DepartmanId, x.DepartmanKodu, x.DepartmanAdi, x.AktifMi, x.SubeId, x.Sube.SubeAdi, null, x.Aciklama, null)).ToListAsync(token),
            "work-centers" => await db.UretimHatlari.AsNoTracking().Where(x => includeInactive || x.AktifMi)
                .OrderBy(x => x.HatAdi).Select(x => Row(x.HatId, x.HatKodu, x.HatAdi, x.AktifMi, x.DepartmanId, x.Departman.DepartmanAdi, null, x.Aciklama, null)).ToListAsync(token),
            "components" => await db.MakineBilesenler.AsNoTracking().Where(x => includeInactive || x.Aktif)
                .OrderBy(x => x.BilesenAdi).Select(x => Row(x.BilesenId.ToString(), x.BilesenKodu, x.BilesenAdi, x.Aktif, x.MakineId, x.Makine.MakineAdi, x.BilesenTuru, x.Aciklama, null)).ToListAsync(token),
            "failure-codes" => await db.ArizaKoduTanimlari.AsNoTracking().Where(x => includeInactive || x.Aktif)
                .OrderBy(x => x.ArizaAdi).Select(x => Row(x.ArizaKoduId.ToString(), x.ArizaKodu, x.ArizaAdi, x.Aktif, null, null, x.ArizaKategorisi, x.Aciklama, null)).ToListAsync(token),
            "maintenance-types" => await db.BakimTuruTanimlari.AsNoTracking().Where(x => includeInactive || x.Aktif)
                .OrderBy(x => x.BakimTuruAdi).Select(x => Row(x.BakimTuruId.ToString(), x.BakimTuruKodu, x.BakimTuruAdi, x.Aktif, null, null, x.Tur, x.Aciklama, null)).ToListAsync(token),
            "checklists" => await db.BakimKontrolListeleri.AsNoTracking().Where(x => includeInactive || x.Aktif)
                .OrderBy(x => x.KontrolListesiAdi).Select(x => Row(x.KontrolListesiId.ToString(), x.KontrolListesiKodu, x.KontrolListesiAdi, x.Aktif, x.BakimTuruKodu, x.BakimTuruKoduNavigation!.BakimTuruAdi, x.MakineTuru, x.Aciklama, null)).ToListAsync(token),
            "risk-rules" => await db.RiskKuralSetleri.AsNoTracking().Where(x => includeInactive || x.Aktif)
                .OrderBy(x => x.KuralSetiAdi).Select(x => Row(x.KuralSetiId.ToString(), x.KuralSetiKodu, x.KuralSetiAdi, x.Aktif, null, null, x.Versiyon, x.Aciklama, null)).ToListAsync(token),
            "hybrid-parameters" => await db.HibritKararParametreleri.AsNoTracking().Where(x => includeInactive || x.Aktif)
                .OrderBy(x => x.SetAdi).Select(x => Row(x.HibritSetId.ToString(), x.SetKodu, x.SetAdi, x.Aktif, x.KuralSetiId.ToString(), x.KuralSeti!.KuralSetiAdi, x.ModelAdi, x.Aciklama, null)).ToListAsync(token),
            "maintenance-recommendations" => await db.BakimOneriParametreleri.AsNoTracking().Where(x => includeInactive || x.Aktif)
                .OrderBy(x => x.MinPuan).Select(x => Row(x.OneriId.ToString(), x.OneriKodu, x.RiskSeviyesi, x.Aktif, null, null, x.VarsayilanBakimTuru, x.Aciklama, null)).ToListAsync(token),
            "counters" => await db.MakineSayacTanimlari.AsNoTracking().Where(x => includeInactive || x.Aktif)
                .OrderBy(x => x.SayacAdi).Select(x => Row(x.SayacTanimId.ToString(), x.SayacKodu, x.SayacAdi, x.Aktif, null, null, x.SayacTuru, x.Aciklama, null)).ToListAsync(token),
            "shifts" => await db.Vardiyalar.AsNoTracking().Where(x => includeInactive || x.Aktif)
                .OrderBy(x => x.BaslangicSaati).Select(x => Row(x.VardiyaId.ToString(), x.VardiyaKodu, x.VardiyaAdi, x.Aktif, null, null, x.BaslangicSaati + " – " + x.BitisSaati, x.Aciklama, null)).ToListAsync(token),
            "teams" => await db.Ekipler.AsNoTracking().Where(x => includeInactive || x.Aktif)
                .OrderBy(x => x.EkipAdi).Select(x => Row(x.EkipId.ToString(), x.EkipKodu, x.EkipAdi, x.Aktif, null, null, null, x.Aciklama, null)).ToListAsync(token),
            "personnel" => await db.Personeller.AsNoTracking().Where(x => includeInactive || x.AktifMi)
                .OrderBy(x => x.AdSoyad).Select(x => Row(x.PersonelId, x.PersonelId, x.AdSoyad, x.AktifMi, null, null, x.Rol, null, null)).ToListAsync(token),
            _ => throw new DomainValidationException("Geçersiz tanım modülü.")
        };
    }

    public async Task<LocalDefinitionRow> GetAsync(string module, string id, CancellationToken token)
    {
        var row = (await ListAsync(module, true, token)).SingleOrDefault(x => x.Id == id)
            ?? throw new ResourceNotFoundException("Kayıt bulunamadı.");
        var details = new Dictionary<string, string?>();
        switch (module)
        {
            case "components":
                var component = await db.MakineBilesenler.AsNoTracking()
                    .SingleAsync(x => x.BilesenId == IntId(id), token);
                details["severity"] = component.Kritiklik;
                details["estimatedLife"] = component.TahminiOmur?.ToString(CultureInfo.InvariantCulture);
                details["unit"] = component.OmurBirimi;
                break;
            case "failure-codes":
                var failureCode = await db.ArizaKoduTanimlari.AsNoTracking()
                    .SingleAsync(x => x.ArizaKoduId == IntId(id), token);
                details["severity"] = failureCode.VarsayilanOnemSeviyesi;
                details["machineType"] = failureCode.MakineTuru;
                details["flag"] = failureCode.UretimiDurdururMu.ToString();
                break;
            case "maintenance-types":
                var maintenanceType = await db.BakimTuruTanimlari.AsNoTracking()
                    .SingleAsync(x => x.BakimTuruId == IntId(id), token);
                details["severity"] = maintenanceType.VarsayilanOncelik;
                break;
            case "shifts":
                var shift = await db.Vardiyalar.AsNoTracking().SingleAsync(x => x.VardiyaId == IntId(id), token);
                details["startTime"] = shift.BaslangicSaati.ToString("HH:mm"); details["endTime"] = shift.BitisSaati.ToString("HH:mm");
                break;
            case "hybrid-parameters":
                var hybrid = await db.HibritKararParametreleri.AsNoTracking().SingleAsync(x => x.HibritSetId == IntId(id), token);
                details["aiWeight"] = hybrid.MlAgirligi.ToString(); details["ruleWeight"] = hybrid.KuralAgirligi.ToString(); details["version"] = hybrid.ModelVersiyonu;
                break;
            case "maintenance-recommendations":
                var recommendation = await db.BakimOneriParametreleri.AsNoTracking().SingleAsync(x => x.OneriId == IntId(id), token);
                details["minimum"] = recommendation.MinPuan.ToString(CultureInfo.InvariantCulture); details["maximum"] = recommendation.MaxPuan.ToString(CultureInfo.InvariantCulture); details["recommendedDays"] = recommendation.OnerilenGun.ToString(); details["maxIncluded"] = recommendation.MaxDahil.ToString();
                break;
            case "checklists":
                var items = await db.KontrolListesiMaddeleri.AsNoTracking().Where(x => x.KontrolListesiId == IntId(id) && x.Aktif).OrderBy(x => x.SiraNo).ToListAsync(token);
                details["items"] = System.Text.Json.JsonSerializer.Serialize(items.Select(x => new { itemId = x.MaddeId, text = x.KontrolMaddesi, required = x.Zorunlu, descriptionRequired = x.AciklamaGerekli }));
                break;
        }
        return row with { Details = details };
    }

    public async Task<LocalDefinitionRow> SaveAsync(string module, string? id, LocalDefinitionUpsertRequest r, CancellationToken token)
    {
        ValidateModule(module);
        Normalize(r);
        switch (module)
        {
            case "companies": await SaveCompany(id, r, token); break;
            case "branches": await SaveBranch(id, r, token); break;
            case "departments": await SaveDepartment(id, r, token); break;
            case "work-centers": await SaveWorkCenter(id, r, token); break;
            case "components": await SaveComponent(id, r, token); break;
            case "failure-codes": await SaveFailureCode(id, r, token); break;
            case "maintenance-types": await SaveMaintenanceType(id, r, token); break;
            case "checklists": await SaveChecklist(id, r, token); break;
            case "risk-rules": await SaveRuleSet(id, r, token); break;
            case "hybrid-parameters": await SaveHybrid(id, r, token); break;
            case "maintenance-recommendations": await SaveRecommendation(id, r, token); break;
            case "counters": await SaveCounter(id, r, token); break;
            case "shifts": await SaveShift(id, r, token); break;
            case "teams": await SaveTeam(id, r, token); break;
            case "personnel": await SavePersonnel(id, r, token); break;
        }
        await db.SaveChangesAsync(token);
        return await GetAsync(module, id ?? await ResolveId(module, r.Code, token), token);
    }

    public async Task DeactivateAsync(string module, string id, CancellationToken token)
    {
        ValidateModule(module);
        switch (module)
        {
            case "companies": (await db.Firmalar.FindAsync([id], token) ?? throw NotFound()).AktifMi = false; break;
            case "branches": (await db.Subeler.FindAsync([id], token) ?? throw NotFound()).AktifMi = false; break;
            case "departments": (await db.Departmanlar.FindAsync([id], token) ?? throw NotFound()).AktifMi = false; break;
            case "work-centers": (await db.UretimHatlari.FindAsync([id], token) ?? throw NotFound()).AktifMi = false; break;
            case "components": (await db.MakineBilesenler.FindAsync([IntId(id)], token) ?? throw NotFound()).Aktif = false; break;
            case "failure-codes": (await db.ArizaKoduTanimlari.FindAsync([IntId(id)], token) ?? throw NotFound()).Aktif = false; break;
            case "maintenance-types": (await db.BakimTuruTanimlari.FindAsync([IntId(id)], token) ?? throw NotFound()).Aktif = false; break;
            case "checklists": (await db.BakimKontrolListeleri.FindAsync([IntId(id)], token) ?? throw NotFound()).Aktif = false; break;
            case "risk-rules": (await db.RiskKuralSetleri.FindAsync([IntId(id)], token) ?? throw NotFound()).Aktif = false; break;
            case "hybrid-parameters": (await db.HibritKararParametreleri.FindAsync([IntId(id)], token) ?? throw NotFound()).Aktif = false; break;
            case "maintenance-recommendations": (await db.BakimOneriParametreleri.FindAsync([IntId(id)], token) ?? throw NotFound()).Aktif = false; break;
            case "counters": (await db.MakineSayacTanimlari.FindAsync([IntId(id)], token) ?? throw NotFound()).Aktif = false; break;
            case "shifts": (await db.Vardiyalar.FindAsync([IntId(id)], token) ?? throw NotFound()).Aktif = false; break;
            case "teams": (await db.Ekipler.FindAsync([IntId(id)], token) ?? throw NotFound()).Aktif = false; break;
            case "personnel": (await db.Personeller.FindAsync([id], token) ?? throw NotFound()).AktifMi = false; break;
        }
        await db.SaveChangesAsync(token);
    }

    private async Task SaveCompany(string? id, LocalDefinitionUpsertRequest r, CancellationToken t)
    {
        if (await db.Firmalar.AnyAsync(x => x.FirmaKodu == r.Code && x.FirmaId != id, t)) throw Duplicate("firma");
        var x = id is null ? new Firmalar { FirmaId = NewId("FRM"), OlusturmaTarihi = DateTime.UtcNow } : await db.Firmalar.FindAsync([id], t) ?? throw NotFound();
        x.FirmaKodu = r.Code; x.FirmaAdi = r.Name; x.AktifMi = r.Active; x.Aciklama = r.Description; x.VeriKaynagi = "LOCAL"; x.GuncellemeTarihi = DateTime.UtcNow;
        if (id is null) db.Firmalar.Add(x);
    }

    private async Task SaveBranch(string? id, LocalDefinitionUpsertRequest r, CancellationToken t)
    {
        RequireParent(r); if (!await db.Firmalar.AnyAsync(x => x.FirmaId == r.ParentId && x.AktifMi, t)) throw InvalidParent();
        if (await db.Subeler.AnyAsync(x => x.FirmaId == r.ParentId && x.SubeKodu == r.Code && x.SubeId != id, t)) throw Duplicate("şube");
        var x = id is null ? new Subeler { SubeId = NewId("SUB"), OlusturmaTarihi = DateTime.UtcNow } : await db.Subeler.FindAsync([id], t) ?? throw NotFound();
        x.FirmaId = r.ParentId!; x.SubeKodu = r.Code; x.SubeAdi = r.Name; x.AktifMi = r.Active; x.Aciklama = r.Description; x.VeriKaynagi = "LOCAL"; x.GuncellemeTarihi = DateTime.UtcNow; if (id is null) db.Subeler.Add(x);
    }

    private async Task SaveDepartment(string? id, LocalDefinitionUpsertRequest r, CancellationToken t)
    {
        RequireParent(r); if (!await db.Subeler.AnyAsync(x => x.SubeId == r.ParentId && x.AktifMi, t)) throw InvalidParent();
        if (await db.Departmanlar.AnyAsync(x => x.SubeId == r.ParentId && x.DepartmanKodu == r.Code && x.DepartmanId != id, t)) throw Duplicate("departman");
        var x = id is null ? new Departmanlar { DepartmanId = NewId("DEP"), OlusturmaTarihi = DateTime.UtcNow } : await db.Departmanlar.FindAsync([id], t) ?? throw NotFound();
        x.SubeId = r.ParentId!; x.DepartmanKodu = r.Code; x.DepartmanAdi = r.Name; x.AktifMi = r.Active; x.Aciklama = r.Description; x.VeriKaynagi = "LOCAL"; x.GuncellemeTarihi = DateTime.UtcNow; if (id is null) db.Departmanlar.Add(x);
    }

    private async Task SaveWorkCenter(string? id, LocalDefinitionUpsertRequest r, CancellationToken t)
    {
        RequireParent(r); if (!await db.Departmanlar.AnyAsync(x => x.DepartmanId == r.ParentId && x.AktifMi, t)) throw InvalidParent();
        if (await db.UretimHatlari.AnyAsync(x => x.DepartmanId == r.ParentId && x.HatKodu == r.Code && x.HatId != id, t)) throw Duplicate("iş merkezi");
        var x = id is null ? new UretimHatlari { HatId = NewId("ISM"), OlusturmaTarihi = DateTime.UtcNow } : await db.UretimHatlari.FindAsync([id], t) ?? throw NotFound();
        x.DepartmanId = r.ParentId!; x.HatKodu = r.Code; x.HatAdi = r.Name; x.VardiyaSayisi = 3; x.AktifMi = r.Active; x.Aciklama = r.Description; x.VeriKaynagi = "LOCAL"; x.GuncellemeTarihi = DateTime.UtcNow; if (id is null) db.UretimHatlari.Add(x);
    }

    private async Task SaveComponent(string? id, LocalDefinitionUpsertRequest r, CancellationToken t)
    {
        if (r.Code.Length > 20)
            throw new DomainValidationException("Bileşen kodu en fazla 20 karakter olabilir.");
        if (r.Name.Length > 100)
            throw new DomainValidationException("Bileşen adı en fazla 100 karakter olabilir.");
        if (r.Type?.Length > 50)
            throw new DomainValidationException("Bileşen türü en fazla 50 karakter olabilir.");
        if (r.Severity?.Length > 20)
            throw new DomainValidationException("Kritiklik en fazla 20 karakter olabilir.");
        if (r.Unit?.Length > 20)
            throw new DomainValidationException("Ömür birimi en fazla 20 karakter olabilir.");
        if (r.EstimatedLife < 0)
            throw new DomainValidationException("Tahmini ömür negatif olamaz.");
        r.ParentId ??= r.MachineId; RequireParent(r); if (!await db.Makineler.AnyAsync(x => x.MakineId == r.ParentId && x.Aktif, t)) throw InvalidParent();
        var key = id is null ? (int?)null : IntId(id); if (await db.MakineBilesenler.AnyAsync(x => x.MakineId == r.ParentId && x.BilesenKodu == r.Code && x.BilesenId != key, t)) throw Duplicate("bileşen");
        var x = key is null ? new MakineBilesenler() : await db.MakineBilesenler.FindAsync([key.Value], t) ?? throw NotFound();
        x.MakineId = r.ParentId!; x.BilesenKodu = r.Code; x.BilesenAdi = r.Name; x.BilesenTuru = r.Type; x.Kritiklik = r.Severity; x.TahminiOmur = r.EstimatedLife; x.OmurBirimi = r.Unit; x.Aktif = r.Active; x.Aciklama = r.Description; if (key is null) db.MakineBilesenler.Add(x);
    }

    private async Task SaveFailureCode(string? id, LocalDefinitionUpsertRequest r, CancellationToken t)
    {
        if (r.Code.Length > 20) throw new DomainValidationException("Arıza kodu en fazla 20 karakter olabilir.");
        if (r.Name.Length > 150) throw new DomainValidationException("Arıza adı en fazla 150 karakter olabilir.");
        if (r.Category?.Length > 50) throw new DomainValidationException("Arıza kategorisi en fazla 50 karakter olabilir.");
        if (r.Severity?.Length > 20) throw new DomainValidationException("Önem seviyesi en fazla 20 karakter olabilir.");
        if (r.MachineType?.Length > 50) throw new DomainValidationException("Makine türü en fazla 50 karakter olabilir.");
        var key = id is null ? (int?)null : IntId(id); if (await db.ArizaKoduTanimlari.AnyAsync(x => x.ArizaKodu == r.Code && x.ArizaKoduId != key, t)) throw Duplicate("arıza kodu");
        var x = key is null ? new ArizaKoduTanimlari() : await db.ArizaKoduTanimlari.FindAsync([key.Value], t) ?? throw NotFound();
        x.ArizaKodu = r.Code; x.ArizaAdi = r.Name; x.ArizaKategorisi = r.Category; x.VarsayilanOnemSeviyesi = r.Severity; x.MakineTuru = r.MachineType; x.UretimiDurdururMu = r.Flag; x.Aktif = r.Active; x.Aciklama = r.Description; if (key is null) db.ArizaKoduTanimlari.Add(x);
    }

    private async Task SaveMaintenanceType(string? id, LocalDefinitionUpsertRequest r, CancellationToken t)
    {
        if (r.Code.Length > 20) throw new DomainValidationException("Bakım türü kodu en fazla 20 karakter olabilir.");
        if (r.Name.Length > 100) throw new DomainValidationException("Bakım türü adı en fazla 100 karakter olabilir.");
        if (r.Type?.Length > 20) throw new DomainValidationException("Bakım türü en fazla 20 karakter olabilir.");
        if (r.Severity?.Length > 20) throw new DomainValidationException("Varsayılan öncelik en fazla 20 karakter olabilir.");
        var key = id is null ? (int?)null : IntId(id); if (await db.BakimTuruTanimlari.AnyAsync(x => x.BakimTuruKodu == r.Code && x.BakimTuruId != key, t)) throw Duplicate("bakım türü");
        var x = key is null ? new BakimTuruTanimlari() : await db.BakimTuruTanimlari.FindAsync([key.Value], t) ?? throw NotFound();
        x.BakimTuruKodu = r.Code; x.BakimTuruAdi = r.Name; x.Tur = r.Type; x.VarsayilanOncelik = r.Severity; x.Aktif = r.Active; x.Aciklama = r.Description; if (key is null) db.BakimTuruTanimlari.Add(x);
    }

    private async Task SaveChecklist(string? id, LocalDefinitionUpsertRequest r, CancellationToken t)
    {
        if (r.Items.Count == 0) throw new DomainValidationException("Kontrol listesi en az bir madde içermelidir.");
        var key = id is null ? (int?)null : IntId(id); if (await db.BakimKontrolListeleri.AnyAsync(x => x.KontrolListesiKodu == r.Code && x.KontrolListesiId != key, t)) throw Duplicate("kontrol listesi");
        var x = key is null ? new BakimKontrolListeleri { OlusturmaTarihi = DateTime.UtcNow } : await db.BakimKontrolListeleri.Include(y => y.KontrolListesiMaddeleri).SingleOrDefaultAsync(y => y.KontrolListesiId == key, t) ?? throw NotFound();
        x.KontrolListesiKodu = r.Code; x.KontrolListesiAdi = r.Name; x.BakimTuruKodu = r.ParentId; x.MakineTuru = r.MachineType; x.Aktif = r.Active; x.Aciklama = r.Description; x.GuncellemeTarihi = DateTime.UtcNow;
        db.KontrolListesiMaddeleri.RemoveRange(x.KontrolListesiMaddeleri); x.KontrolListesiMaddeleri = r.Items.Select((m, i) => new KontrolListesiMaddeleri { SiraNo = i + 1, KontrolMaddesi = m.Text.Trim(), Zorunlu = m.Required, AciklamaGerekli = m.DescriptionRequired, Aktif = true }).ToList(); if (key is null) db.BakimKontrolListeleri.Add(x);
    }

    private async Task SaveRuleSet(string? id, LocalDefinitionUpsertRequest r, CancellationToken t)
    {
        var key = id is null ? (int?)null : IntId(id); var version = string.IsNullOrWhiteSpace(r.Version) ? "1.0" : r.Version.Trim();
        if (await db.RiskKuralSetleri.AnyAsync(x => x.KuralSetiKodu == r.Code && x.Versiyon == version && x.KuralSetiId != key, t)) throw Duplicate("kural seti");
        var x = key is null ? new RiskKuralSetleri() : await db.RiskKuralSetleri.FindAsync([key.Value], t) ?? throw NotFound(); x.KuralSetiKodu = r.Code; x.KuralSetiAdi = r.Name; x.Versiyon = version; x.Aktif = r.Active; x.Aciklama = r.Description; if (key is null) db.RiskKuralSetleri.Add(x);
    }

    private async Task SaveHybrid(string? id, LocalDefinitionUpsertRequest r, CancellationToken t)
    {
        var ai = r.AiWeight ?? 40; var rule = r.RuleWeight ?? 60; if (ai + rule != 100) throw new DomainValidationException("AI ve kural ağırlıklarının toplamı 100 olmalıdır.");
        int? parent = string.IsNullOrWhiteSpace(r.ParentId) ? null : IntId(r.ParentId); if (parent.HasValue && !await db.RiskKuralSetleri.AnyAsync(x => x.KuralSetiId == parent, t)) throw InvalidParent();
        var key = id is null ? (int?)null : IntId(id); if (await db.HibritKararParametreleri.AnyAsync(x => x.SetKodu == r.Code && x.HibritSetId != key, t)) throw Duplicate("hibrit set");
        var x = key is null ? new HibritKararParametreleri() : await db.HibritKararParametreleri.FindAsync([key.Value], t) ?? throw NotFound(); x.SetKodu = r.Code; x.SetAdi = r.Name; x.KuralSetiId = parent; x.ModelAdi = r.Type; x.ModelVersiyonu = r.Version; x.MlAgirligi = ai; x.KuralAgirligi = rule; x.BuyukFarktaEngelle = r.Flag; x.OtomatikPlanTaslagi = true; x.Aktif = r.Active; x.Aciklama = r.Description; if (key is null) db.HibritKararParametreleri.Add(x);
    }

    private async Task SaveRecommendation(string? id, LocalDefinitionUpsertRequest r, CancellationToken t)
    {
        var min = r.Minimum ?? 0; var max = r.Maximum ?? 100; if (min < 0 || max > 100 || min >= max) throw new DomainValidationException("Risk aralığı 0–100 içinde olmalı ve alt değer üst değerden küçük olmalıdır.");
        var key = id is null ? (int?)null : IntId(id); if (await db.BakimOneriParametreleri.AnyAsync(x => x.OneriId != key && x.Aktif && min < x.MaxPuan && x.MinPuan < max, t)) throw new ConflictException("Risk aralığı başka bir aktif parametre ile çakışıyor.");
        var x = key is null ? new BakimOneriParametreleri() : await db.BakimOneriParametreleri.FindAsync([key.Value], t) ?? throw NotFound(); x.OneriKodu = r.Code; x.RiskSeviyesi = r.Name; x.MinPuan = min; x.MaxPuan = max; x.MaxDahil = r.MaxIncluded; x.OnerilenGun = r.RecommendedDays ?? 21; x.VarsayilanBakimTuru = r.Type; x.OtomatikUyariOlustur = true; x.OtomatikPlanTaslagi = true; x.IslemGerekmiyorSecenegi = true; x.Aktif = r.Active; x.Aciklama = r.Description; if (key is null) db.BakimOneriParametreleri.Add(x);
    }

    private async Task SaveCounter(string? id, LocalDefinitionUpsertRequest r, CancellationToken t)
    {
        var key = id is null ? (int?)null : IntId(id); if (await db.MakineSayacTanimlari.AnyAsync(x => x.SayacKodu == r.Code && x.SayacTanimId != key, t)) throw Duplicate("sayaç"); var x = key is null ? new MakineSayacTanimlari { OlusturmaTarihi = DateTime.UtcNow } : await db.MakineSayacTanimlari.FindAsync([key.Value], t) ?? throw NotFound(); x.SayacKodu = r.Code; x.SayacAdi = r.Name; x.SayacTuru = r.Type ?? "Çalışma"; x.Birim = r.Unit ?? "saat"; x.Aktif = r.Active; x.Aciklama = r.Description; x.GuncellemeTarihi = DateTime.UtcNow; if (key is null) db.MakineSayacTanimlari.Add(x);
    }

    private async Task SaveShift(string? id, LocalDefinitionUpsertRequest r, CancellationToken t)
    {
        if (!r.StartTime.HasValue || !r.EndTime.HasValue) throw new DomainValidationException("Vardiya başlangıç ve bitiş saatleri zorunludur."); var key = id is null ? (int?)null : IntId(id); if (await db.Vardiyalar.AnyAsync(x => x.VardiyaKodu == r.Code && x.VardiyaId != key, t)) throw Duplicate("vardiya"); var x = key is null ? new Vardiyalar { OlusturmaTarihi = DateTime.UtcNow } : await db.Vardiyalar.FindAsync([key.Value], t) ?? throw NotFound(); x.VardiyaKodu = r.Code; x.VardiyaAdi = r.Name; x.BaslangicSaati = r.StartTime.Value; x.BitisSaati = r.EndTime.Value; x.Aktif = r.Active; x.Aciklama = r.Description; x.GuncellemeTarihi = DateTime.UtcNow; if (key is null) db.Vardiyalar.Add(x);
    }

    private async Task SaveTeam(string? id, LocalDefinitionUpsertRequest r, CancellationToken t)
    {
        var key = id is null ? (int?)null : IntId(id); if (await db.Ekipler.AnyAsync(x => x.EkipKodu == r.Code && x.EkipId != key, t)) throw Duplicate("ekip"); var x = key is null ? new Ekipler { OlusturmaTarihi = DateTime.UtcNow } : await db.Ekipler.FindAsync([key.Value], t) ?? throw NotFound(); x.EkipKodu = r.Code; x.EkipAdi = r.Name; x.Aktif = r.Active; x.Aciklama = r.Description; x.GuncellemeTarihi = DateTime.UtcNow; if (key is null) db.Ekipler.Add(x);
    }

    private async Task SavePersonnel(string? id, LocalDefinitionUpsertRequest r, CancellationToken t)
    {
        var key = id ?? NewId("PRS"); if (id is null && await db.Personeller.AnyAsync(x => x.PersonelId == r.Code, t)) throw Duplicate("personel"); var x = id is null ? new Personeller { PersonelId = r.Code.Length <= 20 ? r.Code : key } : await db.Personeller.FindAsync([id], t) ?? throw NotFound(); x.AdSoyad = r.Name; x.Rol = r.Role ?? r.Type; x.Uzmanlik = r.Category; x.AktifMi = r.Active; x.VeriKaynagi = "LOCAL"; if (id is null) db.Personeller.Add(x);
    }

    private async Task<string> ResolveId(string module, string code, CancellationToken t) => module switch
    {
        "companies" => await db.Firmalar.Where(x => x.FirmaKodu == code).Select(x => x.FirmaId).SingleAsync(t),
        "branches" => await db.Subeler.Where(x => x.SubeKodu == code).Select(x => x.SubeId).FirstAsync(t),
        "departments" => await db.Departmanlar.Where(x => x.DepartmanKodu == code).Select(x => x.DepartmanId).FirstAsync(t),
        "work-centers" => await db.UretimHatlari.Where(x => x.HatKodu == code).Select(x => x.HatId).FirstAsync(t),
        "components" => (await db.MakineBilesenler.Where(x => x.BilesenKodu == code).Select(x => x.BilesenId).FirstAsync(t)).ToString(),
        "failure-codes" => (await db.ArizaKoduTanimlari.Where(x => x.ArizaKodu == code).Select(x => x.ArizaKoduId).SingleAsync(t)).ToString(),
        "maintenance-types" => (await db.BakimTuruTanimlari.Where(x => x.BakimTuruKodu == code).Select(x => x.BakimTuruId).SingleAsync(t)).ToString(),
        "checklists" => (await db.BakimKontrolListeleri.Where(x => x.KontrolListesiKodu == code).Select(x => x.KontrolListesiId).SingleAsync(t)).ToString(),
        "risk-rules" => (await db.RiskKuralSetleri.Where(x => x.KuralSetiKodu == code).OrderByDescending(x => x.KuralSetiId).Select(x => x.KuralSetiId).FirstAsync(t)).ToString(),
        "hybrid-parameters" => (await db.HibritKararParametreleri.Where(x => x.SetKodu == code).Select(x => x.HibritSetId).SingleAsync(t)).ToString(),
        "maintenance-recommendations" => (await db.BakimOneriParametreleri.Where(x => x.OneriKodu == code).Select(x => x.OneriId).SingleAsync(t)).ToString(),
        "counters" => (await db.MakineSayacTanimlari.Where(x => x.SayacKodu == code).Select(x => x.SayacTanimId).SingleAsync(t)).ToString(),
        "shifts" => (await db.Vardiyalar.Where(x => x.VardiyaKodu == code).Select(x => x.VardiyaId).SingleAsync(t)).ToString(),
        "teams" => (await db.Ekipler.Where(x => x.EkipKodu == code).Select(x => x.EkipId).SingleAsync(t)).ToString(),
        "personnel" => await db.Personeller.Where(x => x.PersonelId == code).Select(x => x.PersonelId).SingleAsync(t),
        _ => throw new DomainValidationException("Geçersiz tanım modülü.")
    };

    private static LocalDefinitionRow Row(string id, string? code, string? name, bool active, string? parentId, string? parentName, string? type, string? description, IReadOnlyDictionary<string, string?>? details) => new(id, code ?? "", name ?? "", active, parentId, parentName, type, description, details ?? new Dictionary<string, string?>());
    private static void Normalize(LocalDefinitionUpsertRequest r) { r.Code = r.Code.Trim().ToUpperInvariant(); r.Name = r.Name.Trim(); if (string.IsNullOrWhiteSpace(r.Code) || string.IsNullOrWhiteSpace(r.Name)) throw new DomainValidationException("Kod ve ad zorunludur."); }
    private static void RequireParent(LocalDefinitionUpsertRequest r) { if (string.IsNullOrWhiteSpace(r.ParentId)) throw new DomainValidationException("Üst kayıt seçimi zorunludur."); }
    private static int IntId(string id) => int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : throw new DomainValidationException("Geçersiz kayıt kimliği.");
    private static string NewId(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..20];
    private static Exception Duplicate(string name) => new ConflictException($"Bu {name} kodu zaten kullanılmaktadır.");
    private static Exception NotFound() => new ResourceNotFoundException("Kayıt bulunamadı.");
    private static Exception InvalidParent() => new DomainValidationException("Seçilen üst kayıt bulunamadı veya pasif.");
    private static void ValidateModule(string module) { if (!Modules.Any(x => x.Module == module)) throw new ResourceNotFoundException("Tanım modülü bulunamadı."); }
}
