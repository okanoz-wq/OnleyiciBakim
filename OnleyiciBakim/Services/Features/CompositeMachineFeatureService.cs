using OnleyiciBakim.Infrastructure;
using OnleyiciBakim.Models.Ai;

namespace OnleyiciBakim.Services.Features;

public sealed class CompositeMachineFeatureService(
    BakimYonetimiMachineFeatureService readModelService,
    MachineFeatureService commandModelService,
    ILogger<CompositeMachineFeatureService> logger) : IMachineFeatureService
{
    public async Task<MachineFeatureDto> BuildFeaturesAsync(
        string machineId,
        DateTime calculationDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await readModelService.BuildFeaturesAsync(
                machineId, calculationDate, cancellationToken);
        }
        catch (ResourceNotFoundException)
        {
            logger.LogInformation(
                "Makine SQL Server okuma modelinde bulunamadı; komut modelinden özellik üretilecek. " +
                "MachineId={MachineId}", machineId);
            return await commandModelService.BuildFeaturesAsync(
                machineId, calculationDate, cancellationToken);
        }
    }
}
