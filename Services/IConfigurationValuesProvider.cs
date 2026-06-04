namespace VEMS.Services;

public interface IConfigurationValuesProvider
{
    Task<IReadOnlyList<string>> GetValuesAsync(string configKey, CancellationToken cancellationToken = default);

    Task<string> GetDefaultValueAsync(string configKey, CancellationToken cancellationToken = default);
}
