using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Settings;

namespace RaccoonWarehouse.Application.Service.Settings;

public interface IRefundAuthorizationCodeService
{
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default);
    Task<bool> ValidateCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<Result> SetCodeAsync(string code, CancellationToken cancellationToken = default);
}

public sealed class RefundAuthorizationCodeService : IRefundAuthorizationCodeService
{
    public const string SettingKey = "Security.RefundAuthorizationCode";
    private const int Iterations = 120_000;
    private readonly ApplicationDbContext _dbContext;

    public RefundAuthorizationCodeService(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default)
        => !string.IsNullOrWhiteSpace(await GetHashAsync(cancellationToken));

    public async Task<bool> ValidateCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        var stored = await GetHashAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(stored))
            return false;

        var parts = stored.Split('$');
        if (parts.Length != 4 || parts[0] != "PBKDF2" || !int.TryParse(parts[1], out var iterations))
            return false;

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(code.Trim(), salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public async Task<Result> SetCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code?.Trim() ?? string.Empty;
        if (normalized.Length < 4 || normalized.Length > 32)
            return Result.Fail("The refund code must contain between 4 and 32 characters.");

        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(normalized, salt, Iterations, HashAlgorithmName.SHA256, 32);
        var value = $"PBKDF2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        var setting = await _dbContext.AppSettings.FirstOrDefaultAsync(x => x.Key == SettingKey, cancellationToken);

        if (setting == null)
        {
            _dbContext.AppSettings.Add(new AppSetting
            {
                Key = SettingKey,
                Value = value,
                Description = "Dedicated authorization code for sales refunds; never use a user password.",
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
        return Result.Ok("Refund authorization code saved.");
    }

    private Task<string?> GetHashAsync(CancellationToken cancellationToken)
        => _dbContext.AppSettings.AsNoTracking()
            .Where(x => x.Key == SettingKey)
            .Select(x => x.Value)
            .FirstOrDefaultAsync(cancellationToken);
}
