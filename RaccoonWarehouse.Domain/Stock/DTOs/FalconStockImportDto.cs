namespace RaccoonWarehouse.Domain.Stock.DTOs
{
    #region Temporary Falcon API Integration

    public sealed class FalconStockImportRequestDto
    {
        public int WarehouseId { get; set; }
        public int? UserId { get; set; }
    }

    public sealed class FalconStockImportResultDto
    {
        public int ApiItemCount { get; set; }
        public int PositiveApiItemCount { get; set; }
        public int MatchedProductCount { get; set; }
        public int IncreasedProductCount { get; set; }
        public int DecreasedProductCount { get; set; }
        public int UnchangedProductCount { get; set; }
        public int UnmatchedProductCount { get; set; }
        public int IgnoredItemCount { get; set; }
        public int? StockDocumentId { get; set; }
        public string? StockDocumentNumber { get; set; }
    }

    #endregion
}
