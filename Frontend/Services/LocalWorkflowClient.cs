using System.Net.Http.Json;

namespace OnleyiciBakimSistemi.Services;

public interface ILocalWorkflowClient { Task<string> PostAsync(string endpoint, object request, CancellationToken token); }

public sealed class LocalWorkflowClient(HttpClient http) : ILocalWorkflowClient
{
    public async Task<string> PostAsync(string endpoint, object request, CancellationToken token)
    {
        using var response = await http.PostAsJsonAsync("api/v1/local-workflows/" + endpoint, request, token);
        if (response.IsSuccessStatusCode) return "İşlem yerel SQL Server'a kaydedildi.";
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(cancellationToken: token);
        throw new LocalWorkflowClientException(problem?.Detail ?? problem?.Title ?? "İşlem tamamlanamadı.");
    }
    private sealed record ProblemPayload(string? Title, string? Detail);
}
public sealed class LocalWorkflowClientException(string message) : Exception(message);
