namespace RaccoonWarehouse.Domain.Reports.Financial.Dtos
{
    public class AgingRow
    {
        public int? PartyId { get; set; }
        public string PartyName { get; set; } = string.Empty;
        public decimal Current { get; set; }
        public decimal Days1to30 { get; set; }
        public decimal Days31to60 { get; set; }
        public decimal Days61to90 { get; set; }
        public decimal Over90 { get; set; }
        public decimal Total { get; set; }
    }
}
