using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Insurance;

namespace RaccoonWarehouse.Application.Service.Insurance;

public sealed class InsuranceCoverageLineInput
{
    public int ProductId { get; init; }
    public int? CategoryId { get; init; }
    public string? ServiceCode { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal ApprovedQuantity { get; init; }
    public int? PreAuthorizationId { get; init; }
}

public sealed class InsuranceCoverageLineResult
{
    public int ProductId { get; init; }
    public decimal Quantity { get; init; }
    public decimal GrossAmount { get; init; }
    public decimal CoveredAmount { get; init; }
    public decimal PatientAmount { get; init; }
    public decimal ExcludedAmount { get; init; }
    public decimal DeductibleAmount { get; init; }
    public decimal CopayAmount { get; init; }
    public decimal InsuranceDiscountPercent { get; init; }
    public decimal InsuranceDiscountAmount { get; init; }
    public decimal ContractedUnitPrice { get; init; }
    public decimal ApprovedQuantity { get; init; }
    public bool RequiresAuthorization { get; init; }
    public bool IsExcluded { get; init; }
    public string? DecisionReason { get; init; }
}

public sealed class InsuranceCoverageCalculationResult
{
    public IReadOnlyList<InsuranceCoverageLineResult> Lines { get; init; } = Array.Empty<InsuranceCoverageLineResult>();
    public decimal GrossAmount => Lines.Sum(x => x.GrossAmount);
    public decimal CoveredAmount => Lines.Sum(x => x.CoveredAmount);
    public decimal PatientAmount => Lines.Sum(x => x.PatientAmount);
    public decimal ExcludedAmount => Lines.Sum(x => x.ExcludedAmount);
    public decimal DeductibleAmount => Lines.Sum(x => x.DeductibleAmount);
    public decimal CopayAmount => Lines.Sum(x => x.CopayAmount);
    public decimal InsuranceDiscountAmount => Lines.Sum(x => x.InsuranceDiscountAmount);
}

public interface IInsuranceCoverageService
{
    Task<Result<InsuranceCoverageCalculationResult>> CalculateAsync(
        int contractVersionId,
        int? planId,
        IEnumerable<InsuranceCoverageLineInput> lines,
        CancellationToken cancellationToken = default);
}

public sealed class InsuranceCoverageService : IInsuranceCoverageService
{
    private readonly ApplicationDbContext _db;

    public InsuranceCoverageService(ApplicationDbContext db) => _db = db;

    public async Task<Result<InsuranceCoverageCalculationResult>> CalculateAsync(
        int contractVersionId,
        int? planId,
        IEnumerable<InsuranceCoverageLineInput> lines,
        CancellationToken cancellationToken = default)
    {
        var inputs = lines?.ToList() ?? new List<InsuranceCoverageLineInput>();
        if (contractVersionId <= 0 || inputs.Count == 0)
            return Result<InsuranceCoverageCalculationResult>.Fail("Contract version and invoice lines are required.");
        if (inputs.Any(x => x.ProductId <= 0 || x.Quantity <= 0 || x.UnitPrice < 0 || x.DiscountAmount < 0))
            return Result<InsuranceCoverageCalculationResult>.Fail("Coverage lines contain invalid quantity, price, or discount values.");

        var versionExists = await _db.InsuranceContractVersions.AsNoTracking()
            .AnyAsync(x => x.Id == contractVersionId && x.IsActive
                && x.ValidFrom <= DateTime.Today && x.ValidTo >= DateTime.Today, cancellationToken);
        if (!versionExists)
            return Result<InsuranceCoverageCalculationResult>.Fail("The selected insurance contract version is not active.");

        if (planId.HasValue && !await _db.InsurancePlans.AsNoTracking().AnyAsync(x => x.Id == planId.Value
            && x.Status == InsuranceRecordStatus.Active && x.ValidFrom <= DateTime.Today && x.ValidTo >= DateTime.Today, cancellationToken))
            return Result<InsuranceCoverageCalculationResult>.Fail("The selected insurance plan is not valid for today.");

        var rules = await _db.InsuranceCoverageRules.AsNoTracking()
            .Where(x => x.InsuranceContractVersionId == contractVersionId
                && (x.ValidFrom == default || x.ValidFrom <= DateTime.Today)
                && (x.ValidTo == default || x.ValidTo >= DateTime.Today))
            .ToListAsync(cancellationToken);
        var exclusions = await _db.InsuranceExclusions.AsNoTracking()
            .Where(x => x.InsuranceContractVersionId == contractVersionId)
            .ToListAsync(cancellationToken);

        var results = new List<InsuranceCoverageLineResult>(inputs.Count);
        foreach (var input in inputs)
        {
            var originalGross = Math.Max(0m, input.Quantity * input.UnitPrice - input.DiscountAmount);
            var exclusion = exclusions.FirstOrDefault(x =>
                (!x.InsurancePlanId.HasValue || x.InsurancePlanId == planId)
                && Matches(x.ProductId, x.CategoryId, x.ServiceCode, input));
            var rule = rules
                .Where(x => !x.IsExcluded
                    && (!x.InsurancePlanId.HasValue || x.InsurancePlanId == planId)
                    && Matches(x.ProductId, x.CategoryId, x.ServiceCode, input))
                .OrderByDescending(x => x.ProductId.HasValue ? 3 : x.CategoryId.HasValue ? 2 : !string.IsNullOrWhiteSpace(x.ServiceCode) ? 1 : 0)
                .FirstOrDefault();

            var approvedQuantity = input.ApprovedQuantity <= 0 ? input.Quantity : Math.Min(input.Quantity, input.ApprovedQuantity);
            var requiresAuthorization = rule?.RequiresAuthorization == true && !input.PreAuthorizationId.HasValue;
            var isExcluded = exclusion != null || rule?.IsExcluded == true;
            if (isExcluded)
            {
                results.Add(new InsuranceCoverageLineResult
                {
                    ProductId = input.ProductId, Quantity = input.Quantity, GrossAmount = originalGross,
                    PatientAmount = originalGross, ExcludedAmount = originalGross, ApprovedQuantity = 0,
                    IsExcluded = true, DecisionReason = exclusion?.Reason ?? rule?.Notes ?? "Excluded by contract rule."
                });
                continue;
            }

            if (rule == null || requiresAuthorization)
            {
                results.Add(new InsuranceCoverageLineResult
                {
                    ProductId = input.ProductId, Quantity = input.Quantity, GrossAmount = originalGross,
                    PatientAmount = originalGross, ContractedUnitPrice = input.UnitPrice,
                    ApprovedQuantity = requiresAuthorization ? approvedQuantity : 0,
                    RequiresAuthorization = requiresAuthorization,
                    DecisionReason = requiresAuthorization ? "Prior authorization is required." : "No matching coverage rule was configured."
                });
                continue;
            }

            var discountAmount = Math.Min(originalGross,
                originalGross * Math.Clamp(rule.DiscountPercent, 0m, 100m) / 100m + Math.Max(0m, rule.FixedDiscountAmount));
            var contractedGross = Math.Max(0m, originalGross - discountAmount);
            var contractedUnitPrice = input.Quantity == 0 ? 0m : contractedGross / input.Quantity;
            var coveredBase = Math.Min(contractedGross, contractedGross * Math.Clamp(rule.CoveragePercent, 0m, 100m) / 100m);
            if (rule.PerClaimLimit.HasValue)
                coveredBase = Math.Min(coveredBase, Math.Max(0m, rule.PerClaimLimit.Value));
            var copay = Math.Min(coveredBase, coveredBase * Math.Clamp(rule.CopayPercent, 0m, 100m) / 100m);
            var deductible = Math.Min(coveredBase - copay, Math.Max(0m, rule.DeductibleAmount));
            var covered = Math.Max(0m, coveredBase - copay - deductible);
            var patient = Math.Max(0m, contractedGross - covered);
            results.Add(new InsuranceCoverageLineResult
            {
                ProductId = input.ProductId, Quantity = input.Quantity, GrossAmount = contractedGross,
                CoveredAmount = covered, PatientAmount = patient, DeductibleAmount = deductible,
                CopayAmount = copay, ApprovedQuantity = approvedQuantity,
                InsuranceDiscountPercent = rule.DiscountPercent, InsuranceDiscountAmount = discountAmount,
                ContractedUnitPrice = contractedUnitPrice,
                DecisionReason = "Covered by configured contract rule."
            });
        }

        return Result<InsuranceCoverageCalculationResult>.Ok(new InsuranceCoverageCalculationResult { Lines = results });
    }

    private static bool Matches(int? productId, int? categoryId, string? serviceCode, InsuranceCoverageLineInput input) =>
        (!productId.HasValue || productId.Value == input.ProductId)
        && (!categoryId.HasValue || categoryId.Value == input.CategoryId)
        && (string.IsNullOrWhiteSpace(serviceCode) || string.Equals(serviceCode, input.ServiceCode, StringComparison.OrdinalIgnoreCase));
}
