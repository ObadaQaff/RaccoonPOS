using System.Text.Json.Serialization;

namespace RaccoonWarehouse.Orders
{
    public sealed class OrderInvoiceRow
    {
        public int Id { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? CustomerName { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? DocumentDate { get; set; }
        public string? Status { get; set; }
        public decimal TotalAmount { get; set; }
        public int ItemsCount { get; set; }
        public int DisplayItemsCount => ItemsCount;
    }

    internal sealed class OrderInvoiceApiResponse
    {
        [JsonPropertyName("success")]
        public bool? Success { get; set; }

        [JsonPropertyName("data")]
        public List<OrderInvoiceRow>? Data { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
