using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using OnleyiciBakim.Integrations.Ml;
using OnleyiciBakim.Integrations.Erp;
using OnleyiciBakim.Data;
using OnleyiciBakim.Domain.Enums;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace OnleyiciBakim.Tests;

public sealed class ApiIntegrationTests
{
    [Fact]
    public async Task LiveHealth_ReturnsSuccess()
    {
        await using var factory = new TestApplicationFactory("Test");
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SeededMachine_CanRunEndToEndHybridAnalysis()
    {
        await using var factory = new TestApplicationFactory("Test");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>().SeedAsync();
        }
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/v1/risk-analyses/machines/DEV-MKN-001",
            new { mockScenario = "success" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(document.RootElement.GetProperty("hybridRisk").GetProperty("score").GetDecimal() >= 0m);
        Assert.Equal("Success", document.RootElement.GetProperty("mlStatus").GetString());
        Assert.False(document.RootElement.GetProperty("isFallback").GetBoolean());
    }

    [Fact]
    public async Task MlTimeout_FallsBackToRuleScoreWithoutCrashing()
    {
        await using var factory = new TestApplicationFactory("Test");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>().SeedAsync();
        }
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/v1/risk-analyses/machines/DEV-MKN-001",
            new { mockScenario = "timeout" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(document.RootElement.GetProperty("isFallback").GetBoolean());
        Assert.Equal(0m, document.RootElement.GetProperty("machineLearningWeight").GetDecimal());
    }

    [Fact]
    public async Task Recalculation_UpdatesSingleActivePlanInsteadOfCreatingDuplicate()
    {
        await using var factory = new TestApplicationFactory("Test");
        await SeedAsync(factory);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/risk-analyses/machines/DEV-MKN-003",
            new { mockScenario = "success" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var activePlans = await db.MaintenancePlans.CountAsync(x =>
            x.MachineId == "DEV-MKN-003" &&
            x.Status != MaintenancePlanStatus.Completed &&
            x.Status != MaintenancePlanStatus.Cancelled);
        var plan = await db.MaintenancePlans.SingleAsync(x => x.MachineId == "DEV-MKN-003");
        Assert.Equal(1, activePlans);
        Assert.NotNull(plan.RiskAssessmentId);
        var assessment = await db.RiskAssessments.SingleAsync(x => x.Id == plan.RiskAssessmentId);
        Assert.Equal(assessment.RiskLevel.ToString(), plan.Priority switch
        {
            "Düşük" => RiskLevel.Low.ToString(),
            "Orta" => RiskLevel.Medium.ToString(),
            "Yüksek" => RiskLevel.High.ToString(),
            "Kritik" => RiskLevel.Critical.ToString(),
            _ => string.Empty
        });
    }

    [Fact]
    public async Task Failure_OpenInterveneClose_FlowCompletes()
    {
        await using var factory = new TestApplicationFactory("Test");
        await SeedAsync(factory);
        using var client = factory.CreateClient();
        var create = await client.PostAsJsonAsync("/api/v1/failures", new
        {
            id = "TEST-FLR-100",
            machineId = "DEV-MKN-003",
            occurredAt = DateTimeOffset.UtcNow,
            failureType = "Elektrik",
            severity = "Orta"
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var intervene = await client.PostAsJsonAsync(
            "/api/v1/failures/TEST-FLR-100/interventions",
            new { id = "TEST-INT-100", note = "Kontrol başlatıldı." });
        Assert.Equal(HttpStatusCode.Created, intervene.StatusCode);
        var close = await client.PostAsJsonAsync(
            "/api/v1/failures/TEST-FLR-100/close",
            new { resolution = "Elektrik bağlantısı yenilendi.", downtimeMinutes = 30 });
        Assert.Equal(HttpStatusCode.NoContent, close.StatusCode);
    }

    [Fact]
    public async Task Maintenance_StartComplete_FlowCompletes()
    {
        await using var factory = new TestApplicationFactory("Test");
        await SeedAsync(factory);
        using var client = factory.CreateClient();
        var start = await client.PostAsJsonAsync(
            "/api/v1/maintenance-executions/plans/DEV-PLN-001/start",
            new { executionId = "TEST-EXE-100", personnelId = "DEV-PER-1" });
        Assert.Equal(HttpStatusCode.Created, start.StatusCode);
        var complete = await client.PostAsJsonAsync(
            "/api/v1/maintenance-executions/TEST-EXE-100/complete",
            new { result = "Tamamlandı.", durationMinutes = 60, cost = 250m });
        Assert.Equal(HttpStatusCode.NoContent, complete.StatusCode);
    }

    [Fact]
    public async Task InvalidFailureRequest_ReturnsValidationProblem()
    {
        await using var factory = new TestApplicationFactory("Test");
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/failures", new { });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DevTest_IsNotAvailableInProduction()
    {
        await using var factory = new TestApplicationFactory("Production");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var response = await client.GetAsync("/dev-test");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task SeedAsync(TestApplicationFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>().SeedAsync();
    }

    private sealed class TestApplicationFactory(string environment)
        : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"tests-{Guid.NewGuid():N}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environment);
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:Provider"] = environment == "Production" ? "PostgreSql" : "InMemory",
                    ["MlService:UseMock"] = environment == "Production" ? "false" : "true",
                    ["Erp:UseMock"] = environment == "Production" ? "false" : "true",
                    ["Erp:EnableBackgroundSync"] = "false",
                    ["RiskAutomation:Enabled"] = "false",
                    ["ConnectionStrings:PostgreSql"] =
                        "Host=localhost;Port=5432;Database=test;Username=test;Password=test"
                });
            });
            if (environment != "Production")
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                    services.RemoveAll<ApplicationDbContext>();
                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseInMemoryDatabase(_databaseName));
                    services.RemoveAll<IMlPredictionClient>();
                    services.AddSingleton<IMlPredictionClient, MockMlPredictionClient>();
                    services.RemoveAll<IErpClient>();
                    services.AddSingleton<IErpClient, MockErpClient>();
                });
            }
        }
    }
}
