namespace RaccoonWarehouse.Domain.Settings;

public sealed class DashboardAppearance
{
    public string SidebarColor { get; set; } = "#211B4B";
    public string SidebarHoverColor { get; set; } = "#30266D";
    public string SidebarPressedColor { get; set; } = "#4938A0";
    public string PrimaryButtonColor { get; set; } = "#5B2BE0";
    public string PrimaryButtonHoverColor { get; set; } = "#4520B2";
    public string HeaderStartColor { get; set; } = "#1D1740";
    public string HeaderMiddleColor { get; set; } = "#39268A";
    public string HeaderEndColor { get; set; } = "#127C9A";
    public string SidebarTitle { get; set; } = "مدير المستودع";
    public string? LogoBase64 { get; set; }
}
