using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Core.Audit;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Insurance;

namespace RaccoonWarehouse.Application.Service.Insurance;

public interface IInsuranceManagementService
{
    Task<Result<List<InsuranceCompanyDto>>> GetCompaniesAsync(bool activeOnly = false);
    Task<Result<InsuranceCompanyDto>> SaveCompanyAsync(InsuranceCompanyWriteDto dto);
    Task<Result<List<InsurancePlanDto>>> GetPlansAsync(int contractId, bool activeOnly = false);
    Task<Result<InsurancePlanDto>> SavePlanAsync(InsurancePlanWriteDto dto);
    Task<Result<List<InsuranceContractVersionDto>>> GetContractVersionsAsync(int contractId);
    Task<Result<InsuranceContractVersionDto>> SaveContractVersionAsync(InsuranceContractVersionWriteDto dto);
    Task<Result<List<InsuranceCoverageRuleDto>>> GetCoverageRulesAsync(int contractVersionId, int? planId = null);
    Task<Result<InsuranceCoverageRuleDto>> SaveCoverageRuleAsync(InsuranceCoverageRuleWriteDto dto);
    Task<Result<List<InsuranceContractDto>>> GetContractsAsync(int? companyId = null, bool activeOnly = false);
    Task<Result<InsuranceContractDto>> SaveContractAsync(InsuranceContractWriteDto dto);
    Task<Result<List<InsuranceMemberPolicyDto>>> GetMemberPoliciesAsync(int? customerId = null, int? companyId = null);
    Task<Result<InsuranceMemberPolicyDto>> SaveMemberPolicyAsync(InsuranceMemberPolicyWriteDto dto);
    Task<Result<InsuranceEligibilityCheck>> RecordEligibilityCheckAsync(InsuranceEligibilityCheckWriteDto dto);
    Task<Result<List<InsuranceClaimSummaryDto>>> GetClaimsAsync(int? companyId = null, InsuranceClaimStatus? status = null);
    Task<Result<InsuranceClaimSummaryDto>> CreateClaimAsync(int invoiceId, int coverageId);
    Task<Result<InsuranceClaimSummaryDto>> ReviewClaimAsync(InsuranceClaimReviewDto dto);
    Task<Result<InsuranceClaimBatchDto>> CreateClaimBatchAsync(int companyId, DateTime periodFrom, DateTime periodTo);
    Task<Result<List<InsuranceReceivableAgingDto>>> GetReceivableAgingAsync(int? companyId = null, DateTime? asOf = null);
    Task<Result<InsuranceClaimPayment>> RecordPaymentAsync(InsuranceClaimPaymentWriteDto dto);
}

public sealed class InsuranceManagementService : IInsuranceManagementService
{
    private readonly ApplicationDbContext _db;
    private readonly IPermissionService? _permissions;
    private readonly IUserSession? _userSession;
    private readonly IAuditLogService? _audit;

    public InsuranceManagementService(
        ApplicationDbContext db,
        IPermissionService? permissions = null,
        IUserSession? userSession = null,
        IAuditLogService? audit = null)
    {
        _db = db;
        _permissions = permissions;
        _userSession = userSession;
        _audit = audit;
    }

    public async Task<Result<List<InsuranceCompanyDto>>> GetCompaniesAsync(bool activeOnly = false)
    {
        var query = _db.InsuranceCompanies.AsNoTracking().AsQueryable();
        if (activeOnly)
            query = query.Where(x => x.Status == InsuranceRecordStatus.Active);

        var companies = await query
            .OrderBy(x => x.Name)
            .Select(x => new InsuranceCompanyDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                NameEnglish = x.NameEnglish,
                TaxNumber = x.TaxNumber,
                Phone = x.Phone,
                Email = x.Email,
                Address = x.Address,
                Status = x.Status,
                OrganizationType = x.OrganizationType,
                CreatedDate = x.CreatedDate,
                UpdatedDate = x.UpdatedDate
            })
            .ToListAsync();

        return Result<List<InsuranceCompanyDto>>.Ok(companies);
    }

    public async Task<Result<InsuranceCompanyDto>> SaveCompanyAsync(InsuranceCompanyWriteDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Name))
            return Result<InsuranceCompanyDto>.Fail("Insurance company code and name are required.");

        var normalizedCode = dto.Code.Trim();
        var duplicate = await _db.InsuranceCompanies.AnyAsync(x =>
            x.Code == normalizedCode && x.Id != dto.Id);
        if (duplicate)
            return Result<InsuranceCompanyDto>.Fail("Insurance company code already exists.");

        InsuranceCompany company;
        if (dto.Id > 0)
        {
            company = await _db.InsuranceCompanies.FirstOrDefaultAsync(x => x.Id == dto.Id)
                ?? new InsuranceCompany { Id = dto.Id };
            if (company.Id == 0)
                return Result<InsuranceCompanyDto>.Fail("Insurance company was not found.");
        }
        else
        {
            company = new InsuranceCompany();
            await _db.InsuranceCompanies.AddAsync(company);
        }

        company.Code = normalizedCode;
        company.Name = dto.Name.Trim();
        company.NameEnglish = dto.NameEnglish?.Trim();
        company.TaxNumber = dto.TaxNumber?.Trim();
        company.Phone = dto.Phone?.Trim();
        company.Email = dto.Email?.Trim();
        company.Address = dto.Address?.Trim();
        company.Status = dto.Status;
        company.OrganizationType = dto.OrganizationType;
        company.ReceivableAccountId = dto.ReceivableAccountId;
        company.DeductionAccountId = dto.DeductionAccountId;
        company.PaymentAccountId = dto.PaymentAccountId;
        company.Notes = dto.Notes?.Trim();
        company.UpdatedDate = DateTime.Now;
        if (company.CreatedDate == default)
            company.CreatedDate = company.UpdatedDate;

        await _db.SaveChangesAsync();
        return Result<InsuranceCompanyDto>.Ok(new InsuranceCompanyDto
        {
            Id = company.Id,
            Code = company.Code,
            Name = company.Name,
            NameEnglish = company.NameEnglish,
            TaxNumber = company.TaxNumber,
            Phone = company.Phone,
            Email = company.Email,
            Address = company.Address,
            Status = company.Status,
            OrganizationType = company.OrganizationType,
            CreatedDate = company.CreatedDate,
            UpdatedDate = company.UpdatedDate
        });
    }

    public async Task<Result<List<InsurancePlanDto>>> GetPlansAsync(int contractId, bool activeOnly = false)
    {
        var query = _db.InsurancePlans.AsNoTracking().Where(x => x.InsuranceContractId == contractId);
        if (activeOnly)
            query = query.Where(x => x.Status == InsuranceRecordStatus.Active);

        var plans = await query.OrderBy(x => x.Name).Select(x => new InsurancePlanDto
        {
            Id = x.Id,
            InsuranceContractId = x.InsuranceContractId,
            InsuranceContractVersionId = x.InsuranceContractVersionId,
            Code = x.Code,
            Name = x.Name,
            NameEnglish = x.NameEnglish,
            ValidFrom = x.ValidFrom,
            ValidTo = x.ValidTo,
            Status = x.Status
        }).ToListAsync();
        return Result<List<InsurancePlanDto>>.Ok(plans);
    }

    public async Task<Result<InsurancePlanDto>> SavePlanAsync(InsurancePlanWriteDto dto)
    {
        if (dto.InsuranceContractId <= 0 || string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Name))
            return Result<InsurancePlanDto>.Fail("Insurance plan contract, code, and name are required.");
        if (dto.ValidTo < dto.ValidFrom)
            return Result<InsurancePlanDto>.Fail("Insurance plan end date cannot be before its start date.");

        var contractExists = await _db.InsuranceContracts.AnyAsync(x => x.Id == dto.InsuranceContractId);
        if (!contractExists)
            return Result<InsurancePlanDto>.Fail("Insurance contract was not found.");

        var code = dto.Code.Trim();
        if (await _db.InsurancePlans.AnyAsync(x => x.InsuranceContractId == dto.InsuranceContractId && x.Code == code && x.Id != dto.Id))
            return Result<InsurancePlanDto>.Fail("Insurance plan code already exists in this contract.");

        var plan = dto.Id > 0
            ? await _db.InsurancePlans.FirstOrDefaultAsync(x => x.Id == dto.Id)
            : new InsurancePlan();
        if (dto.Id > 0 && plan == null)
            return Result<InsurancePlanDto>.Fail("Insurance plan was not found.");
        plan ??= new InsurancePlan();
        if (dto.Id == 0)
            await _db.InsurancePlans.AddAsync(plan);

        plan.InsuranceContractId = dto.InsuranceContractId;
        plan.InsuranceContractVersionId = dto.InsuranceContractVersionId;
        plan.Code = code;
        plan.Name = dto.Name.Trim();
        plan.NameEnglish = dto.NameEnglish?.Trim();
        plan.ValidFrom = dto.ValidFrom;
        plan.ValidTo = dto.ValidTo;
        plan.Status = dto.Status;
        plan.Notes = dto.Notes?.Trim();
        plan.UpdatedDate = DateTime.Now;
        if (plan.CreatedDate == default)
            plan.CreatedDate = plan.UpdatedDate;
        await _db.SaveChangesAsync();

        return Result<InsurancePlanDto>.Ok(new InsurancePlanDto
        {
            Id = plan.Id,
            InsuranceContractId = plan.InsuranceContractId,
            InsuranceContractVersionId = plan.InsuranceContractVersionId,
            Code = plan.Code,
            Name = plan.Name,
            NameEnglish = plan.NameEnglish,
            ValidFrom = plan.ValidFrom,
            ValidTo = plan.ValidTo,
            Status = plan.Status
        });
    }

    public async Task<Result<List<InsuranceContractVersionDto>>> GetContractVersionsAsync(int contractId)
    {
        var versions = await _db.InsuranceContractVersions.AsNoTracking()
            .Where(x => x.InsuranceContractId == contractId)
            .OrderByDescending(x => x.VersionNumber)
            .Select(x => new InsuranceContractVersionDto
            {
                Id = x.Id, InsuranceContractId = x.InsuranceContractId, VersionNumber = x.VersionNumber,
                ValidFrom = x.ValidFrom, ValidTo = x.ValidTo, IsActive = x.IsActive
            }).ToListAsync();
        return Result<List<InsuranceContractVersionDto>>.Ok(versions);
    }

    public async Task<Result<InsuranceContractVersionDto>> SaveContractVersionAsync(InsuranceContractVersionWriteDto dto)
    {
        if (dto.InsuranceContractId <= 0 || dto.VersionNumber <= 0 || dto.ValidTo < dto.ValidFrom)
            return Result<InsuranceContractVersionDto>.Fail("Contract, version number, and valid dates are required.");
        if (!await _db.InsuranceContracts.AnyAsync(x => x.Id == dto.InsuranceContractId))
            return Result<InsuranceContractVersionDto>.Fail("Insurance contract was not found.");
        if (await _db.InsuranceContractVersions.AnyAsync(x => x.InsuranceContractId == dto.InsuranceContractId && x.VersionNumber == dto.VersionNumber && x.Id != dto.Id))
            return Result<InsuranceContractVersionDto>.Fail("This contract version already exists.");

        var version = dto.Id > 0 ? await _db.InsuranceContractVersions.FirstOrDefaultAsync(x => x.Id == dto.Id) : new InsuranceContractVersion();
        if (dto.Id > 0 && version == null)
            return Result<InsuranceContractVersionDto>.Fail("Contract version was not found.");
        version ??= new InsuranceContractVersion();
        if (dto.Id == 0) await _db.InsuranceContractVersions.AddAsync(version);
        version.InsuranceContractId = dto.InsuranceContractId;
        version.VersionNumber = dto.VersionNumber;
        version.ValidFrom = dto.ValidFrom;
        version.ValidTo = dto.ValidTo;
        version.IsActive = dto.IsActive;
        version.Notes = dto.Notes?.Trim();
        version.UpdatedDate = DateTime.Now;
        if (version.CreatedDate == default) version.CreatedDate = version.UpdatedDate;
        await _db.SaveChangesAsync();
        return Result<InsuranceContractVersionDto>.Ok(new InsuranceContractVersionDto
        {
            Id = version.Id, InsuranceContractId = version.InsuranceContractId, VersionNumber = version.VersionNumber,
            ValidFrom = version.ValidFrom, ValidTo = version.ValidTo, IsActive = version.IsActive
        });
    }

    public async Task<Result<List<InsuranceCoverageRuleDto>>> GetCoverageRulesAsync(int contractVersionId, int? planId = null)
    {
        var query = _db.InsuranceCoverageRules.AsNoTracking().Where(x => x.InsuranceContractVersionId == contractVersionId);
        if (planId.HasValue) query = query.Where(x => x.InsurancePlanId == planId.Value);
        var rules = await query.OrderByDescending(x => x.ProductId.HasValue).ThenBy(x => x.ProductId)
            .Select(x => new InsuranceCoverageRuleDto
            {
                Id = x.Id, InsuranceContractVersionId = x.InsuranceContractVersionId, InsurancePlanId = x.InsurancePlanId,
                ProductId = x.ProductId, CategoryId = x.CategoryId, ServiceCode = x.ServiceCode,
                CoveragePercent = x.CoveragePercent, CopayPercent = x.CopayPercent, DeductibleAmount = x.DeductibleAmount,
                PerClaimLimit = x.PerClaimLimit, AnnualLimit = x.AnnualLimit, RequiresAuthorization = x.RequiresAuthorization,
                IsExcluded = x.IsExcluded, DiscountPercent = x.DiscountPercent, FixedDiscountAmount = x.FixedDiscountAmount,
                ValidFrom = x.ValidFrom, ValidTo = x.ValidTo, Notes = x.Notes
            }).ToListAsync();
        return Result<List<InsuranceCoverageRuleDto>>.Ok(rules);
    }

    public async Task<Result<InsuranceCoverageRuleDto>> SaveCoverageRuleAsync(InsuranceCoverageRuleWriteDto dto)
    {
        if (dto.InsuranceContractVersionId <= 0)
            return Result<InsuranceCoverageRuleDto>.Fail("Contract version is required.");
        if (dto.ProductId is null && dto.CategoryId is null && string.IsNullOrWhiteSpace(dto.ServiceCode))
            return Result<InsuranceCoverageRuleDto>.Fail("Product, category, or service code is required.");
        if (dto.CoveragePercent < 0 || dto.CoveragePercent > 100 || dto.CopayPercent < 0 || dto.CopayPercent > 100
            || dto.DiscountPercent < 0 || dto.DiscountPercent > 100 || dto.FixedDiscountAmount < 0 || dto.DeductibleAmount < 0)
            return Result<InsuranceCoverageRuleDto>.Fail("Coverage, copay, and discount percentages must be between 0 and 100.");
        if (dto.ValidTo != default && dto.ValidFrom != default && dto.ValidTo < dto.ValidFrom)
            return Result<InsuranceCoverageRuleDto>.Fail("Coverage rule end date cannot be before its start date.");
        if (!await _db.InsuranceContractVersions.AnyAsync(x => x.Id == dto.InsuranceContractVersionId))
            return Result<InsuranceCoverageRuleDto>.Fail("Contract version was not found.");
        var rule = dto.Id > 0 ? await _db.InsuranceCoverageRules.FirstOrDefaultAsync(x => x.Id == dto.Id) : new InsuranceCoverageRule();
        if (dto.Id > 0 && rule == null)
            return Result<InsuranceCoverageRuleDto>.Fail("Coverage rule was not found.");
        rule ??= new InsuranceCoverageRule();
        if (dto.Id == 0) await _db.InsuranceCoverageRules.AddAsync(rule);
        rule.InsuranceContractVersionId = dto.InsuranceContractVersionId;
        rule.InsurancePlanId = dto.InsurancePlanId;
        rule.ProductId = dto.ProductId;
        rule.CategoryId = dto.CategoryId;
        rule.ServiceCode = dto.ServiceCode?.Trim();
        rule.CoveragePercent = dto.CoveragePercent;
        rule.CopayPercent = dto.CopayPercent;
        rule.DeductibleAmount = dto.DeductibleAmount;
        rule.PerClaimLimit = dto.PerClaimLimit;
        rule.AnnualLimit = dto.AnnualLimit;
        rule.RequiresAuthorization = dto.RequiresAuthorization;
        rule.IsExcluded = dto.IsExcluded;
        rule.DiscountPercent = dto.DiscountPercent;
        rule.FixedDiscountAmount = dto.FixedDiscountAmount;
        rule.ValidFrom = dto.ValidFrom == default ? DateTime.Today : dto.ValidFrom;
        rule.ValidTo = dto.ValidTo == default ? DateTime.Today.AddYears(1) : dto.ValidTo;
        rule.Notes = dto.Notes?.Trim();
        rule.UpdatedDate = DateTime.Now;
        if (rule.CreatedDate == default) rule.CreatedDate = rule.UpdatedDate;
        await _db.SaveChangesAsync();
        return Result<InsuranceCoverageRuleDto>.Ok(new InsuranceCoverageRuleDto
        {
            Id = rule.Id, InsuranceContractVersionId = rule.InsuranceContractVersionId, InsurancePlanId = rule.InsurancePlanId,
            ProductId = rule.ProductId, CategoryId = rule.CategoryId, ServiceCode = rule.ServiceCode,
            CoveragePercent = rule.CoveragePercent, CopayPercent = rule.CopayPercent, DeductibleAmount = rule.DeductibleAmount,
            PerClaimLimit = rule.PerClaimLimit, AnnualLimit = rule.AnnualLimit, RequiresAuthorization = rule.RequiresAuthorization,
            IsExcluded = rule.IsExcluded, DiscountPercent = rule.DiscountPercent, FixedDiscountAmount = rule.FixedDiscountAmount,
            ValidFrom = rule.ValidFrom, ValidTo = rule.ValidTo, Notes = rule.Notes
        });
    }

    public async Task<Result<List<InsuranceContractDto>>> GetContractsAsync(int? companyId = null, bool activeOnly = false)
    {
        var query = _db.InsuranceContracts.AsNoTracking().AsQueryable();
        if (companyId.HasValue)
            query = query.Where(x => x.InsuranceCompanyId == companyId.Value);
        if (activeOnly)
            query = query.Where(x => x.Status == InsuranceRecordStatus.Active);
        var contracts = await query.OrderBy(x => x.ContractNumber).Select(x => new InsuranceContractDto
        {
            Id = x.Id, ContractNumber = x.ContractNumber, InsuranceCompanyId = x.InsuranceCompanyId,
            AdministratorId = x.AdministratorId, ContractualPayerId = x.ContractualPayerId,
            Name = x.Name, ValidFrom = x.ValidFrom, ValidTo = x.ValidTo, Status = x.Status,
            PaymentDueDays = x.PaymentDueDays
        }).ToListAsync();
        return Result<List<InsuranceContractDto>>.Ok(contracts);
    }

    public async Task<Result<InsuranceContractDto>> SaveContractAsync(InsuranceContractWriteDto dto)
    {
        if (dto.InsuranceCompanyId <= 0 || string.IsNullOrWhiteSpace(dto.ContractNumber) || string.IsNullOrWhiteSpace(dto.Name))
            return Result<InsuranceContractDto>.Fail("Insurance company, contract number, and name are required.");
        if (dto.ValidTo < dto.ValidFrom)
            return Result<InsuranceContractDto>.Fail("Contract end date cannot be before its start date.");
        if (!await _db.InsuranceCompanies.AnyAsync(x => x.Id == dto.InsuranceCompanyId))
            return Result<InsuranceContractDto>.Fail("Insurance company was not found.");
        if (dto.AdministratorId.HasValue && !await _db.InsuranceCompanies.AnyAsync(x => x.Id == dto.AdministratorId.Value))
            return Result<InsuranceContractDto>.Fail("Insurance administrator was not found.");
        if (dto.ContractualPayerId.HasValue && !await _db.InsuranceCompanies.AnyAsync(x => x.Id == dto.ContractualPayerId.Value))
            return Result<InsuranceContractDto>.Fail("Contractual payer was not found.");

        var number = dto.ContractNumber.Trim();
        if (await _db.InsuranceContracts.AnyAsync(x => x.ContractNumber == number && x.Id != dto.Id))
            return Result<InsuranceContractDto>.Fail("Contract number already exists.");
        var contract = dto.Id > 0 ? await _db.InsuranceContracts.FirstOrDefaultAsync(x => x.Id == dto.Id) : new InsuranceContract();
        if (dto.Id > 0 && contract == null)
            return Result<InsuranceContractDto>.Fail("Insurance contract was not found.");
        contract ??= new InsuranceContract();
        if (dto.Id == 0)
            await _db.InsuranceContracts.AddAsync(contract);
        contract.ContractNumber = number;
        contract.InsuranceCompanyId = dto.InsuranceCompanyId;
        contract.AdministratorId = dto.AdministratorId;
        contract.ContractualPayerId = dto.ContractualPayerId;
        contract.Name = dto.Name.Trim();
        contract.ValidFrom = dto.ValidFrom;
        contract.ValidTo = dto.ValidTo;
        contract.Status = dto.Status;
        contract.SettlementTerms = dto.SettlementTerms?.Trim();
        contract.PaymentDueDays = dto.PaymentDueDays;
        contract.Notes = dto.Notes?.Trim();
        contract.UpdatedDate = DateTime.Now;
        if (contract.CreatedDate == default) contract.CreatedDate = contract.UpdatedDate;
        await _db.SaveChangesAsync();
        return Result<InsuranceContractDto>.Ok(new InsuranceContractDto
        {
            Id = contract.Id, ContractNumber = contract.ContractNumber, InsuranceCompanyId = contract.InsuranceCompanyId,
            AdministratorId = contract.AdministratorId, ContractualPayerId = contract.ContractualPayerId,
            Name = contract.Name, ValidFrom = contract.ValidFrom, ValidTo = contract.ValidTo,
            Status = contract.Status, PaymentDueDays = contract.PaymentDueDays
        });
    }

    public async Task<Result<List<InsuranceMemberPolicyDto>>> GetMemberPoliciesAsync(int? customerId = null, int? companyId = null)
    {
        var query = _db.InsuranceMemberPolicies.AsNoTracking().AsQueryable();
        if (customerId.HasValue) query = query.Where(x => x.CustomerId == customerId.Value);
        if (companyId.HasValue) query = query.Where(x => x.InsuranceCompanyId == companyId.Value);
        var policies = await query.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.MemberNumber).Select(x => new InsuranceMemberPolicyDto
        {
            Id = x.Id, CustomerId = x.CustomerId, InsuranceCompanyId = x.InsuranceCompanyId,
            InsuranceContractId = x.InsuranceContractId, InsurancePlanId = x.InsurancePlanId,
            MemberNumber = x.MemberNumber, PolicyNumber = x.PolicyNumber, ValidFrom = x.ValidFrom,
            ValidTo = x.ValidTo, EligibilityStatus = x.EligibilityStatus, Status = x.Status
        }).ToListAsync();
        return Result<List<InsuranceMemberPolicyDto>>.Ok(policies);
    }

    public async Task<Result<InsuranceMemberPolicyDto>> SaveMemberPolicyAsync(InsuranceMemberPolicyWriteDto dto)
    {
        if (dto.CustomerId <= 0 || dto.InsuranceCompanyId <= 0 || dto.InsuranceContractId <= 0 || string.IsNullOrWhiteSpace(dto.MemberNumber))
            return Result<InsuranceMemberPolicyDto>.Fail("Customer, company, contract, and member number are required.");
        if (dto.ValidTo < dto.ValidFrom)
            return Result<InsuranceMemberPolicyDto>.Fail("Membership end date cannot be before its start date.");
        if (!await _db.InsuranceContracts.AnyAsync(x => x.Id == dto.InsuranceContractId && x.InsuranceCompanyId == dto.InsuranceCompanyId))
            return Result<InsuranceMemberPolicyDto>.Fail("The selected contract does not belong to the insurance company.");
        var duplicate = await _db.InsuranceMemberPolicies.AnyAsync(x => x.InsuranceCompanyId == dto.InsuranceCompanyId && x.MemberNumber == dto.MemberNumber.Trim() && x.Id != dto.Id);
        if (duplicate)
            return Result<InsuranceMemberPolicyDto>.Fail("Member number already exists for this company.");
        var policy = dto.Id > 0 ? await _db.InsuranceMemberPolicies.FirstOrDefaultAsync(x => x.Id == dto.Id) : new InsuranceMemberPolicy();
        if (dto.Id > 0 && policy == null)
            return Result<InsuranceMemberPolicyDto>.Fail("Insurance membership was not found.");
        policy ??= new InsuranceMemberPolicy();
        if (dto.Id == 0) await _db.InsuranceMemberPolicies.AddAsync(policy);
        policy.CustomerId = dto.CustomerId; policy.InsuranceCompanyId = dto.InsuranceCompanyId;
        policy.InsuranceContractId = dto.InsuranceContractId; policy.InsurancePlanId = dto.InsurancePlanId;
        policy.BeneficiaryCategoryId = dto.BeneficiaryCategoryId; policy.MemberNumber = dto.MemberNumber.Trim();
        policy.PolicyNumber = dto.PolicyNumber?.Trim(); policy.CardNumber = dto.CardNumber?.Trim();
        policy.ValidFrom = dto.ValidFrom; policy.ValidTo = dto.ValidTo; policy.IsPrimary = dto.IsPrimary;
        policy.Status = dto.Status; policy.UpdatedDate = DateTime.Now;
        if (policy.CreatedDate == default) policy.CreatedDate = policy.UpdatedDate;
        await _db.SaveChangesAsync();
        return Result<InsuranceMemberPolicyDto>.Ok(new InsuranceMemberPolicyDto
        {
            Id = policy.Id, CustomerId = policy.CustomerId, InsuranceCompanyId = policy.InsuranceCompanyId,
            InsuranceContractId = policy.InsuranceContractId, InsurancePlanId = policy.InsurancePlanId,
            MemberNumber = policy.MemberNumber, PolicyNumber = policy.PolicyNumber, ValidFrom = policy.ValidFrom,
            ValidTo = policy.ValidTo, EligibilityStatus = policy.EligibilityStatus, Status = policy.Status
        });
    }

    public async Task<Result<InsuranceEligibilityCheck>> RecordEligibilityCheckAsync(InsuranceEligibilityCheckWriteDto dto)
    {
        if (dto.InsuranceMemberPolicyId <= 0)
            return Result<InsuranceEligibilityCheck>.Fail("Insurance membership is required.");
        if (dto.Status == InsuranceEligibilityStatus.Eligible
            && dto.Method != InsuranceVerificationMethod.Manual
            && string.IsNullOrWhiteSpace(dto.ExternalReference))
            return Result<InsuranceEligibilityCheck>.Fail("An external verification reference is required for non-manual eligibility.");

        var policy = await _db.InsuranceMemberPolicies.FirstOrDefaultAsync(x => x.Id == dto.InsuranceMemberPolicyId);
        if (policy == null)
            return Result<InsuranceEligibilityCheck>.Fail("Insurance membership was not found.");

        var checkedAt = dto.CheckedAt ?? DateTime.Now;
        var check = new InsuranceEligibilityCheck
        {
            InsuranceMemberPolicyId = dto.InsuranceMemberPolicyId,
            Status = dto.Status,
            Method = dto.Method,
            PerformedByUserId = dto.PerformedByUserId,
            CheckedAt = checkedAt,
            ValidTo = dto.ValidTo,
            ExternalReference = dto.ExternalReference?.Trim(),
            ResponseSummary = dto.ResponseSummary?.Trim(),
            Notes = dto.Notes?.Trim(),
            CreatedDate = checkedAt,
            UpdatedDate = checkedAt
        };
        await _db.InsuranceEligibilityChecks.AddAsync(check);

        policy.EligibilityStatus = dto.Status;
        policy.LastVerifiedDate = checkedAt;
        policy.LastVerificationMethod = dto.Method;
        policy.ExternalVerificationReference = dto.ExternalReference?.Trim();
        policy.UpdatedDate = checkedAt;
        await _db.SaveChangesAsync();
        return Result<InsuranceEligibilityCheck>.Ok(check);
    }

    public async Task<Result<List<InsuranceClaimSummaryDto>>> GetClaimsAsync(int? companyId = null, InsuranceClaimStatus? status = null)
    {
        var query = _db.InsuranceClaims.AsNoTracking().AsQueryable();
        if (companyId.HasValue)
            query = query.Where(x => x.InsuranceCompanyId == companyId.Value);
        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        var claims = await query
            .OrderByDescending(x => x.CreatedDate)
            .Select(x => new InsuranceClaimSummaryDto
            {
                Id = x.Id,
                ClaimNumber = x.ClaimNumber,
                InvoiceId = x.InvoiceId,
                InsuranceCompanyId = x.InsuranceCompanyId,
                InsuranceClaimBatchId = x.InsuranceClaimBatchId,
                Status = x.Status,
                SubmissionDate = x.SubmissionDate,
                SubmittedAmount = x.SubmittedAmount,
                ApprovedAmount = x.ApprovedAmount,
                RejectedAmount = x.RejectedAmount,
                DeductionAmount = x.DeductionAmount,
                PaidAmount = x.PaidAmount,
                OutstandingAmount = x.OutstandingAmount
            })
            .ToListAsync();

        return Result<List<InsuranceClaimSummaryDto>>.Ok(claims);
    }

    public async Task<Result<InsuranceClaimSummaryDto>> CreateClaimAsync(int invoiceId, int coverageId)
    {
        var coverage = await _db.InsuranceInvoiceCoverages
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == coverageId && x.InvoiceId == invoiceId);
        if (coverage == null)
            return Result<InsuranceClaimSummaryDto>.Fail("Insurance invoice coverage was not found.");

        var existing = await _db.InsuranceClaims.FirstOrDefaultAsync(x =>
            x.InvoiceId == invoiceId && x.InsuranceInvoiceCoverageId == coverage.Id);
        if (existing != null)
            return Result<InsuranceClaimSummaryDto>.Fail("A claim already exists for this invoice.");

        var claim = new InsuranceClaim
        {
            ClaimNumber = $"CLM-{DateTime.Now:yyyyMMddHHmmssfff}",
            InvoiceId = invoiceId,
            InsuranceInvoiceCoverageId = coverage.Id,
            InsuranceCompanyId = coverage.InsuranceCompanyId,
            InsuranceMemberPolicyId = coverage.InsuranceMemberPolicyId,
            Status = InsuranceClaimStatus.Prepared,
            SubmittedAmount = coverage.InsurerAmount,
            OutstandingAmount = coverage.InsurerAmount,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };

        await _db.InsuranceClaims.AddAsync(claim);
        await _db.SaveChangesAsync();
        return Result<InsuranceClaimSummaryDto>.Ok(ToSummary(claim));
    }

    public async Task<Result<InsuranceClaimSummaryDto>> ReviewClaimAsync(InsuranceClaimReviewDto dto)
    {
        var decisionPermission = dto.Status == InsuranceClaimStatus.Rejected
            ? "InsuranceClaims.Reject"
            : "InsuranceClaims.Approve";
        if (!await HasCurrentUserPermissionAsync(decisionPermission))
            return Result<InsuranceClaimSummaryDto>.Fail("You do not have permission to review insurance claims.");

        var claim = await _db.InsuranceClaims.FirstOrDefaultAsync(x => x.Id == dto.ClaimId);
        if (claim == null)
            return Result<InsuranceClaimSummaryDto>.Fail("Insurance claim was not found.");
        if (claim.Status is InsuranceClaimStatus.Paid or InsuranceClaimStatus.Closed or InsuranceClaimStatus.Cancelled)
            return Result<InsuranceClaimSummaryDto>.Fail("A settled or closed claim cannot be reviewed.");
        if (dto.Status is not (InsuranceClaimStatus.PartiallyApproved or InsuranceClaimStatus.Approved or InsuranceClaimStatus.Rejected))
            return Result<InsuranceClaimSummaryDto>.Fail("Only approval, partial approval, or rejection can be recorded.");
        if (dto.ApprovedAmount < 0 || dto.RejectedAmount < 0 || dto.DeductionAmount < 0)
            return Result<InsuranceClaimSummaryDto>.Fail("Claim decision amounts cannot be negative.");
        var decidedAmount = dto.ApprovedAmount + dto.RejectedAmount + dto.DeductionAmount;
        if (Math.Abs(decidedAmount - claim.SubmittedAmount) > 0.000001m)
            return Result<InsuranceClaimSummaryDto>.Fail("Approved, rejected, and deduction amounts must equal the submitted amount.");
        if (dto.Status == InsuranceClaimStatus.Rejected && string.IsNullOrWhiteSpace(dto.RejectionReason))
            return Result<InsuranceClaimSummaryDto>.Fail("A rejection reason is required.");
        if (dto.Status == InsuranceClaimStatus.Approved && dto.ApprovedAmount != claim.SubmittedAmount)
            return Result<InsuranceClaimSummaryDto>.Fail("An approved claim must approve the full submitted amount.");
        if (dto.Status == InsuranceClaimStatus.PartiallyApproved && (dto.ApprovedAmount <= 0 || dto.ApprovedAmount >= claim.SubmittedAmount))
            return Result<InsuranceClaimSummaryDto>.Fail("A partially approved claim must contain both approved and non-approved amounts.");
        if (dto.Status == InsuranceClaimStatus.Rejected && dto.ApprovedAmount != 0)
            return Result<InsuranceClaimSummaryDto>.Fail("A rejected claim cannot contain an approved amount.");

        claim.Status = dto.Status;
        claim.ApprovedAmount = dto.ApprovedAmount;
        claim.RejectedAmount = dto.RejectedAmount;
        claim.DeductionAmount = dto.DeductionAmount;
        claim.OutstandingAmount = Math.Max(0m, dto.ApprovedAmount - claim.PaidAmount);
        claim.DecisionDate = DateTime.Now;
        claim.RejectionReason = dto.RejectionReason?.Trim();
        claim.Notes = dto.Notes?.Trim();
        claim.UpdatedDate = DateTime.Now;
        await _db.SaveChangesAsync();
        await WriteAuditAsync("Insurance claim reviewed", decisionPermission, claim.Id, true);
        return Result<InsuranceClaimSummaryDto>.Ok(ToSummary(claim));
    }

    public async Task<Result<InsuranceClaimBatchDto>> CreateClaimBatchAsync(int companyId, DateTime periodFrom, DateTime periodTo)
    {
        if (companyId <= 0 || periodTo < periodFrom)
            return Result<InsuranceClaimBatchDto>.Fail("Insurance company and a valid period are required.");

        var claims = await _db.InsuranceClaims
            .Where(x => x.InsuranceCompanyId == companyId
                && x.CreatedDate >= periodFrom
                && x.CreatedDate <= periodTo
                && x.InsuranceClaimBatchId == null
                && (x.Status == InsuranceClaimStatus.Prepared || x.Status == InsuranceClaimStatus.ReadyForSubmission))
            .ToListAsync();
        if (claims.Count == 0)
            return Result<InsuranceClaimBatchDto>.Fail("No prepared claims were found for this period.");

        var now = DateTime.Now;
        var batch = new InsuranceClaimBatch
        {
            BatchNumber = $"B-{now:yyyyMMddHHmmssfff}",
            InsuranceCompanyId = companyId,
            PeriodFrom = periodFrom,
            PeriodTo = periodTo,
            Status = InsuranceBatchStatus.ReadyForSubmission,
            SubmittedAmount = claims.Sum(x => x.SubmittedAmount),
            ApprovedAmount = claims.Sum(x => x.ApprovedAmount),
            DeductionAmount = claims.Sum(x => x.DeductionAmount),
            CreatedDate = now,
            UpdatedDate = now
        };
        await _db.InsuranceClaimBatches.AddAsync(batch);
        await _db.SaveChangesAsync();
        foreach (var claim in claims)
        {
            claim.InsuranceClaimBatchId = batch.Id;
            claim.Status = InsuranceClaimStatus.ReadyForSubmission;
            claim.UpdatedDate = now;
        }
        await _db.SaveChangesAsync();
        return Result<InsuranceClaimBatchDto>.Ok(new InsuranceClaimBatchDto
        {
            Id = batch.Id,
            BatchNumber = batch.BatchNumber,
            InsuranceCompanyId = companyId,
            PeriodFrom = periodFrom,
            PeriodTo = periodTo,
            Status = batch.Status,
            ClaimCount = claims.Count,
            SubmittedAmount = batch.SubmittedAmount,
            ApprovedAmount = batch.ApprovedAmount,
            DeductionAmount = batch.DeductionAmount
        });
    }

    public async Task<Result<List<InsuranceReceivableAgingDto>>> GetReceivableAgingAsync(int? companyId = null, DateTime? asOf = null)
    {
        var date = asOf ?? DateTime.Now;
        var query = _db.InsuranceClaims.AsNoTracking().Where(x => x.OutstandingAmount > 0m);
        if (companyId.HasValue)
            query = query.Where(x => x.InsuranceCompanyId == companyId.Value);

        var rows = await query.Select(x => new
        {
            x.InsuranceCompanyId,
            x.CreatedDate,
            x.OutstandingAmount
        }).ToListAsync();

        var result = rows.GroupBy(x => new
        {
            x.InsuranceCompanyId,
            Bucket = (date - x.CreatedDate).TotalDays <= 30 ? "0-30"
                : (date - x.CreatedDate).TotalDays <= 60 ? "31-60"
                : (date - x.CreatedDate).TotalDays <= 90 ? "61-90"
                : (date - x.CreatedDate).TotalDays <= 120 ? "91-120" : "120+"
        }).Select(group => new InsuranceReceivableAgingDto
        {
            InsuranceCompanyId = group.Key.InsuranceCompanyId,
            Bucket = group.Key.Bucket,
            Amount = group.Sum(x => x.OutstandingAmount),
            ClaimCount = group.Count()
        }).OrderBy(x => x.InsuranceCompanyId).ThenBy(x => x.Bucket).ToList();
        return Result<List<InsuranceReceivableAgingDto>>.Ok(result);
    }

    public async Task<Result<InsuranceClaimPayment>> RecordPaymentAsync(InsuranceClaimPaymentWriteDto dto)
    {
        if (!await HasCurrentUserPermissionAsync("InsurancePayments.Create"))
            return Result<InsuranceClaimPayment>.Fail("You do not have permission to record insurance payments.");
        if (dto.Amount <= 0 || dto.Allocations.Count == 0)
            return Result<InsuranceClaimPayment>.Fail("Payment amount and allocations are required.");

        var allocationTotal = dto.Allocations.Sum(x => x.Amount);
        if (allocationTotal <= 0 || Math.Abs(allocationTotal - dto.Amount) > 0.000001m)
            return Result<InsuranceClaimPayment>.Fail("Payment allocations must equal the payment amount.");
        if (string.IsNullOrWhiteSpace(dto.ReceiptNumber))
            return Result<InsuranceClaimPayment>.Fail("A payment receipt number is required.");
        if (await _db.InsuranceClaimPayments.AnyAsync(x => x.InsuranceCompanyId == dto.InsuranceCompanyId && x.ReceiptNumber == dto.ReceiptNumber.Trim()))
            return Result<InsuranceClaimPayment>.Fail("This insurance payment receipt already exists.");

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var claimIds = dto.Allocations.Select(x => x.InsuranceClaimId).Distinct().ToList();
        var claims = await _db.InsuranceClaims
            .Where(x => claimIds.Contains(x.Id) && x.InsuranceCompanyId == dto.InsuranceCompanyId)
            .ToDictionaryAsync(x => x.Id);

        if (claims.Count != claimIds.Count)
            return Result<InsuranceClaimPayment>.Fail("One or more claims do not belong to this insurance company.");

        var payment = new InsuranceClaimPayment
        {
            InsuranceCompanyId = dto.InsuranceCompanyId,
            ReceiptNumber = dto.ReceiptNumber.Trim(),
            PaymentDate = dto.PaymentDate == default ? DateTime.Now : dto.PaymentDate,
            Amount = dto.Amount,
            FinancialTransactionId = dto.FinancialTransactionId,
            ReferenceNumber = dto.ReferenceNumber?.Trim(),
            Notes = dto.Notes?.Trim(),
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };
        await _db.InsuranceClaimPayments.AddAsync(payment);

        foreach (var allocation in dto.Allocations)
        {
            if (allocation.Amount <= 0)
                return Result<InsuranceClaimPayment>.Fail("Claim allocation must be greater than zero.");

            var claim = claims[allocation.InsuranceClaimId];
            if (claim.Status is not (InsuranceClaimStatus.Approved or InsuranceClaimStatus.PartiallyApproved or InsuranceClaimStatus.PartiallyPaid or InsuranceClaimStatus.PartiallySettled))
                return Result<InsuranceClaimPayment>.Fail($"Claim {claim.ClaimNumber} is not approved for settlement.");
            var outstanding = Math.Max(claim.ApprovedAmount - claim.PaidAmount, claim.OutstandingAmount);
            if (allocation.Amount > outstanding)
                return Result<InsuranceClaimPayment>.Fail($"Payment exceeds claim outstanding balance for claim {claim.ClaimNumber}.");

            await _db.InsuranceClaimPaymentAllocations.AddAsync(new InsuranceClaimPaymentAllocation
            {
                InsuranceClaimId = claim.Id,
                Amount = allocation.Amount,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            });
            claim.PaidAmount += allocation.Amount;
            claim.OutstandingAmount = Math.Max(0m, claim.OutstandingAmount - allocation.Amount);
            claim.Status = claim.OutstandingAmount <= 0m ? InsuranceClaimStatus.Paid : InsuranceClaimStatus.PartiallyPaid;
            claim.UpdatedDate = DateTime.Now;
        }

        await _db.SaveChangesAsync();
        foreach (var allocation in _db.InsuranceClaimPaymentAllocations.Local
                     .Where(x => x.InsuranceClaimPaymentId == 0))
            allocation.InsuranceClaimPaymentId = payment.Id;
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        await WriteAuditAsync("Insurance payment recorded", "InsurancePayments.Create", payment.Id, true);
        return Result<InsuranceClaimPayment>.Ok(payment);
    }

    private async Task<bool> HasCurrentUserPermissionAsync(string permissionKey)
    {
        if (_permissions == null || _userSession == null)
            return true;
        return await _permissions.HasCurrentUserPermissionAsync(permissionKey);
    }

    private Task WriteAuditAsync(string action, string permissionKey, int entityId, bool succeeded, string? error = null)
        => _audit?.WriteAsync(new AuditEvent(action, permissionKey, "Insurance", entityId, succeeded, error)) ?? Task.CompletedTask;

    private static InsuranceClaimSummaryDto ToSummary(InsuranceClaim claim) => new()
    {
        Id = claim.Id,
        ClaimNumber = claim.ClaimNumber,
        InvoiceId = claim.InvoiceId,
        InsuranceCompanyId = claim.InsuranceCompanyId,
        InsuranceClaimBatchId = claim.InsuranceClaimBatchId,
        Status = claim.Status,
        SubmissionDate = claim.SubmissionDate,
        SubmittedAmount = claim.SubmittedAmount,
        ApprovedAmount = claim.ApprovedAmount,
        RejectedAmount = claim.RejectedAmount,
        DeductionAmount = claim.DeductionAmount,
        PaidAmount = claim.PaidAmount,
        OutstandingAmount = claim.OutstandingAmount
    };
}
