using System.Text.Json;
using System.IO;
using RaccoonWarehouse.Core.Modules;

namespace RaccoonWarehouse.Navigation.Modules
{
    public sealed class ModuleConfiguration : IModuleConfiguration
    {
        private readonly IReadOnlyDictionary<string, bool> _enabledModules;

        public ModuleConfiguration()
        {
            _enabledModules = LoadEnabledModules();
        }

        public bool IsEnabled(string moduleKey) =>
            !_enabledModules.TryGetValue(moduleKey, out var enabled) || enabled;

        private static IReadOnlyDictionary<string, bool> LoadEnabledModules()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(path))
                return new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                if (!document.RootElement.TryGetProperty("Modules", out var modules) ||
                    modules.ValueKind != JsonValueKind.Object)
                    return new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

                return modules.EnumerateObject()
                    .Where(property => property.Value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    .ToDictionary(
                        property => property.Name,
                        property => property.Value.GetBoolean(),
                        StringComparer.OrdinalIgnoreCase);
            }
            catch (JsonException)
            {
                return new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
