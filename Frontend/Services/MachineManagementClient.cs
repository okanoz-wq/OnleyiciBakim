using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using OnleyiciBakimSistemi.Models.ViewModels;

namespace OnleyiciBakimSistemi.Services;

public interface IMachineManagementClient
{
    Task<MachineManagementPage> GetAsync(MachineManagementQuery query, CancellationToken cancellationToken);
    Task<MachineManagementLookupsViewModel> GetLookupsAsync(CancellationToken cancellationToken);
    Task<ManagedMachineDetailsViewModel> GetDetailsAsync(string id, CancellationToken cancellationToken);
    Task<ManagementOperationViewModel> CreateAsync(MachineFormViewModel model, CancellationToken cancellationToken);
    Task<ManagementOperationViewModel> UpdateAsync(string id, MachineFormViewModel model, CancellationToken cancellationToken);
    Task<ManagementOperationViewModel> DeactivateAsync(string id, CancellationToken cancellationToken);
    Task<ManagementOperationViewModel> ActivateAsync(string id, CancellationToken cancellationToken);
}

public sealed class MachineManagementClient(HttpClient httpClient) : IMachineManagementClient
{
    private const string BasePath = "api/v1/bakim-yonetimi/management/machines";

    public async Task<MachineManagementPage> GetAsync(MachineManagementQuery query, CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, string?>
        {
            ["search"] = query.Search, ["status"] = query.Status,
            ["criticality"] = query.Criticality, ["sourceSystem"] = query.SourceSystem,
            ["sortBy"] = query.SortBy, ["descending"] = query.Descending.ToString().ToLowerInvariant(),
            ["page"] = query.Page.ToString(), ["pageSize"] = query.PageSize.ToString()
        };
        return await GetRequiredAsync<MachineManagementPage>(
            QueryHelpers.AddQueryString(BasePath, values), cancellationToken);
    }

    public Task<MachineManagementLookupsViewModel> GetLookupsAsync(CancellationToken cancellationToken) =>
        GetRequiredAsync<MachineManagementLookupsViewModel>($"{BasePath}/lookups", cancellationToken);

    public Task<ManagedMachineDetailsViewModel> GetDetailsAsync(string id, CancellationToken cancellationToken) =>
        GetRequiredAsync<ManagedMachineDetailsViewModel>($"{BasePath}/{Uri.EscapeDataString(id)}", cancellationToken);

    public Task<ManagementOperationViewModel> CreateAsync(MachineFormViewModel model, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Post, BasePath, ToRequest(model), cancellationToken);

    public Task<ManagementOperationViewModel> UpdateAsync(string id, MachineFormViewModel model, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Put, $"{BasePath}/{Uri.EscapeDataString(id)}", ToRequest(model), cancellationToken);

    public Task<ManagementOperationViewModel> DeactivateAsync(string id, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Delete, $"{BasePath}/{Uri.EscapeDataString(id)}", null, cancellationToken);

    public Task<ManagementOperationViewModel> ActivateAsync(string id, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Post, $"{BasePath}/{Uri.EscapeDataString(id)}/activate", null, cancellationToken);

    private async Task<T> GetRequiredAsync<T>(string uri, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(uri, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
               ?? throw new BackendManagementException("Sunucudan geçerli bir yanıt alınamadı.");
    }

    private async Task<ManagementOperationViewModel> SendAsync(
        HttpMethod method, string uri, object? body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, uri);
        if (body is not null) request.Content = JsonContent.Create(body);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ManagementOperationViewModel>(cancellationToken: cancellationToken)
               ?? throw new BackendManagementException("İşlem sonucu alınamadı.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        var message = "İşlem tamamlanamadı.";
        try
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = json.RootElement;
            if (root.TryGetProperty("detail", out var detail) && !string.IsNullOrWhiteSpace(detail.GetString()))
                message = detail.GetString()!;
            else if (root.TryGetProperty("title", out var title) && !string.IsNullOrWhiteSpace(title.GetString()))
                message = title.GetString()!;
            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                var first = errors.EnumerateObject().SelectMany(x => x.Value.EnumerateArray())
                    .Select(x => x.GetString()).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
                if (first is not null) message = first;
            }
        }
        catch (JsonException) { }
        throw new BackendManagementException(message, (int)response.StatusCode);
    }

    private static object ToRequest(MachineFormViewModel model) => new
    {
        model.Code, model.Name, model.CompanyId, model.BranchId, model.DepartmentId,
        model.ProductionLineId, model.Model, model.Age, model.Criticality,
        model.InstallationYear, model.Status, model.LastMaintenanceDate
    };
}

public sealed class BackendManagementException(string message, int? statusCode = null) : Exception(message)
{
    public int? StatusCode { get; } = statusCode;
}

public sealed class MachineManagementQuery
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? Criticality { get; set; }
    public string? SourceSystem { get; set; }
    public string SortBy { get; set; } = "risk";
    public bool Descending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = PaginationViewModel.DefaultPageSize;
}

public sealed class MachineManagementPage
{
    public List<ManagedMachineItemViewModel> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public sealed class ManagementOperationViewModel
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Id { get; set; }
    public bool Deactivated { get; set; }
}
