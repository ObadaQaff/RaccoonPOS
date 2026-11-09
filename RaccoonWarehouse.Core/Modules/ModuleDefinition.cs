namespace RaccoonWarehouse.Core.Modules
{
    public sealed record ModuleActionDefinition(
        string Key,
        string DisplayLabel,
        string? PermissionKey = null);

    public sealed record ModuleGroupDefinition(
        string Title,
        IReadOnlyList<ModuleActionDefinition> Actions);

    public sealed record ModuleDefinition(
        string Key,
        string DisplayName,
        IReadOnlyList<ModuleGroupDefinition> Groups);
}
