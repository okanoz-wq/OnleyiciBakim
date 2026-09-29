extern alias frontend;

using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Xunit;
using FrontendProgram = frontend::Program;

namespace OnleyiciBakim.Tests;

public sealed class FrontendSecurityTests
{
    [Fact]
    public async Task DefinitionPost_WithoutAntiForgeryToken_IsRejected()
    {
        await using var factory = new WebApplicationFactory<FrontendProgram>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        using var response = await client.PostAsync("/Definitions/Save", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Module"] = "companies", ["Code"] = "NO-TOKEN", ["Name"] = "Reddedilmeli"
            }));
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest,
            $"Beklenen 400, gerçekleşen {(int)response.StatusCode}. {content}");
    }
}
