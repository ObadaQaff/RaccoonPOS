using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Insurance;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Insurance;
using Xunit;

namespace RaccoonWarehouse.Tests;

public sealed class InsuranceCoverageServiceTests
{
    [Fact]
    public async Task CalculateAsync_AppliesCopayAndDeductibleFromSpecificRule()
    {
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(nameof(CalculateAsync_AppliesCopayAndDeductibleFromSpecificRule)).Options);
        var version = new InsuranceContractVersion
        {
            InsuranceContractId = 1,
            VersionNumber = 1,
            IsActive = true,
            ValidFrom = DateTime.Today,
            ValidTo = DateTime.Today.AddYears(1),
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };
        context.InsuranceContractVersions.Add(version);
        await context.SaveChangesAsync();
        context.InsuranceCoverageRules.Add(new InsuranceCoverageRule
        {
            InsuranceContractVersionId = version.Id,
            ProductId = 5,
            CoveragePercent = 80m,
            CopayPercent = 10m,
            DeductibleAmount = 2m,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        });
        await context.SaveChangesAsync();

        var result = await new InsuranceCoverageService(context).CalculateAsync(version.Id, null, new[]
        {
            new InsuranceCoverageLineInput { ProductId = 5, Quantity = 1, UnitPrice = 100m }
        });

        Assert.True(result.Success);
        Assert.Equal(100m, result.Data!.GrossAmount);
        Assert.Equal(70m, result.Data.CoveredAmount);
        Assert.Equal(30m, result.Data.PatientAmount);
    }

    [Fact]
    public async Task CalculateAsync_AppliesProductDiscountBeforeCoverage()
    {
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(nameof(CalculateAsync_AppliesProductDiscountBeforeCoverage)).Options);
        var version = new InsuranceContractVersion
        {
            InsuranceContractId = 1, VersionNumber = 1, IsActive = true,
            ValidFrom = DateTime.Today, ValidTo = DateTime.Today.AddYears(1), CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now
        };
        context.InsuranceContractVersions.Add(version);
        await context.SaveChangesAsync();
        context.InsuranceCoverageRules.Add(new InsuranceCoverageRule
        {
            InsuranceContractVersionId = version.Id, ProductId = 7, CoveragePercent = 100m,
            DiscountPercent = 10m, CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now
        });
        await context.SaveChangesAsync();

        var result = await new InsuranceCoverageService(context).CalculateAsync(version.Id, null, new[]
        {
            new InsuranceCoverageLineInput { ProductId = 7, Quantity = 1, UnitPrice = 100m }
        });

        Assert.True(result.Success);
        Assert.Equal(10m, result.Data!.InsuranceDiscountAmount);
        Assert.Equal(90m, result.Data.GrossAmount);
        Assert.Equal(90m, result.Data.CoveredAmount);
        Assert.Equal(0m, result.Data.PatientAmount);
    }
}
