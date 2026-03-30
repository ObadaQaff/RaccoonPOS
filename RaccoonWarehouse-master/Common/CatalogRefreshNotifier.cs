using System;

namespace RaccoonWarehouse.Common
{
    public static class CatalogRefreshNotifier
    {
        public static event EventHandler? CatalogChanged;

        public static void NotifyCatalogChanged()
        {
            CatalogChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}
