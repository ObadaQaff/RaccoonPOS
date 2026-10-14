using RaccoonWarehouse.Core.Common;

namespace RaccoonWarehouse.Application.Service.Insurance;

public sealed record InsuranceEligibilityProviderRequest(
    int CompanyId,
    int MemberPolicyId,
    string MemberNumber,
    CancellationToken CancellationToken = default);

public sealed record InsuranceEligibilityProviderResult(
    bool IsEligible,
    string? ExternalReference,
    DateTime? ValidTo,
    string? Summary);

public sealed record InsuranceClaimSubmissionProviderRequest(
    int CompanyId,
    int? AdministratorId,
    int? PayerId,
    int ClaimBatchId,
    string BatchNumber,
    CancellationToken CancellationToken = default);

public sealed record InsuranceClaimSubmissionProviderResult(
    bool Accepted,
    string? ExternalReference,
    string? Summary);

public interface IInsuranceProvider
{
    string ProviderKey { get; }
    Task<Result<InsuranceEligibilityProviderResult>> CheckEligibilityAsync(InsuranceEligibilityProviderRequest request);
    Task<Result<InsuranceClaimSubmissionProviderResult>> SubmitClaimBatchAsync(InsuranceClaimSubmissionProviderRequest request);
}

/// <summary>
/// Safe default for manual/portal workflows. It never fabricates eligibility or submission success.
/// </summary>
public sealed class ManualInsuranceProvider : IInsuranceProvider
{
    public string ProviderKey => "manual";

    public Task<Result<InsuranceEligibilityProviderResult>> CheckEligibilityAsync(InsuranceEligibilityProviderRequest request) =>
        Task.FromResult(Result<InsuranceEligibilityProviderResult>.Fail(
            "Manual verification requires an operator result; no external verification was performed."));

    public Task<Result<InsuranceClaimSubmissionProviderResult>> SubmitClaimBatchAsync(InsuranceClaimSubmissionProviderRequest request) =>
        Task.FromResult(Result<InsuranceClaimSubmissionProviderResult>.Fail(
            "Manual/portal submission must be recorded by an operator; no external submission was performed."));
}
