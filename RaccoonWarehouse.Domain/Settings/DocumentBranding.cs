namespace RaccoonWarehouse.Domain.Settings;

public sealed class DocumentBranding
{
    public string DisplayName { get; set; } = "";
    public string Address { get; set; } = "";
    public string Phone { get; set; } = "";
    public string TaxNumber { get; set; } = "";
    public string AdditionalInformation { get; set; } = "";
    public string Footer { get; set; } = "";
    // Embedded PNG/JPEG, shared across workstations, never a machine-specific path.
    public string? LogoBase64 { get; set; }
    public int ThermalPaperWidthMm { get; set; } = 80;
    public bool EscPosCutEnabled { get; set; }
}
