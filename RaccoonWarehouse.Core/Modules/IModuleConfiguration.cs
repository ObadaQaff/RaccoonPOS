namespace RaccoonWarehouse.Core.Modules
{
    public interface IModuleConfiguration
    {
        bool IsEnabled(string moduleKey);
    }
}
