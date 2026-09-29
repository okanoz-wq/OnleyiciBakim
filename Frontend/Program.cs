using OnleyiciBakimSistemi.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC (Razor View) ve Web API controller'ları aynı altyapı üzerinden ekleniyor.
builder.Services.AddControllersWithViews();

var useMockData = builder.Configuration.GetValue<bool>("Backend:UseMockData");
var backendBaseUrl = builder.Configuration["Backend:BaseUrl"]
    ?? throw new InvalidOperationException("Backend:BaseUrl yapılandırması bulunamadı.");
if (useMockData)
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("Frontend mock verisi yalnızca Development ortamında kullanılabilir.");
    builder.Services.AddSingleton<IMaintenanceDataService, MockMaintenanceDataService>();
}
else
{
    builder.Services.AddHttpClient<IMaintenanceDataService, BackendMaintenanceDataService>(client =>
    {
        client.BaseAddress = new Uri(backendBaseUrl);
        // The first maintenance-plan request may enrich historical SQL rows with live ML
        // predictions. Subsequent requests are served from the backend cache.
        client.Timeout = TimeSpan.FromSeconds(60);
    });
}
builder.Services.AddHttpClient<IMachineManagementClient, MachineManagementClient>(client =>
{
    client.BaseAddress = new Uri(backendBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient<ILocalDefinitionClient, LocalDefinitionClient>(client =>
{
    client.BaseAddress = new Uri(backendBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient<ILocalWorkflowClient, LocalWorkflowClient>(client =>
{
    client.BaseAddress = new Uri(backendBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddHttpClient<ILocalReportClient, LocalReportClient>(client =>
{
    client.BaseAddress = new Uri(backendBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error/500");
    app.UseHsts();
}

// 404, 403, 401 gibi durum kodlarını yakalayıp özel tasarlanmış sayfalara yönlendirir.
// Örn. var olmayan bir /Machines/Details/999 -> 404 -> Views/Error/NotFound.cshtml
app.UseStatusCodePagesWithReExecute("/Error/{0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program;
