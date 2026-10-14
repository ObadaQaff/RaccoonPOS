using System.Text.Json;

namespace RaccoonWarehouse.Data
{
    public static class DatabaseConnectionStringProvider
    {
        private const string FallbackConnectionString = "Data Source=SQL1002.site4now.net;Initial Catalog=db_abc5d4_raccoon;User Id=db_abc5d4_raccoon_admin;Password=1234@raccoon;TrustServerCertificate=True;";
        private const string ConnectionStringVariable = "RACCOONWAREHOUSE_CONNECTION_STRING";
        private const string ConnectionStringsSectionName = "ConnectionStrings";
        private const string DefaultConnectionName = "DefaultConnection";
        private const string RaccoonConnectionName = "RaccoonConnection";
        private const string PharmacyProfile = "Pharmacy";
        private const string RaccoonProfile = "Raccoon";
        private const string AppSettingsFileName = "appsettings.json";
        private static readonly string ProfileFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ROCCOPOS",
            "database-profile.txt");

        public static string GetConnectionString()
        {
            var selectedProfile = TryGetSelectedProfile();
            if (string.Equals(selectedProfile, RaccoonProfile, StringComparison.OrdinalIgnoreCase))
            {
                return TryGetConnectionStringFromAppSettings(RaccoonConnectionName) ?? FallbackConnectionString;
            }

            if (string.Equals(selectedProfile, PharmacyProfile, StringComparison.OrdinalIgnoreCase))
            {
                return TryGetConnectionStringFromAppSettings(DefaultConnectionName) ?? FallbackConnectionString;
            }

            var environmentConnectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);
            if (!string.IsNullOrWhiteSpace(environmentConnectionString))
            {
                return environmentConnectionString;
            }

            var appSettingsConnectionString = TryGetConnectionStringFromAppSettings(DefaultConnectionName);
            if (!string.IsNullOrWhiteSpace(appSettingsConnectionString))
            {
                return appSettingsConnectionString;
            }

            return FallbackConnectionString;
        }

        public static string GetActiveProfile() => TryGetSelectedProfile() ?? PharmacyProfile;

        public static void SwitchProfile(string profile)
        {
            var normalizedProfile = profile?.Trim();
            if (!string.Equals(normalizedProfile, PharmacyProfile, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(normalizedProfile, RaccoonProfile, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Unknown database profile.", nameof(profile));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(ProfileFilePath)!);
            File.WriteAllText(ProfileFilePath, normalizedProfile);
        }

        private static string? TryGetSelectedProfile()
        {
            try
            {
                if (!File.Exists(ProfileFilePath))
                {
                    return null;
                }

                var profile = File.ReadAllText(ProfileFilePath).Trim();
                return string.Equals(profile, PharmacyProfile, StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(profile, RaccoonProfile, StringComparison.OrdinalIgnoreCase)
                    ? profile
                    : null;
            }
            catch
            {
                return null;
            }
        }

        private static string? TryGetConnectionStringFromAppSettings(string connectionName)
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

            if (!connectionStringsElement.TryGetProperty(connectionName, out var connectionElement))
            {
                return null;
            }

            return connectionElement.GetString();
        }
    }
}
