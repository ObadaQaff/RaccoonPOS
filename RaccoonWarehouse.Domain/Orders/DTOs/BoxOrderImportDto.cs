namespace RaccoonWarehouse.Domain.Orders.DTOs
{
    public sealed class BoxOrderImportResultDto
    {
        public int ReceivedCount { get; set; }
        public int ImportedCount { get; set; }
        public int ExistingCount { get; set; }
        public int SkippedCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public sealed class BoxOrderExportDto
    {
        public int CartId { get; set; }
        public int UserId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? ShopName { get; set; }
        public DateTime CreatedDate { get; set; }
        public decimal TotalPrice { get; set; }
        public List<BoxOrderExportItemDto> Items { get; set; } = new();
    }

    public sealed class BoxOrderExportItemDto
    {
        public int CartItemId { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? UnitName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public sealed class BoxPendingOrdersSnapshotDto
    {
        public int Count => Orders.Count;
        public List<BoxOrderExportDto> Orders { get; set; } = new();
    }

    public sealed class EndpointOrderEditDto
    {
        public int InvoiceId { get; set; }
        public List<EndpointOrderLocalLineDto> Lines { get; set; } = new();
    }

    public sealed class EndpointOrderLocalLineDto
    {
        public int InvoiceLineId { get; set; }
        public int ProductId { get; set; }
        public int ProductUnitId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public sealed class EndpointOrderLineEditDto
    {
        public int InvoiceLineId { get; set; }
        public int CartItemId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public sealed class BoxCartWriteDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int UserId { get; set; }
        public List<BoxCartItemWriteDto> CartItems { get; set; } = new();
        public decimal? TotalPrice { get; set; }
        public int CartStatus { get; set; }
        public DateTime PickUpTime { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }

    public sealed class BoxCartItemWriteDto
    {
        public int Id { get; set; }
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public int UnitId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
