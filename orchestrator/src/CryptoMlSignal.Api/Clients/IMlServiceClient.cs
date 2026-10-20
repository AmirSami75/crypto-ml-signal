using CryptoMlSignal.Api.Contracts;

namespace CryptoMlSignal.Api.Clients;

public interface IMlServiceClient
{
    Task<DependencyHealth> GetHealthAsync(CancellationToken cancellationToken);

    Task<MlServiceCapabilities?> GetCapabilitiesAsync(
        CancellationToken cancellationToken);
}
