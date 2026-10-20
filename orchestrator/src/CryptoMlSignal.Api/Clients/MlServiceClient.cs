using System.Net.Http.Json;
using CryptoMlSignal.Api.Contracts;

namespace CryptoMlSignal.Api.Clients;

public sealed class MlServiceClient(HttpClient httpClient, ILogger<MlServiceClient> logger)
    : IMlServiceClient
{
    public async Task<DependencyHealth> GetHealthAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync("/health", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new DependencyHealth(
                    "python-ml",
                    "unhealthy",
                    $"HTTP {(int)response.StatusCode}");
            }

            var health = await response.Content.ReadFromJsonAsync<MlServiceHealth>(
                cancellationToken);
            return new DependencyHealth(
                "python-ml",
                health?.Status ?? "unhealthy");
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Python ML health check failed");
            return new DependencyHealth("python-ml", "unavailable", exception.Message);
        }
    }

    public async Task<MlServiceCapabilities?> GetCapabilitiesAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<MlServiceCapabilities>(
                "/api/v1/capabilities",
                cancellationToken);
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Python ML capability request failed");
            return null;
        }
    }
}
