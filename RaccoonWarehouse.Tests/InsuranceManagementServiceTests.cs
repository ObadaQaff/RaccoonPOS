using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Insurance;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Insurance;
using Xunit;

namespace RaccoonWarehouse.Tests;

public sealed class InsuranceManagementServiceTests
{
    private static ApplicationDbContext CreateContext(string name) => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options);

    [Fact]
    public async Task SaveCompanyAndPlan_PreservesSeparateOrganizationRole()
    {
        await using var context = CreateContext(nameof(SaveCompanyAndPlan_PreservesSeparateOrganizationRole));
        var service = new InsuranceManagementService(context);

        var company = await service.SaveCompanyAsync(new InsuranceCompanyWriteDto
        {
            Code = "TPA-01",
            Name = "Administrator",
            OrganizationType = InsuranceOrganizationType.Administrator
        });
        Assert.True(company.Success);

        var contract = new InsuranceContract
        {
            ContractNumber = "C-01",
            InsuranceCompanyId = company.Data!.Id,
            Name = "Contract",
            ValidFrom = DateTime.Today,
            ValidTo = DateTime.Today.AddYears(1),
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };
        context.InsuranceContracts.Add(contract);
        await context.SaveChangesAsync();

        var plan = await service.SavePlanAsync(new InsurancePlanWriteDto
        {
            InsuranceContractId = contract.Id,
            Code = "PLAN-A",
            Name = "Standard",
            ValidFrom = DateTime.Today,
            ValidTo = DateTime.Today.AddYears(1),
            Status = InsuranceRecordStatus.Active
        });

        Assert.True(plan.Success);
        Assert.Equal("PLAN-A", plan.Data!.Code);
        Assert.Equal(InsuranceOrganizationType.Administrator,
            (await context.InsuranceCompanies.SingleAsync()).OrganizationType);
    }

    [Fact]
    public async Task ReviewClaim_RejectsAmountsAboveSubmittedValue()
    {
        await using var context = CreateContext(nameof(ReviewClaim_RejectsAmountsAboveSubmittedValue));
        var claim = new InsuranceClaim
        {
            ClaimNumber = "CLM-1",
            InvoiceId = 10,
            InsuranceInvoiceCoverageId = 20,
            InsuranceCompanyId = 1,
            InsuranceMemberPolicyId = 2,
            SubmittedAmount = 30m,
            OutstandingAmount = 30m,
            Status = InsuranceClaimStatus.Prepared,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };
        context.InsuranceClaims.Add(claim);
        await context.SaveChangesAsync();

        var result = await new InsuranceManagementService(context).ReviewClaimAsync(new InsuranceClaimReviewDto
        {
            ClaimId = claim.Id,
            Status = InsuranceClaimStatus.Approved,
            ApprovedAmount = 31m
        });

        Assert.False(result.Success);
        Assert.Equal(30m, (await context.InsuranceClaims.SingleAsync()).SubmittedAmount);
    }

    [Fact]
    public async Task ReviewClaim_RequiresDecisionAmountsToReconcile()
    {
        await using var context = CreateContext(nameof(ReviewClaim_RequiresDecisionAmountsToReconcile));
        var claim = new InsuranceClaim
        {
            ClaimNumber = "CLM-RECON-1",
            InvoiceId = 10,
            InsuranceInvoiceCoverageId = 20,
            InsuranceCompanyId = 1,
            InsuranceMemberPolicyId = 2,
            SubmittedAmount = 30m,
            OutstandingAmount = 30m,
            Status = InsuranceClaimStatus.Prepared,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };
        context.InsuranceClaims.Add(claim);
        await context.SaveChangesAsync();

        var result = await new InsuranceManagementService(context).ReviewClaimAsync(new InsuranceClaimReviewDto
        {
            ClaimId = claim.Id,
            Status = InsuranceClaimStatus.PartiallyApproved,
            ApprovedAmount = 20m
        });

        Assert.False(result.Success);
        Assert.Contains("equal the submitted amount", result.Message);
    }

    [Fact]
    public async Task CreateClaimBatch_AssignsPreparedClaimsAndAgingReadsOutstandingBalance()
    {
        await using var context = CreateContext(nameof(CreateClaimBatch_AssignsPreparedClaimsAndAgingReadsOutstandingBalance));
        var now = DateTime.Now;
        var claim = new InsuranceClaim
        {
            ClaimNumber = "CLM-BATCH-1",
            InvoiceId = 1,
            InsuranceInvoiceCoverageId = 1,
            InsuranceCompanyId = 9,
            InsuranceMemberPolicyId = 1,
            Status = InsuranceClaimStatus.Prepared,
            SubmittedAmount = 50m,
            OutstandingAmount = 50m,
            CreatedDate = now.AddDays(-10),
            UpdatedDate = now
        };
        context.InsuranceClaims.Add(claim);
        await context.SaveChangesAsync();

        var service = new InsuranceManagementService(context);
        var batch = await service.CreateClaimBatchAsync(9, now.AddDays(-20), now);
        var aging = await service.GetReceivableAgingAsync(9, now);

        Assert.True(batch.Success);
        Assert.Equal(1, batch.Data!.ClaimCount);
        Assert.Equal(batch.Data.Id, (await context.InsuranceClaims.SingleAsync()).InsuranceClaimBatchId);
        Assert.Contains(aging.Data!, row => row.Bucket == "0-30" && row.Amount == 50m);
    }

    [Fact]
    public async Task SaveContractVersionAndCoverageRule_UsesExistingInsuranceTables()
    {
        await using var context = CreateContext(nameof(SaveContractVersionAndCoverageRule_UsesExistingInsuranceTables));
        var company = new InsuranceCompany { Code = "INS-1", Name = "Insurer", CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now };
        context.InsuranceCompanies.Add(company);
        await context.SaveChangesAsync();
        var contract = new InsuranceContract
        {
            ContractNumber = "C-100", InsuranceCompanyId = company.Id, Name = "Main contract",
            ValidFrom = DateTime.Today, ValidTo = DateTime.Today.AddYears(1), CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now
        };
        context.InsuranceContracts.Add(contract);
        await context.SaveChangesAsync();
        var service = new InsuranceManagementService(context);

        var version = await service.SaveContractVersionAsync(new InsuranceContractVersionWriteDto
        {
            InsuranceContractId = contract.Id, VersionNumber = 1, ValidFrom = DateTime.Today,
            ValidTo = DateTime.Today.AddYears(1), IsActive = true
        });
        var rule = await service.SaveCoverageRuleAsync(new InsuranceCoverageRuleWriteDto
        {
            InsuranceContractVersionId = version.Data!.Id, ProductId = 55, CoveragePercent = 80m,
            CopayPercent = 10m, DeductibleAmount = 1m
        });

        Assert.True(version.Success);
        Assert.True(rule.Success);
        Assert.Contains((await service.GetCoverageRulesAsync(version.Data.Id)).Data!, x => x.ProductId == 55 && x.CoveragePercent == 80m);
    }

    [Fact]
    public async Task SaveCoverageRule_RejectsInvalidPercentages()
    {
        await using var context = CreateContext(nameof(SaveCoverageRule_RejectsInvalidPercentages));
        context.InsuranceContractVersions.Add(new InsuranceContractVersion
        {
            InsuranceContractId = 1, VersionNumber = 1, ValidFrom = DateTime.Today, ValidTo = DateTime.Today.AddYears(1),
            IsActive = true, CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now
        });
        await context.SaveChangesAsync();
        var versionId = (await context.InsuranceContractVersions.SingleAsync()).Id;

        var result = await new InsuranceManagementService(context).SaveCoverageRuleAsync(new InsuranceCoverageRuleWriteDto
        {
            InsuranceContractVersionId = versionId, ProductId = 1, CoveragePercent = 101m
        });

        Assert.False(result.Success);
    }
}
