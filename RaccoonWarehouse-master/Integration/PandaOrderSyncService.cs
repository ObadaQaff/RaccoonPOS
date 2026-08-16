using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RaccoonWarehouse.Application.Service.Orders;
using RaccoonWarehouse.Domain.Integration;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaccoonWarehouse.Integration;

public sealed record OrderImportedEventArgs(int PandaOrderId, int RaccoonInvoiceId);
public interface IPandaOrderSyncService : IAsyncDisposable
{
    event EventHandler<OrderImportedEventArgs>? OrderImported;
    Task StartAsync(CancellationToken cancellationToken = default);
    Task SynchronizeAsync(CancellationToken cancellationToken = default);
}

public sealed class PandaOrderSyncService : IPandaOrderSyncService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PandaOrderSyncService> _logger;
    private readonly HttpClient _httpClient = new();
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private HubConnection? _connection;
    private readonly Settings _settings = Settings.Load();
    public event EventHandler<OrderImportedEventArgs>? OrderImported;

    public PandaOrderSyncService(IServiceScopeFactory scopeFactory, ILogger<PandaOrderSyncService> logger)
    { _scopeFactory = scopeFactory; _logger = logger; }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled) return;
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        var hubUrl = new Uri(new Uri(_settings.BaseUrl), "hubs/order-sync");
        _connection = new HubConnectionBuilder().WithUrl(hubUrl).WithAutomaticReconnect().Build();
        _connection.On<OrderAvailableNotification>("OrderAvailable", item => ProcessEventAsync(item.EventId, CancellationToken.None));
        _connection.Reconnected += _ => SynchronizeAsync(CancellationToken.None);
        await _connection.StartAsync(cancellationToken);
        await SynchronizeAsync(cancellationToken);
    }

    public async Task SynchronizeAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled) return;
        await _syncLock.WaitAsync(cancellationToken);
        try
        {
            var afterId = 0;
            while (true)
            {
                var page = await _httpClient.GetFromJsonAsync<PendingResponse>($"api/integration/orders/pending?afterId={afterId}&pageSize=100", JsonOptions, cancellationToken);
                if (page?.Data == null || page.Data.Count == 0) break;
                foreach (var item in page.Data)
                {
                    await ProcessEventAsync(item.EventId, cancellationToken);
                    afterId = Math.Max(afterId, item.Id);
                }
                if (page.Data.Count < 100) break;
            }
        }
        finally { _syncLock.Release(); }
    }

    private async Task ProcessEventAsync(Guid eventId, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"api/integration/orders/{eventId}", cancellationToken);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var integrationEvent = JsonSerializer.Deserialize<OrderSubmittedV1>(json, JsonOptions)
                ?? throw new InvalidOperationException("Panda returned an invalid order event.");
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IPandaOrderProcessor>();
            var result = await processor.ProcessAsync(integrationEvent, json, cancellationToken);
            if (!result.Success || result.Data == null)
            {
                await RejectAsync(eventId, "PROCESSING_FAILED", result.Message, cancellationToken);
                return;
            }
            using var acknowledge = await _httpClient.PostAsync(
                $"api/integration/orders/{eventId}/acknowledge", null, cancellationToken);
            acknowledge.EnsureSuccessStatusCode();
            if (!result.Data.AlreadyProcessed)
                OrderImported?.Invoke(this, new(integrationEvent.Order.OrderId, result.Data.InvoiceId));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Panda order event {EventId} could not be imported into Raccoon.", eventId);
            try
            {
                await RejectAsync(eventId, "IMPORT_EXCEPTION", ex.Message, cancellationToken);
            }
            catch (Exception rejectException)
            {
                _logger.LogError(rejectException, "Raccoon could not report failure for Panda event {EventId}.", eventId);
            }
        }
    }

    private async Task RejectAsync(Guid eventId, string errorCode, string? errorSummary,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync($"api/integration/orders/{eventId}/reject",
            new { errorCode, errorSummary }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null) await _connection.DisposeAsync();
        _httpClient.Dispose(); _syncLock.Dispose();
    }

    private sealed record OrderAvailableNotification(Guid EventId, string EventType, string PandaOrderId, DateTime OccurredAtUtc);
    private sealed record PendingItem(int Id, Guid EventId);
    private sealed record PendingResponse(bool Success, List<PendingItem> Data);
    private sealed class Settings
    {
        public bool Enabled { get; init; }
        public string BaseUrl { get; init; } = "https://api.boxjo.app/";
        public string ApiKey { get; init; } = string.Empty;
        public static Settings Load()
        {
            var enabled = false; var baseUrl = "https://api.boxjo.app/";
            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (File.Exists(path))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                if (doc.RootElement.TryGetProperty("PandaOrderSync", out var section))
                {
                    if (section.TryGetProperty("Enabled", out var e)) enabled = e.GetBoolean();
                    if (section.TryGetProperty("BaseUrl", out var b) && !string.IsNullOrWhiteSpace(b.GetString())) baseUrl = b.GetString()!;
                }
            }
            return new Settings { Enabled = enabled, BaseUrl = baseUrl, ApiKey = Environment.GetEnvironmentVariable("PANDA_INTEGRATION_API_KEY") ?? string.Empty };
        }
    }
}
