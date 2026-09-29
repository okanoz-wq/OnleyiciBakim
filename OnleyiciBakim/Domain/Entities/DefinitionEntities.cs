namespace OnleyiciBakim.Domain.Entities;

public sealed class FailureCode : BaseEntity
{
    public string Id { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string DefaultSeverity { get; set; } = "Orta";
    public bool IsActive { get; set; } = true;
}

public sealed class MaintenanceTypeDefinition : BaseEntity
{
    public string Id { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int? DefaultPeriodDays { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class MaintenanceChecklist : BaseEntity
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? MachineType { get; set; }
    public string MaintenanceTypeId { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public ICollection<MaintenanceChecklistItem> Items { get; set; } = [];
    public MaintenanceTypeDefinition MaintenanceType { get; set; } = null!;
}

public sealed class MaintenanceChecklistItem : BaseEntity
{
    public long Id { get; set; }
    public string ChecklistId { get; set; } = null!;
    public int Order { get; set; }
    public string Description { get; set; } = null!;
    public bool IsRequired { get; set; } = true;
    public MaintenanceChecklist Checklist { get; set; } = null!;
}

public sealed class ApplicationParameter : BaseEntity
{
    public string Id { get; set; } = null!;
    public string Group { get; set; } = null!;
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
    public string DataType { get; set; } = "decimal";
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
