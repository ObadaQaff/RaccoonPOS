using System.Text.Json;

namespace RaccoonWarehouse.Data
{
    public static class DatabaseConnectionStringProvider
    {
        private const string FallbackConnectionString = "Data Source=SQL1002.site4now.net;Initial Catalog=db_abc5d4_raccoon;User Id=db_abc5d4_raccoon_admin;Password=1234@raccoon;TrustServerCertificate=True;";
        private const string ConnectionStringVariable = "RACCOONWAREHOUSE_CONNECTION_STRING";
        private const string ConnectionStringsSectionName = "ConnectionStrings";
        private const string DefaultConnectionName = "DefaultConnection";
        private const string AppSettingsFileName = "appsettings.json";

        public static string GetConnectionString()
        {
            var environmentConnectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);
            if (!string.IsNullOrWhiteSpace(environmentConnectionString))
            {
                return environmentConnectionString;
            }

            var appSettingsConnectionString = TryGetConnectionStringFromAppSettings();
            if (!string.IsNullOrWhiteSpace(appSettingsConnectionString))
            {
                return appSettingsConnectionString;
            }

            return FallbackConnectionString;
        }

        private static string? TryGetConnectionStringFromAppSettings()
        {
            var appSettingsPath = Path.Combine(AppContext.BaseDirectory, AppSettingsFileName);
            if (!File.Exists(appSettingsPath))
            {
                return null;
            }

            using var stream = File.OpenRead(appSettingsPath);
            using var document = JsonDocument.Parse(stream);

            if (!document.RootElement.TryGetProperty(ConnectionStringsSectionName, out var connectionStringsElement))
            {
                return null;
            }

            if (!connectionStringsElement.TryGetProperty(DefaultConnectionName, out var defaultConnectionElement))
            {
                return null;
            }

            return defaultConnectionElement.GetString();
        }
    }
}
