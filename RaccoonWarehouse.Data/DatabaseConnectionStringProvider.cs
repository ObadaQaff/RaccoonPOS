namespace RaccoonWarehouse.Data
{
    public static class DatabaseConnectionStringProvider
    {
        private const string ConnectionStringVariable = "RACCOONWAREHOUSE_CONNECTION_STRING";

        public static string GetConnectionString()
        {
            return Environment.GetEnvironmentVariable(ConnectionStringVariable)
                ?? string.Empty;
        }
    }
}
