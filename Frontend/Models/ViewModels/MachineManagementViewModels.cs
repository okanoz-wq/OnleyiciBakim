using System.ComponentModel.DataAnnotations;

namespace OnleyiciBakimSistemi.Models.ViewModels;

public sealed class ManagedMachineItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Criticality { get; set; } = string.Empty;
    public int InstallationYear { get; set; }
    public DateTimeOffset? LastMaintenanceAt { get; set; }
    public decimal RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string RiskColorKey { get; set; } = string.Empty;
    public string SourceSystem { get; set; } = string.Empty;
    public bool CanManage { get; set; }
    public int RelatedRecordCount { get; set; }
}

public sealed class MachineFormViewModel : IValidatableObject
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "Makine kodu zorunludur."), StringLength(50)]
    [Display(Name = "Makine Kodu")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Makine adı zorunludur."), StringLength(200)]
    [Display(Name = "Makine Adı")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Firma seçimi zorunludur.")]
    [Display(Name = "Firma")]
    public string CompanyId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şube seçimi zorunludur.")]
    [Display(Name = "Şube")]
    public string BranchId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Departman seçimi zorunludur.")]
    [Display(Name = "Departman")]
    public string DepartmentId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Üretim hattı seçimi zorunludur.")]
    [Display(Name = "Üretim Hattı")]
    public string ProductionLineId { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Model { get; set; }

    [Range(0, 200, ErrorMessage = "Makine yaşı 0-200 arasında olmalıdır.")]
    [Display(Name = "Makine Yaşı")]
    public int Age { get; set; }

    [Required(ErrorMessage = "Kritiklik seviyesi zorunludur.")]
    [Display(Name = "Kritiklik")]
    public string Criticality { get; set; } = "Orta";

    [Range(1900, 2200, ErrorMessage = "Kurulum yılı geçerli değildir.")]
    [Display(Name = "Kurulum Yılı")]
    public int InstallationYear { get; set; } = DateTime.Today.Year;

    [Required(ErrorMessage = "Durum zorunludur.")]
    public string Status { get; set; } = "Aktif";

    [DataType(DataType.Date)]
    [Display(Name = "Son Bakım Tarihi")]
    public DateTime? LastMaintenanceDate { get; set; }

    public MachineManagementLookupsViewModel Lookups { get; set; } = new();
    public bool IsEdit => !string.IsNullOrWhiteSpace(Id);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (InstallationYear > DateTime.Today.Year)
            yield return new("Kurulum yılı gelecek bir yıl olamaz.", [nameof(InstallationYear)]);
        if (LastMaintenanceDate?.Date > DateTime.Today)
            yield return new("Son bakım tarihi gelecek bir tarih olamaz.", [nameof(LastMaintenanceDate)]);
    }
}

public sealed class ManagementLookupItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ParentId { get; set; }
}

public sealed class MachineManagementLookupsViewModel
{
    public List<ManagementLookupItemViewModel> Companies { get; set; } = [];
    public List<ManagementLookupItemViewModel> Branches { get; set; } = [];
    public List<ManagementLookupItemViewModel> Departments { get; set; } = [];
    public List<ManagementLookupItemViewModel> ProductionLines { get; set; } = [];
    public List<string> Criticalities { get; set; } = [];
    public List<string> Statuses { get; set; } = [];
    public bool UseErpAsMasterSource { get; set; }
}

public sealed class ManagedMachineDetailsViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Model { get; set; }
    public int Age { get; set; }
    public int InstallationYear { get; set; }
    public string Criticality { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? LastMaintenanceAt { get; set; }
    public string CompanyId { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string BranchId { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public string DepartmentId { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string ProductionLineId { get; set; } = string.Empty;
    public string ProductionLine { get; set; } = string.Empty;
    public decimal RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string SourceSystem { get; set; } = string.Empty;
    public string? ErpReferenceId { get; set; }
    public bool CanManage { get; set; }
    public bool HasRelatedRecords { get; set; }
    public int ComponentCount { get; set; }
    public int FailureCount { get; set; }
    public int MaintenanceCount { get; set; }
    public int TelemetryCount { get; set; }
    public int RiskAnalysisCount { get; set; }
    public List<ManagedMachineComponentViewModel> Components { get; set; } = [];
    public List<ManagedMachineHistoryViewModel> RecentFailures { get; set; } = [];
    public List<ManagedMachineHistoryViewModel> RecentMaintenances { get; set; } = [];
    public List<ManagedMachineTelemetryViewModel> RecentTelemetry { get; set; } = [];
}

public sealed class ManagedMachineComponentViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Criticality { get; set; }
    public bool IsActive { get; set; }
}

public sealed class ManagedMachineHistoryViewModel
{
    public string Id { get; set; } = string.Empty;
    public DateTimeOffset Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class ManagedMachineTelemetryViewModel
{
    public DateTimeOffset Date { get; set; }
    public decimal AnomalyScore { get; set; }
    public decimal ProductionIntensity { get; set; }
    public decimal VibrationAverage { get; set; }
}

public sealed class MachineDeleteViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool HasRelatedRecords { get; set; }
    public int RelatedRecordCount { get; set; }
    public string Status { get; set; } = string.Empty;
}
