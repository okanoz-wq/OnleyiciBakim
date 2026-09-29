using OnleyiciBakim.Models.Ai;

namespace OnleyiciBakim.Services.Features;

public interface IMachineFeatureService
{
    Task<MachineFeatureDto> BuildFeaturesAsync(
        string machineId,
        DateTime calculationDate,
        CancellationToken cancellationToken = default);

    Task<MachineFeatureDto> BuildFeaturesAsync(
        int machineId,
        DateTime calculationDate,
        CancellationToken cancellationToken = default) =>
        BuildFeaturesAsync(machineId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            calculationDate, cancellationToken);
}
