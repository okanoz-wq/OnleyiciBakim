using OnleyiciBakim.Contracts.Risk;
using OnleyiciBakim.Models.Ai;

namespace OnleyiciBakim.Services.Risk;

public interface IRuleBasedRiskService
{
    AlgorithmicCalculationResult Calculate(MachineFeatureDto features);
}
