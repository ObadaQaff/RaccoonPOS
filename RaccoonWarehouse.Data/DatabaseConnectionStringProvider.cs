namespace RaccoonWarehouse.Data
{
    public static class DatabaseConnectionStringProvider
    {
        private const string FallbackConnectionString =
            "Data Source=SQL1002.site4now.net;Initial Catalog=db_abc5d4_raccoon;User Id=db_abc5d4_raccoon_admin;Password=1234@raccoon;TrustServerCertificate=True;";

        public static string GetConnectionString()
        {
            return Environment.GetEnvironmentVariable("RACCOONWAREHOUSE_CONNECTION_STRING")
                ?? FallbackConnectionString;
        }
    }
}
