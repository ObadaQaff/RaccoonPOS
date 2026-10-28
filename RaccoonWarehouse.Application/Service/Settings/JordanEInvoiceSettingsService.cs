using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Settings;

namespace RaccoonWarehouse.Application.Service.Settings;

public sealed record JordanEInvoiceSettingsDto(
    bool Enabled,
    string ClientId,
    string SecretKey,
    string IncomeSourceSequence,
    string TaxRegistrationType = "2");

public interface IJordanEInvoiceSettingsService
{
    Task<JordanEInvoiceSettingsDto> GetAsync(CancellationToken cancellationToken = default);
    Task<Result> SaveAsync(JordanEInvoiceSettingsDto settings, CancellationToken cancellationToken = default);
}

public sealed class JordanEInvoiceSettingsService : IJordanEInvoiceSettingsService
{
    public const string SettingKey = "Integration.JoFotara";

    private readonly ApplicationDbContext _dbContext;

    public JordanEInvoiceSettingsService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<JordanEInvoiceSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var value = await _dbContext.AppSettings
            .AsNoTracking()
            .Where(setting => setting.Key == SettingKey)
            .Select(setting => setting.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(value))
            return new JordanEInvoiceSettingsDto(false, string.Empty, string.Empty, string.Empty);

        try
        {
            var settings = JsonSerializer.Deserialize<JordanEInvoiceSettingsDto>(value)
                ?? new JordanEInvoiceSettingsDto(false, string.Empty, string.Empty, string.Empty);
            return settings with
            {
                TaxRegistrationType = string.IsNullOrWhiteSpace(settings.TaxRegistrationType)
                    ? "2"
                    : settings.TaxRegistrationType.Trim()
            };
        }
        catch (JsonException)
        {
            return new JordanEInvoiceSettingsDto(false, string.Empty, string.Empty, string.Empty);
        }
    }

    public async Task<Result> SaveAsync(
        JordanEInvoiceSettingsDto settings,
        CancellationToken cancellationToken = default)
    {
var normalized = settings with
        {
            ClientId = settings.ClientId?.Trim() ?? string.Empty,
            SecretKey = settings.SecretKey?.Trim() ?? string.Empty,
            IncomeSourceSequence = settings.IncomeSourceSequence?.Trim() ?? string.Empty,
            TaxRegistrationType = string.IsNullOrWhiteSpace(settings.TaxRegistrationType)
                ? "2"
                : settings.TaxRegistrationType.Trim()
        };

        if (normalized.TaxRegistrationType is not ("1" or "2" or "3"))
            return Result.Fail("Tax registration type must be 1 (income tax), 2 (general sales tax), or 3 (special sales tax). / يجب أن يكون نوع التسجيل الضريبي 1 أو 2 أو 3.");

        if (normalized.Enabled &&
            (string.IsNullOrWhiteSpace(normalized.ClientId) ||
             string.IsNullOrWhiteSpace(normalized.SecretKey) ||
             string.IsNullOrWhiteSpace(normalized.IncomeSourceSequence)))
        {
            return Result.Fail("Client ID, Secret Key, and Income Source Sequence are required when JoFotara is enabled / يجب إدخال رقم المستخدم والمفتاح السري وتسلسل مصدر الدخل عند تفعيل جوافوترة.");
        }

        var value = JsonSerializer.Serialize(normalized);
        var setting = await _dbContext.AppSettings
            .FirstOrDefaultAsync(item => item.Key == SettingKey, cancellationToken);

        if (setting == null)
        {
            _dbContext.AppSettings.Add(new AppSetting
            {
                Key = SettingKey,
                Value = value,
                Description = "JoFotara sales e-invoice integration settings.",
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            });
        }
        else
        {
            setting.Value = value;
            setting.UpdatedDate = DateTime.Now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Ok("JoFotara settings saved / تم حفظ إعدادات جوافوترة.");
    }
}
