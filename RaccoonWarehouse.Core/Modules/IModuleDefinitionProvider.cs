namespace RaccoonWarehouse.Core.Modules
{
    public interface IModuleDefinitionProvider
    {
        string ModuleKey { get; }

        Task<ModuleDefinition> GetDefinitionAsync();
    }
}
