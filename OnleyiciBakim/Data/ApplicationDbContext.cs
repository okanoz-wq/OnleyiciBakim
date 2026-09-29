using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using OnleyiciBakim.Domain.Entities;
using OnleyiciBakim.Domain.Enums;

namespace OnleyiciBakim.Data;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IHttpContextAccessor? httpContextAccessor = null)
    : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<ProductionLine> ProductionLines => Set<ProductionLine>();
    public DbSet<Personnel> Personnel => Set<Personnel>();
    public DbSet<Machine> Machines => Set<Machine>();
    public DbSet<MachineComponent> MachineComponents => Set<MachineComponent>();
    public DbSet<FailureRecord> FailureRecords => Set<FailureRecord>();
    public DbSet<FailureIntervention> FailureInterventions => Set<FailureIntervention>();
    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();
    public DbSet<ErrorRecord> ErrorRecords => Set<ErrorRecord>();
    public DbSet<DailyTelemetry> DailyTelemetry => Set<DailyTelemetry>();
    public DbSet<MaintenancePlan> MaintenancePlans => Set<MaintenancePlan>();
    public DbSet<MaintenancePlanTask> MaintenancePlanTasks => Set<MaintenancePlanTask>();
    public DbSet<MaintenanceExecution> MaintenanceExecutions => Set<MaintenanceExecution>();
    public DbSet<MaintenanceExecutionChecklistResult> MaintenanceExecutionChecklistResults => Set<MaintenanceExecutionChecklistResult>();
    public DbSet<ReplacedPart> ReplacedParts => Set<ReplacedPart>();
    public DbSet<RiskAssessment> RiskAssessments => Set<RiskAssessment>();
    public DbSet<RiskFactorResultEntity> RiskFactorResults => Set<RiskFactorResultEntity>();
    public DbSet<MlPredictionRecord> MlPredictionRecords => Set<MlPredictionRecord>();
    public DbSet<DecisionHistory> DecisionHistory => Set<DecisionHistory>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<FailureCode> FailureCodes => Set<FailureCode>();
    public DbSet<MaintenanceTypeDefinition> MaintenanceTypes => Set<MaintenanceTypeDefinition>();
    public DbSet<MaintenanceChecklist> MaintenanceChecklists => Set<MaintenanceChecklist>();
    public DbSet<MaintenanceChecklistItem> MaintenanceChecklistItems => Set<MaintenanceChecklistItem>();
    public DbSet<ApplicationParameter> ApplicationParameters => Set<ApplicationParameter>();
    public DbSet<IntegrationSyncLog> IntegrationSyncLogs => Set<IntegrationSyncLog>();
    public DbSet<ErpWorkOrderMapping> ErpWorkOrderMappings => Set<ErpWorkOrderMapping>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureOrganization(modelBuilder);
        ConfigureMachines(modelBuilder);
        ConfigureMaintenance(modelBuilder);
        ConfigureRisk(modelBuilder);
        ConfigureDefinitions(modelBuilder);
        ApplySnakeCaseNames(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entries = ChangeTracker.Entries<BaseEntity>().ToArray();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        foreach (var entry in entries.Where(x =>
                     x.Entity is not AuditLog &&
                     x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted &&
                     IsAuditedEntity(x.Entity)))
        {
            var id = entry.Metadata.FindPrimaryKey()?.Properties
                .Select(property => entry.Property(property.Name).CurrentValue?.ToString())
                .FirstOrDefault() ?? "unknown";
            var changedProperties = entry.State == EntityState.Modified
                ? entry.Properties.Where(x => x.IsModified).Select(x => x.Metadata.Name).ToArray()
                : [];
            AuditLogs.Add(new AuditLog
            {
                EntityType = entry.Metadata.ClrType.Name,
                EntityId = id,
                Action = entry.State.ToString(),
                UserId = httpContextAccessor?.HttpContext?.User.Identity?.Name,
                ChangesJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    changedProperties
                }),
                CorrelationId = httpContextAccessor?.HttpContext?.TraceIdentifier,
                OccurredAtUtc = now
            });
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    private static bool IsAuditedEntity(BaseEntity entity) =>
        entity is FailureRecord or FailureIntervention or MaintenancePlan or
            MaintenanceExecution or RiskAssessment or Alert or
            OnleyiciBakim.Domain.Entities.DecisionHistory or
            ErpWorkOrderMapping or OutboxMessage or ApplicationParameter;

    private static void ConfigureOrganization(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(30);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Code).HasMaxLength(50);
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(30);
            entity.Property(x => x.CompanyId).HasMaxLength(30);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Code).HasMaxLength(50);
            entity.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            entity.HasOne(x => x.Company).WithMany(x => x.Branches).HasForeignKey(x => x.CompanyId);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(30);
            entity.Property(x => x.BranchId).HasMaxLength(30);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Code).HasMaxLength(50);
            entity.HasIndex(x => new { x.BranchId, x.Code }).IsUnique();
            entity.HasOne(x => x.Branch).WithMany(x => x.Departments).HasForeignKey(x => x.BranchId);
        });

        modelBuilder.Entity<ProductionLine>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(30);
            entity.Property(x => x.DepartmentId).HasMaxLength(30);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Code).HasMaxLength(50);
            entity.HasIndex(x => new { x.DepartmentId, x.Code }).IsUnique();
            entity.HasOne(x => x.Department).WithMany(x => x.ProductionLines).HasForeignKey(x => x.DepartmentId);
        });

        modelBuilder.Entity<Personnel>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(30);
            entity.Property(x => x.FullName).HasMaxLength(200);
            entity.Property(x => x.Role).HasMaxLength(100);
        });
    }

    private static void ConfigureMachines(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Machine>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(30);
            entity.Property(x => x.Code).HasMaxLength(50);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.CurrentRiskScore).HasPrecision(8, 4);
            entity.Property(x => x.WorkingHours).HasPrecision(14, 2);
            entity.Property(x => x.CounterLimitHours).HasPrecision(14, 2);
            entity.Property(x => x.CurrentRiskLevel).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => new { x.CurrentRiskLevel, x.CurrentRiskScore });
            entity.ToTable(table => table.HasCheckConstraint(
                "ck_machine_risk_score",
                "current_risk_score >= 0 AND current_risk_score <= 100"));
            entity.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ProductionLine).WithMany(x => x.Machines).HasForeignKey(x => x.ProductionLineId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MachineComponent>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(30);
            entity.Property(x => x.MachineId).HasMaxLength(30);
            entity.Property(x => x.ExpectedLifeHours).HasPrecision(14, 2);
            entity.Property(x => x.UsedLifeHours).HasPrecision(14, 2);
            entity.HasIndex(x => new { x.MachineId, x.Code }).IsUnique();
            entity.HasOne(x => x.Machine).WithMany(x => x.Components)
                .HasForeignKey(x => x.MachineId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DailyTelemetry>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.MachineId).HasMaxLength(30);
            entity.Property(x => x.VoltageAverage).HasPrecision(12, 4);
            entity.Property(x => x.VoltageStdDev).HasPrecision(12, 4);
            entity.Property(x => x.RotationAverage).HasPrecision(12, 4);
            entity.Property(x => x.RotationStdDev).HasPrecision(12, 4);
            entity.Property(x => x.PressureAverage).HasPrecision(12, 4);
            entity.Property(x => x.PressureStdDev).HasPrecision(12, 4);
            entity.Property(x => x.VibrationAverage).HasPrecision(12, 4);
            entity.Property(x => x.VibrationStdDev).HasPrecision(12, 4);
            entity.Property(x => x.AnomalyScore).HasPrecision(8, 2);
            entity.Property(x => x.ProductionIntensityPercent).HasPrecision(5, 2);
            entity.HasIndex(x => new { x.MachineId, x.Date }).IsUnique();
            entity.HasOne(x => x.Machine).WithMany(x => x.DailyTelemetry).HasForeignKey(x => x.MachineId);
        });
    }

    private static void ConfigureMaintenance(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FailureRecord>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(30);
            entity.Property(x => x.MachineId).HasMaxLength(30);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.EstimatedCost).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.MachineId, x.OccurredAt });
            entity.HasIndex(x => new { x.Status, x.Severity });
            entity.HasOne(x => x.Machine).WithMany(x => x.Failures).HasForeignKey(x => x.MachineId);
            entity.HasOne(x => x.Personnel).WithMany().HasForeignKey(x => x.PersonnelId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<FailureIntervention>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(30);
            entity.Property(x => x.FailureRecordId).HasMaxLength(30);
            entity.HasIndex(x => new { x.FailureRecordId, x.StartedAt });
            entity.HasOne(x => x.FailureRecord).WithMany(x => x.Interventions)
                .HasForeignKey(x => x.FailureRecordId);
            entity.HasOne(x => x.Personnel).WithMany().HasForeignKey(x => x.PersonnelId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MaintenanceRecord>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(30);
            entity.Property(x => x.MachineId).HasMaxLength(30);
            entity.Property(x => x.Cost).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.MachineId, x.PerformedAt });
            entity.HasOne(x => x.Machine).WithMany(x => x.MaintenanceRecords).HasForeignKey(x => x.MachineId);
            entity.HasOne(x => x.Personnel).WithMany().HasForeignKey(x => x.PersonnelId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.FailureRecord).WithMany().HasForeignKey(x => x.FailureRecordId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ErrorRecord>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(30);
            entity.Property(x => x.MachineId).HasMaxLength(30);
            entity.HasIndex(x => new { x.MachineId, x.OccurredAt });
            entity.HasOne(x => x.Machine).WithMany(x => x.ErrorRecords).HasForeignKey(x => x.MachineId);
            entity.HasOne(x => x.FailureRecord).WithMany().HasForeignKey(x => x.FailureRecordId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MaintenancePlan>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(30);
            entity.Property(x => x.MachineId).HasMaxLength(30);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.EstimatedCost).HasPrecision(18, 2);
            entity.Property(x => x.ModelConfidence).HasPrecision(5, 2);
            entity.HasIndex(x => new { x.Status, x.PlannedAt });
            entity.HasOne(x => x.Machine).WithMany(x => x.MaintenancePlans).HasForeignKey(x => x.MachineId);
            entity.HasOne(x => x.ResponsiblePersonnel).WithMany().HasForeignKey(x => x.ResponsiblePersonnelId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.RiskAssessment).WithMany().HasForeignKey(x => x.RiskAssessmentId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MaintenancePlanTask>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.MaintenancePlanId, x.Order }).IsUnique();
            entity.HasOne(x => x.MaintenancePlan).WithMany(x => x.Tasks)
                .HasForeignKey(x => x.MaintenancePlanId);
            entity.HasOne(x => x.ResponsiblePersonnel).WithMany()
                .HasForeignKey(x => x.ResponsiblePersonnelId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MaintenanceExecution>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.Cost).HasPrecision(18, 2);
            entity.HasIndex(x => x.MaintenancePlanId);
            entity.HasOne(x => x.MaintenancePlan).WithMany(x => x.Executions)
                .HasForeignKey(x => x.MaintenancePlanId);
            entity.HasOne(x => x.Personnel).WithMany().HasForeignKey(x => x.PersonnelId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MaintenanceExecutionChecklistResult>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.MaintenanceExecutionId, x.ChecklistItemId }).IsUnique();
            entity.HasOne(x => x.MaintenanceExecution).WithMany(x => x.ChecklistResults)
                .HasForeignKey(x => x.MaintenanceExecutionId);
        });

        modelBuilder.Entity<ReplacedPart>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Quantity).HasPrecision(14, 3);
            entity.HasOne(x => x.MaintenanceExecution).WithMany(x => x.ReplacedParts)
                .HasForeignKey(x => x.MaintenanceExecutionId);
        });
    }

    private static void ConfigureRisk(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RiskAssessment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(40);
            entity.Property(x => x.MachineId).HasMaxLength(30);
            entity.Property(x => x.AlgorithmicScore).HasPrecision(8, 4);
            entity.Property(x => x.MachineLearningScore).HasPrecision(8, 4);
            entity.Property(x => x.HybridScore).HasPrecision(8, 4);
            entity.Property(x => x.AlgorithmWeight).HasPrecision(4, 3);
            entity.Property(x => x.MachineLearningWeight).HasPrecision(4, 3);
            entity.Property(x => x.ModelConfidence).HasPrecision(5, 2);
            entity.Property(x => x.RiskLevel).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.AlgorithmDetailsJson).HasColumnType("jsonb");
            entity.Property(x => x.DataCompletenessPercent).HasPrecision(5, 2);
            entity.HasIndex(x => new { x.MachineId, x.EvaluatedAt });
            entity.HasIndex(x => x.CorrelationId).IsUnique();
            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "ck_risk_scores",
                    "algorithmic_score BETWEEN 0 AND 100 AND (machine_learning_score IS NULL OR machine_learning_score BETWEEN 0 AND 100) AND hybrid_score BETWEEN 0 AND 100");
                table.HasCheckConstraint(
                    "ck_risk_weights",
                    "algorithm_weight >= 0 AND machine_learning_weight >= 0 AND algorithm_weight + machine_learning_weight = 1");
            });
            entity.HasOne(x => x.Machine).WithMany(x => x.RiskAssessments).HasForeignKey(x => x.MachineId);
        });

        modelBuilder.Entity<RiskFactorResultEntity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.NormalizedValue).HasPrecision(8, 4);
            entity.Property(x => x.Weight).HasPrecision(5, 4);
            entity.Property(x => x.Contribution).HasPrecision(8, 4);
            entity.HasIndex(x => new { x.RiskAssessmentId, x.FactorKey }).IsUnique();
            entity.HasOne(x => x.RiskAssessment).WithMany(x => x.Factors)
                .HasForeignKey(x => x.RiskAssessmentId);
        });

        modelBuilder.Entity<MlPredictionRecord>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RiskScore).HasPrecision(8, 4);
            entity.Property(x => x.Confidence).HasPrecision(5, 4);
            entity.Property(x => x.WarningsJson).HasColumnType("jsonb");
            entity.HasIndex(x => x.PredictionId);
            entity.HasOne(x => x.RiskAssessment).WithMany(x => x.MlPredictions)
                .HasForeignKey(x => x.RiskAssessmentId);
        });

        modelBuilder.Entity<DecisionHistory>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DecisionType).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.PreviousScore).HasPrecision(8, 4);
            entity.Property(x => x.FinalScore).HasPrecision(8, 4);
            entity.HasIndex(x => new { x.MachineId, x.DecidedAtUtc });
        });

        modelBuilder.Entity<Alert>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(40);
            entity.Property(x => x.Level).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            entity.HasIndex(x => new { x.Status, x.Level, x.GeneratedAt });
            entity.HasOne(x => x.Machine).WithMany().HasForeignKey(x => x.MachineId);
            entity.HasOne(x => x.RiskAssessment).WithMany(x => x.Alerts).HasForeignKey(x => x.RiskAssessmentId);
            entity.HasOne(x => x.ResponsiblePersonnel).WithMany().HasForeignKey(x => x.ResponsiblePersonnelId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<IntegrationSyncLog>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Direction).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.PayloadJson).HasColumnType("jsonb");
            entity.HasIndex(x => x.CorrelationId);
        });

        modelBuilder.Entity<ErpWorkOrderMapping>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.MaintenancePlanId).IsUnique();
            entity.HasIndex(x => x.IdempotencyKey).IsUnique();
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PayloadJson).HasColumnType("jsonb");
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            entity.HasIndex(x => x.IdempotencyKey).IsUnique();
            entity.HasIndex(x => new { x.Status, x.NextAttemptAtUtc });
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ChangesJson).HasColumnType("jsonb");
            entity.HasIndex(x => new { x.EntityType, x.EntityId, x.OccurredAtUtc });
        });
    }

    private static void ConfigureDefinitions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FailureCode>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
        });
        modelBuilder.Entity<MaintenanceTypeDefinition>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
        });
        modelBuilder.Entity<MaintenanceChecklist>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.MaintenanceType).WithMany().HasForeignKey(x => x.MaintenanceTypeId);
        });
        modelBuilder.Entity<MaintenanceChecklistItem>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ChecklistId, x.Order }).IsUnique();
            entity.HasOne(x => x.Checklist).WithMany(x => x.Items).HasForeignKey(x => x.ChecklistId);
        });
        modelBuilder.Entity<ApplicationParameter>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.Group, x.Key }).IsUnique();
        });
    }

    private static void ApplySnakeCaseNames(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName(ToSnakeCase(entity.GetTableName() ?? entity.ClrType.Name));

            foreach (var property in entity.GetProperties())
            {
                var storeObject = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());
                property.SetColumnName(ToSnakeCase(property.GetColumnName(storeObject) ?? property.Name));
            }

            foreach (var key in entity.GetKeys())
            {
                key.SetName(ToSnakeCase(key.GetName() ?? $"pk_{entity.GetTableName()}"));
            }

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                foreignKey.SetConstraintName(ToSnakeCase(
                    foreignKey.GetConstraintName() ?? $"fk_{entity.GetTableName()}"));
            }

            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(ToSnakeCase(
                    index.GetDatabaseName() ?? $"ix_{entity.GetTableName()}"));
            }
        }
    }

    private static string ToSnakeCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var result = new StringBuilder(value.Length + 10);
        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];
            if (char.IsUpper(current) && i > 0 &&
                (char.IsLower(value[i - 1]) || (i + 1 < value.Length && char.IsLower(value[i + 1]))))
            {
                result.Append('_');
            }
            result.Append(char.ToLowerInvariant(current));
        }
        return result.ToString();
    }
}
