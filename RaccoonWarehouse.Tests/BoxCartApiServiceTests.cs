using RaccoonWarehouse.Application.Service.Orders;
using RaccoonWarehouse.Domain.Enums;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace RaccoonWarehouse.Tests;

public class BoxCartApiServiceTests
{
    [Fact]
    public async Task GetPendingOrdersAsync_ShouldReturnCurrentCartIds()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal(
                "https://box.test/api/Cart/RaccoonPendingOrders",
                request.RequestUri!.ToString());

            return JsonResponse("""
                {
                  "success": true,
                  "message": "Success",
                  "data": [
                    { "cartId": 704, "userId": 1, "items": [] },
                    { "cartId": 705, "userId": 2, "items": [] }
                  ]
                }
                """);
        });
        var service = CreateService(handler);

        var result = await service.GetPendingOrdersAsync();

        Assert.True(result.Success, result.Message);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data!.Count);
        Assert.Equal([704, 705], result.Data.Orders.Select(order => order.CartId));
    }

    [Fact]
    public async Task UpdateCartStatusAsync_ShouldLoadFullCartAndSendUpdatedStatus()
    {
        var requests = new List<HttpRequestMessage>();
        var handler = new StubHttpMessageHandler(async request =>
        {
            requests.Add(request);
            if (request.Method == HttpMethod.Post)
            {
                return JsonResponse("""
                    {
                      "success": true,
                      "message": "Success",
                      "data": {
                        "id": 704,
                        "name": "Order #704",
                        "userId": 312,
                        "cartItems": [
                          {
                            "id": 13758,
                            "cartId": 704,
                            "productId": 2885,
                            "unitId": 712,
                            "quantity": 1,
                            "price": 4.65
                          }
                        ],
                        "totalPrice": 4.65,
                        "cartStatus": 2,
                        "pickUpTime": "2026-06-09T15:57:37",
                        "createdDate": "2026-06-09T18:57:37",
                        "updatedDate": "2026-06-09T15:57:37"
                      }
                    }
                    """);
            }

            var json = await request.Content!.ReadAsStringAsync();
            using var document = JsonDocument.Parse(json);
            Assert.Equal(704, document.RootElement.GetProperty("id").GetInt32());
            Assert.Equal(BoxCartApiService.CompletedStatus, document.RootElement.GetProperty("cartStatus").GetInt32());
            Assert.Single(document.RootElement.GetProperty("cartItems").EnumerateArray());
            return JsonResponse("""{"success":true,"message":"Updated","data":{}}""");
        });
        var service = CreateService(handler);

        var result = await service.UpdateCartStatusAsync(
            704,
            BoxCartApiService.CompletedStatus);

        Assert.True(result.Success, result.Message);
        Assert.Equal(2, requests.Count);
        Assert.Equal(HttpMethod.Post, requests[0].Method);
        Assert.Equal(HttpMethod.Put, requests[1].Method);
    }

    [Fact]
    public async Task UpdateCartItemsAsync_ShouldUpdateExistingItemsAndTotal()
    {
        var handler = new StubHttpMessageHandler(async request =>
        {
            if (request.Method == HttpMethod.Post)
            {
                return JsonResponse("""
                    {
                      "success": true,
                      "data": {
                        "id": 704,
                        "userId": 312,
                        "cartItems": [
                          { "id": 13758, "cartId": 704, "productId": 2885, "unitId": 712, "quantity": 1, "price": 4.65 }
                        ],
                        "totalPrice": 4.65,
                        "cartStatus": 0
                      }
                    }
                    """);
            }

            var json = await request.Content!.ReadAsStringAsync();
            using var document = JsonDocument.Parse(json);
            var item = document.RootElement.GetProperty("cartItems")[0];
            Assert.Equal(3, item.GetProperty("quantity").GetInt32());
            Assert.Equal(6.25m, item.GetProperty("price").GetDecimal());
            Assert.Equal(18.75m, document.RootElement.GetProperty("totalPrice").GetDecimal());
            return JsonResponse("""{"success":true,"message":"Updated","data":{}}""");
        });
        var service = CreateService(handler);

        var result = await service.UpdateCartItemsAsync(
            704,
            [
                new()
                {
                    InvoiceLineId = 1,
                    CartItemId = 13758,
                    Quantity = 3,
                    UnitPrice = 6.25m
                }
            ]);

        Assert.True(result.Success, result.Message);
    }

    [Theory]
    [InlineData(InvoiceStatus.Unknown, BoxCartApiService.UnknownStatus)]
    [InlineData(InvoiceStatus.InProcess, BoxCartApiService.InProcessStatus)]
    [InlineData(InvoiceStatus.Completed, BoxCartApiService.CompletedStatus)]
    [InlineData(InvoiceStatus.Cancelled, BoxCartApiService.CancelledStatus)]
    public void DesktopStatus_ShouldMapToExpectedBoxStatus(
        InvoiceStatus invoiceStatus,
        int expectedBoxStatus)
    {
        Assert.Equal(expectedBoxStatus, EndpointOrderStatusService.MapBoxCartStatus(invoiceStatus));
    }

    private static BoxCartApiService CreateService(HttpMessageHandler handler)
    {
        return new BoxCartApiService(
            new HttpClient(handler),
            "https://box.test/",
            timeoutSeconds: 5);
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            : this(request => Task.FromResult(handler(request)))
        {
        }

        public StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return _handler(request);
        }
    }
}
