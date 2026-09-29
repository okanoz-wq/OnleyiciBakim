using System.Net;
using System.Net.Http.Json;
using OnleyiciBakimSistemi.Models.ViewModels;

namespace OnleyiciBakimSistemi.Services;

public interface ILocalDefinitionClient
{
    Task<IReadOnlyList<DefinitionMetadataViewModel>> ModulesAsync(CancellationToken token);
    Task<IReadOnlyList<DefinitionRowViewModel>> ListAsync(string module, bool inactive, CancellationToken token);
    Task<DefinitionRowViewModel> GetAsync(string module, string id, CancellationToken token);
    Task SaveAsync(DefinitionFormViewModel form, CancellationToken token);
    Task DeactivateAsync(string module, string id, CancellationToken token);
}

public sealed class LocalDefinitionClient(HttpClient http) : ILocalDefinitionClient
{
    public async Task<IReadOnlyList<DefinitionMetadataViewModel>> ModulesAsync(CancellationToken token) =>
        await http.GetFromJsonAsync<List<DefinitionMetadataViewModel>>("api/v1/local-definitions/modules", token) ?? [];

    public async Task<IReadOnlyList<DefinitionRowViewModel>> ListAsync(string module, bool inactive, CancellationToken token) =>
        await http.GetFromJsonAsync<List<DefinitionRowViewModel>>($"api/v1/local-definitions/{Uri.EscapeDataString(module)}?includeInactive={inactive.ToString().ToLowerInvariant()}", token) ?? [];

    public async Task<DefinitionRowViewModel> GetAsync(string module, string id, CancellationToken token) =>
        await http.GetFromJsonAsync<DefinitionRowViewModel>($"api/v1/local-definitions/{Uri.EscapeDataString(module)}/{Uri.EscapeDataString(id)}", token)
        ?? throw new DefinitionClientException("Kayıt bulunamadı.");

    public async Task SaveAsync(DefinitionFormViewModel form, CancellationToken token)
    {
        var url = $"api/v1/local-definitions/{Uri.EscapeDataString(form.Module)}" +
                  (form.Id is null ? "" : "/" + Uri.EscapeDataString(form.Id));
        var payload = new
        {
            form.Code, form.Name, form.ParentId, form.Type, form.Category, form.Severity,
            form.Unit, form.Version, form.MachineType, form.Role, form.EstimatedLife,
            form.StartTime, form.EndTime,
            form.Minimum, form.Maximum, form.RecommendedDays, form.AiWeight, form.RuleWeight,
            form.Active, form.Flag, form.MaxIncluded, form.Description, form.Items
        };
        using var response = form.Id is null
            ? await http.PostAsJsonAsync(url, payload, token)
            : await http.PutAsJsonAsync(url, payload, token);
        await EnsureSuccess(response, token);
    }

    public async Task DeactivateAsync(string module, string id, CancellationToken token)
    {
        using var response = await http.DeleteAsync($"api/v1/local-definitions/{Uri.EscapeDataString(module)}/{Uri.EscapeDataString(id)}", token);
        await EnsureSuccess(response, token);
    }

    private static async Task EnsureSuccess(HttpResponseMessage response, CancellationToken token)
    {
        if (response.IsSuccessStatusCode) return;
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsPayload>(cancellationToken: token);
        throw new DefinitionClientException(problem?.Detail ?? problem?.Title ??
            (response.StatusCode == HttpStatusCode.Conflict ? "Kayıt başka bir kayıtla çakışıyor." : "İşlem tamamlanamadı."));
    }

    private sealed record ProblemDetailsPayload(string? Title, string? Detail);
}

public sealed class DefinitionClientException(string message) : Exception(message);
