namespace OnleyiciBakim.Domain.Entities;

public sealed class Company : BaseEntity
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public string? DataSource { get; set; }
    public ICollection<Branch> Branches { get; set; } = [];
}

public sealed class Branch : BaseEntity
{
    public string Id { get; set; } = null!;
    public string CompanyId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public string? DataSource { get; set; }
    public Company Company { get; set; } = null!;
    public ICollection<Department> Departments { get; set; } = [];
}

public sealed class Department : BaseEntity
{
    public string Id { get; set; } = null!;
    public string BranchId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public string? DataSource { get; set; }
    public Branch Branch { get; set; } = null!;
    public ICollection<ProductionLine> ProductionLines { get; set; } = [];
}

public sealed class ProductionLine : BaseEntity
{
    public string Id { get; set; } = null!;
    public string DepartmentId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public int ShiftCount { get; set; }
    public bool IsActive { get; set; } = true;
    public string? DataSource { get; set; }
    public Department Department { get; set; } = null!;
    public ICollection<Machine> Machines { get; set; } = [];
}

public sealed class Personnel : BaseEntity
{
    public string Id { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string? Specialization { get; set; }
    public bool IsActive { get; set; } = true;
    public string? DataSource { get; set; }
}
