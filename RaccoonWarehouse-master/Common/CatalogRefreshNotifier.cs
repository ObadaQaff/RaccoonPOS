using System;
using System.Threading;

namespace RaccoonWarehouse.Common
{
    public static class CatalogRefreshNotifier
    {
        public static event EventHandler? CatalogChanged;

        private static int _suppressionCount;

        public static IDisposable SuppressNotifications()
        {
            Interlocked.Increment(ref _suppressionCount);
            return new NotificationSuppression();
        }

        public static void NotifyCatalogChanged()
        {
            if (Volatile.Read(ref _suppressionCount) > 0)
                return;

            CatalogChanged?.Invoke(null, EventArgs.Empty);
        }

        private sealed class NotificationSuppression : IDisposable
        {
            private int _disposed;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                    Interlocked.Decrement(ref _suppressionCount);
            }
        }
    }
}
