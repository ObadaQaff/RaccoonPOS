namespace RaccoonWarehouse.Domain.Settings;

public sealed class DashboardAppearance
{
    public string SidebarColor { get; set; } = "#22343C";
    public string SidebarHoverColor { get; set; } = "#2E4954";
    public string SidebarPressedColor { get; set; } = "#33596F7A";
    public string PrimaryButtonColor { get; set; } = "#145A4A";
    public string PrimaryButtonHoverColor { get; set; } = "#0F4A3E";
    public string HeaderStartColor { get; set; } = "#1B2E36";
    public string HeaderMiddleColor { get; set; } = "#145A4A";
    public string HeaderEndColor { get; set; } = "#134B70";
    public string SidebarTitle { get; set; } = "مدير المستودع";
    public string? LogoBase64 { get; set; }
}
