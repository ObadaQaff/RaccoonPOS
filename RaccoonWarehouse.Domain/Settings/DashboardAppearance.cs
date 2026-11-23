namespace RaccoonWarehouse.Domain.Settings;

public sealed class DashboardAppearance
{
    public string SidebarColor { get; set; } = "#0B2D45";
    public string SidebarHoverColor { get; set; } = "#124B67";
    public string SidebarPressedColor { get; set; } = "#0C8FA3";
    public string PrimaryButtonColor { get; set; } = "#16B5E8";
    public string PrimaryButtonHoverColor { get; set; } = "#087BA8";
    public string HeaderStartColor { get; set; } = "#0B2D45";
    public string HeaderMiddleColor { get; set; } = "#16B5E8";
    public string HeaderEndColor { get; set; } = "#18C9B3";
    public string SidebarTitle { get; set; } = "مدير المستودع";
    public string? LogoBase64 { get; set; }
}
