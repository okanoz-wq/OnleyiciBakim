namespace OnleyiciBakimSistemi.Models;

public class MonthlyFaultPoint
{
    public string Month { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class FaultTypeDistributionPoint
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}
