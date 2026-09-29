extern alias frontend;

using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
using BackendMaintenanceDataService = frontend::OnleyiciBakimSistemi.Services.BackendMaintenanceDataService;
using DefinitionFormViewModel = frontend::OnleyiciBakimSistemi.Models.ViewModels.DefinitionFormViewModel;
using DefinitionMetadataViewModel = frontend::OnleyiciBakimSistemi.Models.ViewModels.DefinitionMetadataViewModel;
using DefinitionRowViewModel = frontend::OnleyiciBakimSistemi.Models.ViewModels.DefinitionRowViewModel;
using FrontendProgram = frontend::Program;
using ILocalDefinitionClient = frontend::OnleyiciBakimSistemi.Services.ILocalDefinitionClient;

namespace OnleyiciBakim.Tests;

public sealed class FrontendWorkflowRegressionTests
{
    [Fact]
    public async Task ClosedFailure_WithKapandiStatus_IsShownAsResolved()
    {
        const string json = """
            {
              "items": [{
                "id": "ARZ-1",
                "occurredAt": "2026-08-06T10:00:00+03:00",
                "machineId": "M-1",
                "machineName": "Yeni Makine",
                "failureType": "Motor Arızası",
                "severity": "Yüksek",
                "downtimeHours": 1.5,
                "technician": "Teknisyen",
                "status": "Kapandı"
              }],
              "totalPages": 1
            }
            """;
        using var http = new HttpClient(new JsonHandler(json))
        {
            BaseAddress = new Uri("http://localhost/")
        };
        var service = new BackendMaintenanceDataService(http);

        var failure = Assert.Single(await service.GetFaultsAsync());

        Assert.True(failure.Resolved);
    }

    [Fact]
    public async Task MonthlyFaultChart_RequestsSelectedYearAndTwelveMonths()
    {
        const string json = """
            {
              "totalMachines": 1,
              "highOrCriticalRiskMachines": 0,
              "pendingMaintenancePlans": 0,
              "averageFailureDurationHours": 0,
              "monthlyFailureAnalysis": [
                { "year": 2015, "month": 1, "label": "Oca", "failureCount": 3 }
              ],
              "failureTypeDistribution": [],
              "riskDistribution": [],
              "riskiestMachines": [],
              "generatedAt": "2026-08-06T10:00:00Z"
            }
            """;
        var handler = new JsonHandler(json);
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var service = new BackendMaintenanceDataService(http);

        var point = Assert.Single(await service.GetMonthlyFaultTrendAsync(
            12, 2015, CancellationToken.None));

        Assert.Equal("Oca 2015", point.Month);
        Assert.Equal(3, point.Count);
        Assert.Contains("months=12&year=2015", handler.LastRequestUri?.Query);
    }

    [Fact]
    public async Task ComponentForm_PostsSelectedMachineAndSaves()
    {
        var definitions = new RecordingDefinitionClient();
        await using var factory = new WebApplicationFactory<FrontendProgram>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ILocalDefinitionClient>();
                    services.AddSingleton<ILocalDefinitionClient>(definitions);
                });
            });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        using var createResponse = await client.GetAsync("/Definitions/Create?module=components");
        createResponse.EnsureSuccessStatusCode();
        var html = await createResponse.Content.ReadAsStringAsync();
        var tokenMatch = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
            RegexOptions.CultureInvariant);
        Assert.True(tokenMatch.Success, "Bileşen formunda antiforgery anahtarı bulunamadı.");

        using var saveResponse = await client.PostAsync("/Definitions/Save", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = WebUtility.HtmlDecode(tokenMatch.Groups[1].Value),
                ["Module"] = "components",
                ["ParentId"] = "M-NEW",
                ["Code"] = "BLŞ-NEW",
                ["Name"] = "Yeni Makine Bileşeni",
                ["Type"] = "Mekanik",
                ["Severity"] = "Yüksek",
                ["Active"] = "true"
            }));

        Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);
        Assert.NotNull(definitions.SavedForm);
        Assert.Equal("M-NEW", definitions.SavedForm.ParentId);
        Assert.Equal("BLŞ-NEW", definitions.SavedForm.Code);
    }

    private sealed class JsonHandler(string json) : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class RecordingDefinitionClient : ILocalDefinitionClient
    {
        private static readonly DefinitionMetadataViewModel ComponentMetadata = new(
            "components", "Makine Bileşenleri", "Bileşen Kodu", "Bileşen Adı",
            "machines", "Makine", false);

        public DefinitionFormViewModel? SavedForm { get; private set; }

        public Task<IReadOnlyList<DefinitionMetadataViewModel>> ModulesAsync(CancellationToken token) =>
            Task.FromResult<IReadOnlyList<DefinitionMetadataViewModel>>([ComponentMetadata]);

        public Task<IReadOnlyList<DefinitionRowViewModel>> ListAsync(
            string module,
            bool inactive,
            CancellationToken token) => Task.FromResult<IReadOnlyList<DefinitionRowViewModel>>(
            module == "machines"
                ? [new DefinitionRowViewModel(
                    "M-NEW", "M-NEW", "Yeni Makine", true, null, null, null, null, [])]
                : []);

        public Task<DefinitionRowViewModel> GetAsync(string module, string id, CancellationToken token) =>
            throw new NotSupportedException();

        public Task SaveAsync(DefinitionFormViewModel form, CancellationToken token)
        {
            SavedForm = form;
            return Task.CompletedTask;
        }

        public Task DeactivateAsync(string module, string id, CancellationToken token) =>
            throw new NotSupportedException();
    }
}
