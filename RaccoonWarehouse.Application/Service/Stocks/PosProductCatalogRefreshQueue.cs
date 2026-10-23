using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Core.Interface;
using System.Diagnostics;
using System.Threading.Channels;

namespace RaccoonWarehouse.Application.Service.Stocks
{
    public interface IPosProductCatalogRefreshQueue
    {
        void Enqueue(IEnumerable<int> productIds);
    }

    /// <summary>
    /// Refreshes only products touched by stock changes after the owning workflow
    /// has committed. A fresh scope is used so the refresh never shares a DbContext
    /// with the invoice or stock transaction.
    /// </summary>
    public sealed class PosProductCatalogRefreshQueue : IPosProductCatalogRefreshQueue
    {
        public static event Action? CatalogRefreshed;

        private static void NotifyCatalogRefreshed()
        {
            var handlers = CatalogRefreshed;
            if (handlers == null)
                return;

            foreach (Action handler in handlers.GetInvocationList())
            {
                try
                {
                    handler();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[POS catalog notification] {ex}");
                }
            }
        }

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly Channel<int> _productIds = Channel.CreateUnbounded<int>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });

        private Task? _worker;
        private readonly object _workerLock = new();

        public PosProductCatalogRefreshQueue(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public void Enqueue(IEnumerable<int> productIds)
        {
            var hasItems = false;
            foreach (var productId in productIds.Where(id => id > 0).Distinct())
            {
                _productIds.Writer.TryWrite(productId);
                hasItems = true;
            }

            if (!hasItems)
                return;

            lock (_workerLock)
            {
                if (_worker == null || _worker.IsCompleted)
                    _worker = Task.Run(ProcessLoopAsync);
            }
        }

        private async Task ProcessLoopAsync()
        {
            while (await _productIds.Reader.WaitToReadAsync())
            {
                var productIds = new HashSet<int>();
                while (_productIds.Reader.TryRead(out var productId))
                    productIds.Add(productId);

                if (productIds.Count == 0)
                    continue;

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var catalogService = scope.ServiceProvider.GetRequiredService<IPosProductCatalogService>();
                    await catalogService.RefreshProductsAsync(productIds);
                    await scope.ServiceProvider.GetRequiredService<IUOW>().CommitAsync();
                    NotifyCatalogRefreshed();
                }
                catch (Exception ex)
                {
                    // The stock and invoice transaction has already completed. A later
                    // stock change will retry the affected product, while the failure is
                    // retained in the debug output for diagnosis.
                    Debug.WriteLine($"[POS catalog refresh] {ex}");
                }
            }
        }
    }
}
