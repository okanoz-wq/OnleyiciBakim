using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using OnleyiciBakim.Options;

namespace OnleyiciBakim.Infrastructure;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class IntegrationApiKeyAttribute : TypeFilterAttribute
{
    public IntegrationApiKeyAttribute() : base(typeof(IntegrationApiKeyFilter))
    {
    }
}

public sealed class IntegrationApiKeyFilter(
    IOptions<IntegrationOptions> options,
    IHostEnvironment environment) : IAuthorizationFilter
{
    private const string HeaderName = "X-Integration-Key";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var expected = options.Value.ApiKey;
        var supplied = context.HttpContext.Request.Headers[HeaderName].FirstOrDefault();

        if ((environment.IsDevelopment() || environment.IsEnvironment("Test")) &&
            string.IsNullOrWhiteSpace(expected))
            return;

        if (string.IsNullOrWhiteSpace(expected) ||
            expected.StartsWith("CHANGE-ME", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(supplied) ||
            !FixedTimeEquals(expected, supplied))
        {
            context.Result = new UnauthorizedObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Entegrasyon anahtarı geçersiz",
                Detail = $"{HeaderName} başlığında geçerli entegrasyon anahtarı gönderilmelidir."
            });
        }
    }

    private static bool FixedTimeEquals(string expected, string supplied)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        return expectedBytes.Length == suppliedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
    }
}
