using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using OnleyiciBakim.Contracts.Integrations;
using OnleyiciBakim.Contracts.Machines;
using OnleyiciBakim.Options;

namespace OnleyiciBakim.Integrations.Erp;

public sealed record ErpWorkOrderRequest(
    string MaintenancePlanId,
    string MachineId,
    DateTimeOffset PlannedAtUtc,
    string MaintenanceType,
    string Priority,
    string? Description,
    string IdempotencyKey);

public sealed record ErpWorkOrderResult(
    bool IsSuccess,
    string? ExternalWorkOrderId,
    string Status,
    int? HttpStatusCode = null,
    string? ErrorCode = null,
    string? ErrorMessage = null);

public interface IErpClient
{
    Task<IReadOnlyList<CreateMachineRequest>> GetMachinesAsync(CancellationToken cancellationToken);
    Task<ErpWorkOrderResult> CreateWorkOrderAsync(
        ErpWorkOrderRequest request,
        CancellationToken cancellationToken);
    Task<bool> IsReadyAsync(CancellationToken cancellationToken);
}

public sealed class HttpErpClient(
    HttpClient httpClient,
    IOptions<ErpOptions> options,
    ILogger<HttpErpClient> logger) : IErpClient
{
    private readonly ErpOptions _options = options.Value;

    public async Task<IReadOnlyList<CreateMachineRequest>> GetMachinesAsync(CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Get, "/api/machines");
        AddApiKey(message);
        using var response = await httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<CreateMachineRequest>>(
            cancellationToken: cancellationToken) ?? [];
    }

    public async Task<ErpWorkOrderResult> CreateWorkOrderAsync(
        ErpWorkOrderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, "/api/work-orders")
            {
                Content = JsonContent.Create(request)
            };
            AddApiKey(message);
            message.Headers.TryAddWithoutValidation("Idempotency-Key", request.IdempotencyKey);
            using var response = await httpClient.SendAsync(message, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new(false, null, "Failed", (int)response.StatusCode,
                    $"http-{(int)response.StatusCode}", "ERP iş emri oluşturulamadı.");
            var payload = await response.Content.ReadFromJsonAsync<WorkOrderApiResponse>(
                cancellationToken: cancellationToken);
            return new(true, payload?.Id, payload?.Status ?? "Created", (int)response.StatusCode);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(exception, "ERP iş emri gönderilemedi. PlanId={PlanId}",
                request.MaintenancePlanId);
            return new(false, null, "Pending", null, "unavailable",
                "ERP servisine ulaşılamadı; işlem outbox üzerinden yeniden denenecek.");
        }
    }

    public async Task<bool> IsReadyAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, "/health");
            AddApiKey(message);
            using var response = await httpClient.SendAsync(message, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private void AddApiKey(HttpRequestMessage message)
    {
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            message.Headers.TryAddWithoutValidation("X-Api-Key", _options.ApiKey);
    }

    private sealed record WorkOrderApiResponse(string Id, string Status);
}

public sealed class MockErpClient : IErpClient
{
    public Task<IReadOnlyList<CreateMachineRequest>> GetMachinesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<CreateMachineRequest> machines =
        [
            new()
            {
                Id = "MKN-ERP-001", ExternalId = "EQ-1001", Code = "MKN-ERP-001",
                Name = "ERP Demo CNC", Type = "CNC", Criticality = "Yüksek",
                Status = "Aktif", DataSource = "MockERP"
            }
        ];
        return Task.FromResult(machines);
    }

    public Task<ErpWorkOrderResult> CreateWorkOrderAsync(
        ErpWorkOrderRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new ErpWorkOrderResult(
            true, $"MOCK-WO-{request.MaintenancePlanId}", "Created", 201));

    public Task<bool> IsReadyAsync(CancellationToken cancellationToken) => Task.FromResult(true);
}
