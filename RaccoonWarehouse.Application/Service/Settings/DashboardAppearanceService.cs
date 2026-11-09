using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Settings;

namespace RaccoonWarehouse.Application.Service.Settings;

public sealed class DashboardAppearanceService
{
    public const string Key = "Dashboard.Appearance.v1";
    private static readonly Regex HexColor = new("^#[0-9a-fA-F]{3,8}$", RegexOptions.Compiled);
    private readonly ApplicationDbContext _db;

    public DashboardAppearanceService(ApplicationDbContext db) => _db = db;

    public async Task<DashboardAppearance> GetAsync(CancellationToken cancellationToken = default)
    {
        var json = await _db.AppSettings.AsNoTracking()
            .Where(x => x.Key == Key)
            .Select(x => x.Value)
            .SingleOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(json))
            return new DashboardAppearance();

        try
        {
            var value = JsonSerializer.Deserialize<DashboardAppearance>(json);
            return value ?? new DashboardAppearance();
        }
        catch (JsonException)
        {
            return new DashboardAppearance();
        }
    }

    public async Task SaveAsync(DashboardAppearance value, CancellationToken cancellationToken = default)
    {
        NormalizeAndValidate(value);
        var row = await _db.AppSettings.SingleOrDefaultAsync(x => x.Key == Key, cancellationToken);
        if (row == null)
        {
            row = new AppSetting { Key = Key, Description = "Dashboard colors, title and logo", CreatedDate = DateTime.Now };
            _db.AppSettings.Add(row);
        }

        row.Value = JsonSerializer.Serialize(value);
        row.UpdatedDate = DateTime.Now;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public static void NormalizeAndValidate(DashboardAppearance value)
    {
        ArgumentNullException.ThrowIfNull(value);
        value.SidebarColor = NormalizeColor(value.SidebarColor, nameof(value.SidebarColor));
        value.SidebarHoverColor = NormalizeColor(value.SidebarHoverColor, nameof(value.SidebarHoverColor));
        value.SidebarPressedColor = NormalizeColor(value.SidebarPressedColor, nameof(value.SidebarPressedColor));
        value.PrimaryButtonColor = NormalizeColor(value.PrimaryButtonColor, nameof(value.PrimaryButtonColor));
        value.PrimaryButtonHoverColor = NormalizeColor(value.PrimaryButtonHoverColor, nameof(value.PrimaryButtonHoverColor));
        value.HeaderStartColor = NormalizeColor(value.HeaderStartColor, nameof(value.HeaderStartColor));
        value.HeaderMiddleColor = NormalizeColor(value.HeaderMiddleColor, nameof(value.HeaderMiddleColor));
        value.HeaderEndColor = NormalizeColor(value.HeaderEndColor, nameof(value.HeaderEndColor));
        value.SidebarTitle = (value.SidebarTitle ?? string.Empty).Trim();
        if (value.SidebarTitle.Length > 120)
            throw new ArgumentException("The sidebar title cannot exceed 120 characters.");

        if (!string.IsNullOrWhiteSpace(value.LogoBase64))
        {
            byte[] bytes;
            try { bytes = Convert.FromBase64String(value.LogoBase64); }
            catch (FormatException) { throw new ArgumentException("Invalid logo image."); }
            if (bytes.Length is < 8 or > 1024 * 1024 ||
                !(bytes[0] == 137 && bytes[1] == 80 && bytes[2] == 78 && bytes[3] == 71 ||
                  bytes[0] == 255 && bytes[1] == 216))
                throw new ArgumentException("Use a PNG or JPEG logo up to 1 MB.");
        }
    }

    private static string NormalizeColor(string? value, string name)
    {
        value = value?.Trim() ?? string.Empty;
        if (!HexColor.IsMatch(value) || value.Length is not (4 or 5 or 7 or 9))
            throw new ArgumentException($"{name} must be a color such as #FFF or #145A4A.");

        if (value.Length == 4)
            return $"#{value[1]}{value[1]}{value[2]}{value[2]}{value[3]}{value[3]}".ToUpperInvariant();
        return value.ToUpperInvariant();
    }
}
