using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Domain.Orders.DTOs;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaccoonWarehouse.Application.Service.Orders
{
    public interface IBoxCartApiService
    {
        Task<Result<BoxPendingOrdersSnapshotDto>> GetPendingOrdersAsync(
            CancellationToken cancellationToken = default);

        Task<Result> UpdateCartStatusAsync(
            int cartId,
            int cartStatus,
            CancellationToken cancellationToken = default);

        Task<Result> UpdateCartItemsAsync(
            int cartId,
            IReadOnlyCollection<EndpointOrderLineEditDto> lines,
            CancellationToken cancellationToken = default);
    }

    public sealed class BoxCartApiService : IBoxCartApiService
    {
        public const int UnknownStatus = 0;
        public const int CompletedStatus = 1;
        public const int InProcessStatus = 2;
        public const int CancelledStatus = 3;

        private const string DefaultBaseUrl = "https://api.boxjo.app/";
        private const string PendingOrdersPath = "api/Cart/RaccoonPendingOrders";
        private const string GetCartPath = "api/Cart/GetCartById";
        private const string UpdateCartPath = "api/Cart/UpdateCart";
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly HttpClient _httpClient;
        private readonly string? _baseUrlOverride;
        private readonly int? _timeoutSecondsOverride;

        public BoxCartApiService()
            : this(new HttpClient())
        {
        }

        public BoxCartApiService(
            HttpClient httpClient,
            string? baseUrl = null,
            int? timeoutSeconds = null)
        {
            _httpClient = httpClient;
            _baseUrlOverride = baseUrl;
            _timeoutSecondsOverride = timeoutSeconds;
        }

        public async Task<Result<BoxPendingOrdersSnapshotDto>> GetPendingOrdersAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var settings = LoadSettings();
                using var timeoutCancellation = CreateTimeoutCancellation(
                    cancellationToken,
                    settings.TimeoutSeconds);
                var endpoint = BuildEndpoint(settings.BaseUrl, PendingOrdersPath);
                using var response = await _httpClient.GetAsync(endpoint, timeoutCancellation.Token);
                response.EnsureSuccessStatusCode();

                var payload = await response.Content.ReadFromJsonAsync<BoxApiResult<List<BoxOrderExportDto>>>(
                    JsonOptions,
                    timeoutCancellation.Token);
                if (payload?.Success != true)
                {
                    return Result<BoxPendingOrdersSnapshotDto>.Fail(
                        payload?.Message ?? "Box API returned an unsuccessful result.");
                }
                                            
                return Result<BoxPendingOrdersSnapshotDto>.Ok(new BoxPendingOrdersSnapshotDto
                {
                    Orders = payload.Data ?? new List<BoxOrderExportDto>()
                });
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return Result<BoxPendingOrdersSnapshotDto>.Fail("Box API request timed out.");
            }
            catch (Exception ex)
            {
                return Result<BoxPendingOrdersSnapshotDto>.Fail($"Box API request failed: {ex.Message}");
            }
        }

        public async Task<Result> UpdateCartStatusAsync(
            int cartId,
            int cartStatus,
            CancellationToken cancellationToken = default)
        {
            if (cartId <= 0)
                return Result.Fail("Box cart id is required.");

            if (cartStatus is < UnknownStatus or > CancelledStatus)
                return Result.Fail("Box cart status is invalid.");

            try
            {
                var settings = LoadSettings();
                using var timeoutCancellation = CreateTimeoutCancellation(
                    cancellationToken,
                    settings.TimeoutSeconds);

                var getEndpoint = BuildEndpoint(settings.BaseUrl, $"{GetCartPath}?Id={cartId}");
                using var getResponse = await _httpClient.PostAsync(
                    getEndpoint,
                    content: null,
                    timeoutCancellation.Token);
                getResponse.EnsureSuccessStatusCode();

                var getPayload = await getResponse.Content.ReadFromJsonAsync<BoxApiResult<BoxCartWriteDto>>(
                    JsonOptions,
                    timeoutCancellation.Token);
                if (getPayload?.Success != true || getPayload.Data == null)
                {
                    return Result.Fail(
                        getPayload?.Message ?? $"Box cart {cartId} could not be loaded.");
                }

                getPayload.Data.CartStatus = cartStatus;
                getPayload.Data.UpdatedDate = DateTime.Now;

                var updateEndpoint = BuildEndpoint(settings.BaseUrl, UpdateCartPath);
                using var updateResponse = await _httpClient.PutAsJsonAsync(
                    updateEndpoint,
                    getPayload.Data,
                    JsonOptions,
                    timeoutCancellation.Token);
                updateResponse.EnsureSuccessStatusCode();

                var updatePayload = await updateResponse.Content.ReadFromJsonAsync<BoxApiResult<JsonElement>>(
                    JsonOptions,
                    timeoutCancellation.Token);
                return updatePayload?.Success == true
                    ? Result.Ok(updatePayload.Message ?? "Box cart status updated.")
                    : Result.Fail(updatePayload?.Message ?? "Box cart status update failed.");
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return Result.Fail("Box cart status update timed out.");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Box cart status update failed: {ex.Message}");
            }
        }

        public async Task<Result> UpdateCartItemsAsync(
            int cartId,
            IReadOnlyCollection<EndpointOrderLineEditDto> lines,
            CancellationToken cancellationToken = default)
        {
            if (cartId <= 0)
                return Result.Fail("Box cart id is required.");

            if (lines == null || lines.Count == 0)
                return Result.Fail("At least one Box cart item is required.");

            try
            {
                var settings = LoadSettings();
                using var timeoutCancellation = CreateTimeoutCancellation(
                    cancellationToken,
                    settings.TimeoutSeconds);

                var cartResult = await LoadCartAsync(cartId, settings, timeoutCancellation.Token);
                if (!cartResult.Success || cartResult.Data == null)
                    return Result.Fail(cartResult.Message, cartResult.Errors);

                var editsByItemId = lines.ToDictionary(line => line.CartItemId);
                foreach (var item in cartResult.Data.CartItems)
                {
                    if (!editsByItemId.TryGetValue(item.Id, out var edit))
                        continue;

                    item.Quantity = edit.Quantity;
                    item.Price = edit.UnitPrice;
                    item.UpdatedDate = DateTime.Now;
                    editsByItemId.Remove(item.Id);
                }

                if (editsByItemId.Count > 0)
                {
                    return Result.Fail(
                        $"Box cart items were not found: {string.Join(", ", editsByItemId.Keys.OrderBy(id => id))}.");
                }

                cartResult.Data.TotalPrice = cartResult.Data.CartItems.Sum(
                    item => item.Quantity * item.Price);
                cartResult.Data.UpdatedDate = DateTime.Now;
                return await PutCartAsync(cartResult.Data, settings, timeoutCancellation.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return Result.Fail("Box cart item update timed out.");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Box cart item update failed: {ex.Message}");
            }
        }

        private async Task<Result<BoxCartWriteDto>> LoadCartAsync(
            int cartId,
            BoxOrderImportSettings settings,
            CancellationToken cancellationToken)
        {
            var getEndpoint = BuildEndpoint(settings.BaseUrl, $"{GetCartPath}?Id={cartId}");
            using var getResponse = await _httpClient.PostAsync(
                getEndpoint,
                content: null,
                cancellationToken);
            getResponse.EnsureSuccessStatusCode();

            var getPayload = await getResponse.Content.ReadFromJsonAsync<BoxApiResult<BoxCartWriteDto>>(
                JsonOptions,
                cancellationToken);
            return getPayload?.Success == true && getPayload.Data != null
                ? Result<BoxCartWriteDto>.Ok(getPayload.Data)
                : Result<BoxCartWriteDto>.Fail(
                    getPayload?.Message ?? $"Box cart {cartId} could not be loaded.");
        }

        private async Task<Result> PutCartAsync(
            BoxCartWriteDto cart,
            BoxOrderImportSettings settings,
            CancellationToken cancellationToken)
        {
            var updateEndpoint = BuildEndpoint(settings.BaseUrl, UpdateCartPath);
            using var updateResponse = await _httpClient.PutAsJsonAsync(
                updateEndpoint,
                cart,
                JsonOptions,
                cancellationToken);
            updateResponse.EnsureSuccessStatusCode();

            var updatePayload = await updateResponse.Content.ReadFromJsonAsync<BoxApiResult<JsonElement>>(
                JsonOptions,
                cancellationToken);
            return updatePayload?.Success == true
                ? Result.Ok(updatePayload.Message ?? "Box cart updated.")
                : Result.Fail(updatePayload?.Message ?? "Box cart update failed.");
        }

        private BoxOrderImportSettings LoadSettings()
        {
            var settings = new BoxOrderImportSettings
            {
                BaseUrl = _baseUrlOverride ?? DefaultBaseUrl,
                TimeoutSeconds = _timeoutSecondsOverride ?? 30
            };

            if (!string.IsNullOrWhiteSpace(_baseUrlOverride) || _timeoutSecondsOverride.HasValue)
                return settings;

            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(path))
                return settings;

            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (!document.RootElement.TryGetProperty("BoxApi", out var section))
                return settings;

            if (section.TryGetProperty("BaseUrl", out var baseUrlElement) &&
                !string.IsNullOrWhiteSpace(baseUrlElement.GetString()))
            {
                settings.BaseUrl = baseUrlElement.GetString()!;
            }

            if (section.TryGetProperty("TimeoutSeconds", out var timeoutElement) &&
                timeoutElement.TryGetInt32(out var timeoutSeconds) &&
                timeoutSeconds > 0)
            {
                settings.TimeoutSeconds = timeoutSeconds;
            }

            return settings;
        }

        private static CancellationTokenSource CreateTimeoutCancellation(
            CancellationToken cancellationToken,
            int timeoutSeconds)
        {
            var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCancellation.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            return timeoutCancellation;
        }

        private static Uri BuildEndpoint(string baseUrl, string path)
        {
            var normalizedBaseUrl = string.IsNullOrWhiteSpace(baseUrl)
                ? DefaultBaseUrl
                : baseUrl.Trim();
            if (!normalizedBaseUrl.EndsWith("/", StringComparison.Ordinal))
                normalizedBaseUrl += "/";

            return new Uri(new Uri(normalizedBaseUrl), path);
        }

        private sealed class BoxApiResult<T>
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public T? Data { get; set; }
        }

        private sealed class BoxOrderImportSettings
        {
            public string BaseUrl { get; set; } = DefaultBaseUrl;
            public int TimeoutSeconds { get; set; }
        }
    }
}
