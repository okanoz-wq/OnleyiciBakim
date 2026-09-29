using OnleyiciBakimSistemi.Models;
using OnleyiciBakimSistemi.Models.ViewModels;

namespace OnleyiciBakimSistemi.Services;

// Geçici mock veri servisi. Veritabanı ve Python tarafındaki tahmin modeli
// hazır olduğunda bu sınıf yerine gerçek veriyi okuyan bir servis yazılacak.
public class MockMaintenanceDataService : IMaintenanceDataService
{
    private static readonly string[] MachineTypes =
    {
        "CNC Torna", "Hidrolik Pres", "Konveyör Bant", "Kompresör",
        "Kaynak Robotu", "Enjeksiyon Makinesi", "Paketleme Ünitesi", "Vinç"
    };

    private static readonly string[] Locations = { "Hat 1", "Hat 2", "Hat 3", "Depo", "Montaj Alanı" };

    private static readonly string[] Technicians = { "A. Yılmaz", "M. Kaya", "E. Demir", "S. Şahin", "B. Çelik" };

    public IReadOnlyList<string> FaultTypes { get; } = new[] { "Mekanik", "Elektrik", "Hidrolik", "Yazılım", "Aşınma" };
    public IReadOnlyList<string> Severities { get; } = new[] { "Düşük", "Orta", "Yüksek", "Kritik" };

    private readonly List<Machine> _machines;
    private readonly List<FaultRecord> _faults;
    private readonly List<MaintenancePlanItem> _plans;

    public MockMaintenanceDataService()
    {
        var rnd = new Random(42);

        _machines = Enumerable.Range(0, 12).Select(i =>
        {
            var risk = rnd.Next(0, 101);
            var status = risk > 70 ? "Riskli" : risk > 40 ? "İzlemede" : "Normal";
            return new Machine
            {
                Id = $"MKN-{(i + 1):000}",
                Code = $"MKN-{(i + 1):000}",
                Name = $"{MachineTypes[i % MachineTypes.Length]} {i + 1}",
                Type = MachineTypes[i % MachineTypes.Length],
                Location = Locations[i % Locations.Length],
                Status = status,
                RiskScore = risk,
                RiskLevel = risk >= 80 ? "Kritik" : risk >= 60 ? "Yüksek" : risk >= 30 ? "Orta" : "Düşük",
                RiskColorKey = risk >= 80 ? "dark-red" : risk >= 60 ? "red" : risk >= 30 ? "amber" : "green",
                InstallDate = DateTime.Today.AddDays(-rnd.Next(0, 2000)),
                LastMaintenanceDate = DateTime.Today.AddDays(-rnd.Next(0, 180)),
                NextMaintenanceDate = DateTime.Today.AddDays(rnd.Next(1, 90)),
                TotalFaultCount = rnd.Next(3, 23),
            };
        }).ToList();

        _faults = Enumerable.Range(0, 60).Select(i =>
        {
            var machine = _machines[rnd.Next(_machines.Count)];
            return new FaultRecord
            {
                Id = $"FLR-{(i + 1):000}",
                MachineId = machine.Id,
                MachineName = machine.Name,
                Date = DateTime.Today.AddDays(-rnd.Next(0, 720)),
                FaultType = FaultTypes[rnd.Next(FaultTypes.Count)],
                Severity = Severities[rnd.Next(Severities.Count)],
                Description = "Arıza bildirimi - geçmiş kayıt (örnek veri)",
                DowntimeHours = Math.Round((decimal)rnd.NextDouble() * 24m, 1),
                Technician = Technicians[rnd.Next(Technicians.Length)],
                Resolved = rnd.NextDouble() > 0.1,
            };
        }).OrderByDescending(f => f.Date).ToList();

        foreach (var machine in _machines)
        {
            machine.TotalFaultCount = _faults.Count(f => f.MachineId == machine.Id);
        }

        _plans = _machines
            .Where(m => m.RiskScore > 35)
            .Select((m, i) => new MaintenancePlanItem
            {
                Id = $"PLN-{(i + 1):000}",
                MachineId = m.Id,
                MachineName = m.Name,
                PlannedDate = m.NextMaintenanceDate,
                Type = m.RiskScore > 70 ? "Acil" : "Önleyici",
                PredictedRiskScore = m.RiskScore,
                Confidence = rnd.Next(60, 100),
                Status = i % 4 == 0 ? "Tamamlandı" : i % 3 == 0 ? "Devam Ediyor" : "Planlandı",
                RecommendedAction = m.RiskScore > 70
                    ? "Kritik bileşen kontrolü ve değişimi önerilir"
                    : "Rutin kontrol ve yağlama önerilir",
            })
            .OrderBy(p => p.PlannedDate)
            .ToList();
    }

    public Task<IReadOnlyList<Machine>> GetMachinesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Machine>>(_machines);

    public Task<DashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new DashboardViewModel
        {
            TotalMachines = _machines.Count,
            RiskyMachineCount = _machines.Count(machine => machine.RiskScore >= 60m),
            PendingPlanCount = _plans.Count(plan => plan.Status != "Tamamlandı"),
            AverageDowntimeHours = _faults.Count == 0 ? 0 : (double)_faults.Average(fault => fault.DowntimeHours),
            TopRiskMachines = _machines.OrderByDescending(machine => machine.RiskScore).Take(5).ToList()
        });

    public Task<Machine?> GetMachineAsync(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_machines.FirstOrDefault(m => m.Id == id));

    public Task<IReadOnlyList<FaultRecord>> GetFaultsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<FaultRecord>>(_faults);

    public Task<IReadOnlyList<FaultRecord>> GetFaultsByMachineAsync(
        string machineId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<FaultRecord>>(
            _faults.Where(f => f.MachineId == machineId).ToList());

    public Task<IReadOnlyList<MaintenancePlanItem>> GetMaintenancePlansAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MaintenancePlanItem>>(_plans);

    public Task<IReadOnlyList<MonthlyFaultPoint>> GetMonthlyFaultTrendAsync(
        int months,
        int? year,
        CancellationToken cancellationToken = default)
    {
        months = months == 12 ? 12 : 6;
        var now = DateTime.Today;
        var matching = year.HasValue ? _faults.Where(x => x.Date.Year == year.Value) : _faults;
        var latest = matching.OrderByDescending(x => x.Date).Select(x => (DateTime?)x.Date).FirstOrDefault();
        var anchor = latest ?? (year.HasValue && year.Value != now.Year
            ? new DateTime(year.Value, 12, 1)
            : now);
        var start = year.HasValue && months == 12
            ? new DateTime(year.Value, 1, 1)
            : new DateTime(anchor.Year, anchor.Month, 1).AddMonths(-(months - 1));
        var points = new List<MonthlyFaultPoint>();
        for (var i = 0; i < months; i++)
        {
            var month = start.AddMonths(i);
            var count = _faults.Count(f => f.Date.Year == month.Year && f.Date.Month == month.Month);
            points.Add(new MonthlyFaultPoint
            {
                Month = month.ToString("MMM yyyy", new System.Globalization.CultureInfo("tr-TR")),
                Count = count,
            });
        }
        return Task.FromResult<IReadOnlyList<MonthlyFaultPoint>>(points);
    }

    public Task<IReadOnlyList<int>> GetFaultYearsAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<int>>(_faults.Select(x => x.Date.Year)
            .Distinct().OrderByDescending(x => x).ToArray());

    public Task<IReadOnlyList<FaultTypeDistributionPoint>> GetFaultTypeDistributionAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<FaultTypeDistributionPoint>>(
            FaultTypes.Select(t => new FaultTypeDistributionPoint
        {
            Name = t,
            Value = _faults.Count(f => f.FaultType == t),
        }).ToList());
}
