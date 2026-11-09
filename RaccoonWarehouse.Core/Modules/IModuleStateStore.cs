namespace RaccoonWarehouse.Core.Modules;

public interface IModuleStateStore : IModuleConfiguration
{
    IReadOnlyDictionary<string, bool> Snapshot { get; }
    Task LoadAsync(CancellationToken cancellationToken = default);
    Task<bool> SetEnabledAsync(string moduleKey, bool enabled, CancellationToken cancellationToken = default);
}
