using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.SqlClient;
using RaccoonWarehouse.Core.Interface;
using System.Diagnostics;
using System.Threading.Channels;

namespace RaccoonWarehouse.Application.Service.Stocks
{
    public interface IPosProductCatalogRefreshQueue
    {
        void Enqueue(IEnumerable<int> productIds, bool refreshStockSummary = false);
    }

    /// <summary>
    /// Refreshes only products touched by stock changes after the owning workflow
    /// has committed. A fresh scope is used so the refresh never shares a DbContext
    /// with the invoice or stock transaction.
    /// </summary>
    public sealed class PosProductCatalogRefreshQueue : IPosProductCatalogRefreshQueue
    {
        private const int MaxDeadlockRetries = 3;

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
        private readonly Channel<RefreshRequest> _requests = Channel.CreateUnbounded<RefreshRequest>(
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

        public void Enqueue(IEnumerable<int> productIds, bool refreshStockSummary = false)
        {
            var hasItems = false;
            foreach (var productId in productIds.Where(id => id > 0).Distinct())
            {
                _requests.Writer.TryWrite(new RefreshRequest(productId, refreshStockSummary));
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
            while (await _requests.Reader.WaitToReadAsync())
            {
                var productIds = new HashSet<int>();
                var refreshStockSummary = false;
                while (_requests.Reader.TryRead(out var request))
                {
                    productIds.Add(request.ProductId);
                    refreshStockSummary |= request.RefreshStockSummary;
                }

                if (productIds.Count == 0)
                    continue;

                for (var attempt = 0; attempt <= MaxDeadlockRetries; attempt++)
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        if (refreshStockSummary)
                        {
                            var stockService = scope.ServiceProvider.GetRequiredService<IStockService>();
                            await stockService.RefreshStockSummariesAsync(productIds);
                        }

                        var catalogService = scope.ServiceProvider.GetRequiredService<IPosProductCatalogService>();
                        await catalogService.RefreshProductsAsync(productIds);
                        await scope.ServiceProvider.GetRequiredService<IUOW>().CommitAsync();
                        NotifyCatalogRefreshed();
                        break;
                    }
                    catch (SqlException ex) when (ex.Number == 1205 && attempt < MaxDeadlockRetries)
                    {
                        var delay = TimeSpan.FromMilliseconds(200 * (attempt + 1));
                        Debug.WriteLine($"[POS catalog refresh] Deadlock detected; retry {attempt + 1}/{MaxDeadlockRetries} after {delay.TotalMilliseconds:0} ms.");
                        await Task.Delay(delay);
                    }
                    catch (Exception ex)
                    {
                        // The stock and invoice transaction has already completed. A later
                        // stock change will retry the affected product, while the failure is
                        // retained in the debug output for diagnosis.
                        Debug.WriteLine($"[POS catalog refresh] {ex}");
                        break;
                    }
                }
            }
        }

        private readonly record struct RefreshRequest(int ProductId, bool RefreshStockSummary);
    }
}
