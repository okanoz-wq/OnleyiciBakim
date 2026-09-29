namespace OnleyiciBakim.Options;

public sealed class HybridRiskOptions
{
    public const string SectionName = "HybridRisk";

    public decimal HighCompletenessThreshold { get; set; } = 80m;
    public decimal MinimumCompletenessThreshold { get; set; } = 50m;
    public decimal HighCompletenessAiWeight { get; set; } = 0.40m;
    public decimal HighCompletenessRuleWeight { get; set; } = 0.60m;
    public decimal MediumCompletenessAiWeight { get; set; } = 0.20m;
    public decimal MediumCompletenessRuleWeight { get; set; } = 0.80m;
    public decimal AlgorithmWeight { get; set; } = 0.60m;
    public decimal MachineLearningWeight { get; set; } = 0.40m;
    public MlFailurePolicy MlFailurePolicy { get; set; } = MlFailurePolicy.AlgorithmOnlyFallback;
    public bool AutoCreateMaintenanceDraft { get; set; } = true;
}

public enum MlFailurePolicy
{
    FailClosed,
    AlgorithmOnlyFallback
}

public sealed class AlgorithmicRiskOptions
{
    public const string SectionName = "AlgorithmicRisk";

    public decimal FailureFrequencyWeight { get; set; } = 0.25m;
    public decimal MaintenanceAgeWeight { get; set; } = 0.20m;
    public decimal DowntimeWeight { get; set; } = 0.15m;
    public decimal RepeatedFailureWeight { get; set; } = 0.15m;
    public decimal TelemetryAnomalyWeight { get; set; } = 0.15m;
    public decimal CriticalityWeight { get; set; } = 0.10m;
    public int FailureCountMaximum { get; set; } = 5;
    public int MaintenanceAgeMaximumDays { get; set; } = 180;
    public decimal DowntimeMaximumHours { get; set; } = 24m;
    public int RepeatedFailureMaximum { get; set; } = 3;
    public decimal AnomalyScoreMaximum { get; set; } = 10m;
    public decimal CriticalAnomalyThreshold { get; set; } = 8m;
    public string RuleSetVersion { get; set; } = "rule-based-v2";
}

public sealed class MlServiceOptions
{
    public const string SectionName = "MlService";

    public string BaseUrl { get; set; } = "http://127.0.0.1:5001";
    public string PredictionEndpoint { get; set; } = "/api/v1/predict";
    public int TimeoutSeconds { get; set; } = 15;
    public string ApiKey { get; set; } = string.Empty;
    public int RetryCount { get; set; } = 2;
    public bool UseMock { get; set; }
}

public sealed class ErpOptions
{
    public const string SectionName = "Erp";

    public string Provider { get; set; } = "Generic";
    public string BaseUrl { get; set; } = "https://erp.example";
    public int TimeoutSeconds { get; set; } = 30;
    public string ApiKey { get; set; } = string.Empty;
    public int RetryCount { get; set; } = 2;
    public bool UseMock { get; set; }
    public bool EnableBackgroundSync { get; set; }
    public int OutboxPollingSeconds { get; set; } = 30;
}

public sealed class ErpIntegrationOptions
{
    public const string SectionName = "ErpIntegration";

    public bool Enabled { get; set; }
    public bool UseErpAsMasterSource { get; set; }
}

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string Provider { get; set; } = "PostgreSql";
    public bool SeedDevelopmentData { get; set; }
    public bool UseBakimYonetimiReadModel { get; set; }
}

public sealed class SwaggerOptions
{
    public const string SectionName = "Swagger";
    public bool Enabled { get; set; }
}

public sealed class MaintenanceRecommendationOptions
{
    public const string SectionName = "MaintenancePlanning";
    public int LowDays { get; set; } = 60;
    public int MediumDays { get; set; } = 21;
    public int HighDays { get; set; } = 7;
    public int CriticalDays { get; set; } = 3;
    public string LowType { get; set; } = "Periyodik";
    public string MediumType { get; set; } = "Planlı";
    public string HighType { get; set; } = "Acil";
    public string CriticalType { get; set; } = "Acil müdahale";
}

public sealed class RiskAutomationOptions
{
    public const string SectionName = "RiskAutomation";
    public bool Enabled { get; set; }
    public int IntervalHours { get; set; } = 24;
    public int BatchSize { get; set; } = 100;
}

public sealed class IntegrationOptions
{
    public const string SectionName = "Integrations";

    public string ApiKey { get; set; } = string.Empty;
    public string MachineLearningModelName { get; set; } = "predictive-maintenance";
    public string ErpSystemName { get; set; } = "ERP";
}
