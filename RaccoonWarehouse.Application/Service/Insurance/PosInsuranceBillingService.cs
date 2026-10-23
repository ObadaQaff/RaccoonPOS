using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.Accounts;
using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Accounting.JournalEntries.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Insurance;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using System.Text.Json;

namespace RaccoonWarehouse.Application.Service.Insurance;

public sealed record PreparedPosInsurance(InvoiceWriteDto Invoice, PosInsuranceBillingDto Snapshot);

public sealed class PosInsuranceBillingService
{
    private readonly ApplicationDbContext _db;
    public PosInsuranceBillingService(ApplicationDbContext db) => _db = db;
    public void DiscardTrackingAfterRollback() => _db.ChangeTracker.Clear();

    public static PosInsuranceBillingDto Calculate(decimal total, decimal customerPercent, decimal contractualPercent)
    {
        if (total < 0m || customerPercent < 0m || customerPercent > 100m
            || contractualPercent < 0m || contractualPercent > 100m || contractualPercent > customerPercent)
            throw new ArgumentException("Invalid insurance percentages or total.");
        var customerDiscount = Math.Round(total * customerPercent / 100m, 3);
        var contractualDiscount = Math.Round(total * contractualPercent / 100m, 3);
        return new PosInsuranceBillingDto
        {
            OriginalTotal = total, CustomerDiscountPercent = customerPercent,
            ContractualDiscountPercent = contractualPercent,
            CustomerDiscountAmount = customerDiscount, ContractualDiscountAmount = contractualDiscount,
            PatientAmount = total - customerDiscount, InsurerAmount = customerDiscount - contractualDiscount,
            InvoiceTotal = total - contractualDiscount
        };
    }

    // Copy the checkout input so failures/retries never apply the discount twice to the POS basket.
    private static T Copy<T>(T source) where T : new()
    {
        var result = new T();
        foreach (var property in typeof(T).GetProperties().Where(p => p.CanRead && p.CanWrite))
            property.SetValue(result, property.GetValue(source));
        return result;
    }

    public async Task<Result<PreparedPosInsurance>> PrepareAsync(InvoiceWriteDto source, PosInsuranceBillingDto details)
    {
        if (source.InvoiceType != InvoiceType.Sale || source.Status != InvoiceStatus.Completed
            || source.InvoiceLines == null || source.InvoiceLines.Count == 0)
            return Result<PreparedPosInsurance>.Fail("Insurance checkout requires a completed sale with items.");
        if (string.IsNullOrWhiteSpace(details.CustomerName) || string.IsNullOrWhiteSpace(details.MemberNumber))
            return Result<PreparedPosInsurance>.Fail("Customer name and member number are required.");
        var company = await _db.InsuranceCompanies.SingleOrDefaultAsync(x => x.Id == details.InsuranceCompanyId
            && x.Status == InsuranceRecordStatus.Active);
        if (company == null) return Result<PreparedPosInsurance>.Fail("Select an active insurance company.");
        if (company.ContractualDiscountPercent != details.ContractualDiscountPercent)
            return Result<PreparedPosInsurance>.Fail("The contractual discount changed. Reopen insurance details.");
        if (details.CustomerId.HasValue && !await _db.Set<RaccoonWarehouse.Domain.Users.User>().AnyAsync(x => x.Id == details.CustomerId.Value))
            return Result<PreparedPosInsurance>.Fail("Customer was not found.");
        var invoice = Copy(source);
        invoice.InvoiceLines = source.InvoiceLines.Select(Copy).ToList();
        if (invoice.InvoiceLines.Any(x => x.Quantity <= 0m || x.UnitPrice < 0m
            || x.LineDiscountAmount < 0m || x.LineDiscountAmount > x.Quantity * x.UnitPrice))
            return Result<PreparedPosInsurance>.Fail("Invalid insurance invoice items.");
        var lineTotal = invoice.InvoiceLines.Sum(x => x.Quantity * x.UnitPrice - x.LineDiscountAmount);
        var headerDiscount = source.DiscountAmount ?? 0m;
        if (headerDiscount < 0m || headerDiscount > lineTotal)
            return Result<PreparedPosInsurance>.Fail("Invalid invoice discount.");
        var total = Math.Round(lineTotal - headerDiscount, 3);
        if (Math.Abs(lineTotal - headerDiscount - total) > 0.000001m)
            return Result<PreparedPosInsurance>.Fail("Insurance sales require a total rounded to three decimal places.");
        PosInsuranceBillingDto snapshot;
        try { snapshot = Calculate(total, details.CustomerDiscountPercent, company.ContractualDiscountPercent); }
        catch (ArgumentException) { return Result<PreparedPosInsurance>.Fail("Contractual discount cannot exceed the customer discount (0–100%)."); }
        snapshot.InsuranceCompanyId = company.Id; snapshot.CustomerId = details.CustomerId;
        snapshot.CustomerName = details.CustomerName.Trim(); snapshot.Phone = details.Phone?.Trim();
        snapshot.MemberNumber = details.MemberNumber.Trim(); snapshot.PolicyNumber = details.PolicyNumber?.Trim();
        if (total <= 0m) return Result<PreparedPosInsurance>.Fail("The invoice total must be greater than zero.");
        var payments = (source.Payments ?? new List<InvoicePaymentWriteDto>())
            .Where(x => x.PaymentType != PaymentType.InsuranceReceivable).Select(Copy).ToList();
        if (payments.Any(x => x.Amount <= 0m || x.PaymentType == PaymentType.Credit)
            || Math.Abs(payments.Sum(x => x.Amount) - snapshot.PatientAmount) > 0.000001m)
            return Result<PreparedPosInsurance>.Fail("Customer payments must equal the patient amount using cash/card or other paid methods.");
        if (snapshot.InsurerAmount > 0m)
            payments.Add(new InvoicePaymentWriteDto { PaymentType = PaymentType.InsuranceReceivable, Amount = snapshot.InsurerAmount });
        invoice.Payments = payments;
        invoice.PaymentType = payments.FirstOrDefault()?.PaymentType ?? PaymentType.InsuranceReceivable;
        invoice.IsInsuranceSale = true; invoice.CustomerId = details.CustomerId;
        invoice.InsuranceCompanyId = company.Id;
        invoice.InsuranceMemberPolicyId = invoice.InsuranceContractId = invoice.InsuranceContractVersionId = null;
        invoice.PatientResponsibility = snapshot.PatientAmount; invoice.InsuranceResponsibility = snapshot.InsurerAmount;
        // Sales prices include tax. Distribute the contractual reduction across the actual FEFO lines
        // before InvoiceService normalizes tax, rather than subtracting tax-inclusive money from net sales.
        var remaining = headerDiscount + snapshot.ContractualDiscountAmount + (lineTotal - headerDiscount - total);
        var baseRemaining = lineTotal;
        var items = invoice.InvoiceLines.ToList();
        for (var i = 0; i < items.Count; i++)
        {
            var line = items[i]; var basis = line.Quantity * line.UnitPrice - line.LineDiscountAmount;
            var share = i == items.Count - 1 ? remaining : baseRemaining == 0m ? 0m
                : Math.Clamp(Math.Round(remaining * basis / baseRemaining, 6), 0m, basis);
            line.LineDiscountAmount += share; remaining -= share; baseRemaining -= basis;
        }
        invoice.DiscountAmount = 0m; invoice.TotalAmount = snapshot.InvoiceTotal;
        await EnsureReceivableAccountAsync(company.Id);
        return Result<PreparedPosInsurance>.Ok(new PreparedPosInsurance(invoice, snapshot));
    }

    public async Task<int> EnsureReceivableAccountAsync(int companyId)
    {
        var company = await _db.InsuranceCompanies.SingleAsync(x => x.Id == companyId);
        if (company.ReceivableAccountId.HasValue)
        {
            var account = await _db.Set<Account>().SingleOrDefaultAsync(x => x.Id == company.ReceivableAccountId.Value);
            if (account == null || !account.IsActive || !account.IsPosting || account.AccountType != AccountType.Asset
                || await _db.InsuranceCompanies.AnyAsync(x => x.Id != companyId && x.ReceivableAccountId == account.Id))
                throw new InvalidOperationException("The insurer needs an active, dedicated asset receivable account.");
            return account.Id;
        }
        var code = $"INS-{companyId:000000}";
        var existing = await _db.Set<Account>().SingleOrDefaultAsync(x => x.Code == code);
        if (existing != null) throw new InvalidOperationException("The generated insurance account code is already used.");
        var parent = await _db.Set<Account>().FirstOrDefaultAsync(x => x.Code == "1140000000" || x.Code == "1104");
        var now = DateTime.Now;
        var created = new Account { Code = code, Name = company.Name, ArabicName = company.Name,
            EnglishName = company.NameEnglish ?? company.Name, AccountType = AccountType.Asset,
            NormalBalanceType = NormalBalanceType.Debit, IsPosting = true, IsActive = true,
            IsSystemGenerated = true, AllowManualEntry = false, ParentAccountId = parent?.Id,
            Level = (parent?.Level ?? 2) + 1, CreatedDate = now, UpdatedDate = now };
        _db.Set<Account>().Add(created); await _db.SaveChangesAsync();
        company.ReceivableAccountId = created.Id; company.UpdatedDate = now;
        await _db.SaveChangesAsync(); return created.Id;
    }

    public async Task SaveCoverageAndClaimAsync(InvoiceWriteDto saved, PosInsuranceBillingDto snapshot)
    {
        if (await _db.InsuranceInvoiceCoverages.AnyAsync(x => x.InvoiceId == saved.Id))
            throw new InvalidOperationException("This invoice already has insurance coverage.");
        if (Math.Abs(saved.TotalAmount - snapshot.InvoiceTotal) > 0.000001m)
            throw new InvalidOperationException("The saved insurance total differs from the calculation.");
        var now = DateTime.Now;
        var coverage = new InsuranceInvoiceCoverage { InvoiceId = saved.Id,
            InsuranceCompanyId = snapshot.InsuranceCompanyId, GrossAmount = snapshot.OriginalTotal,
            DiscountAmount = snapshot.ContractualDiscountAmount, PatientAmount = snapshot.PatientAmount,
            InsurerAmount = snapshot.InsurerAmount, SnapshotJson = JsonSerializer.Serialize(snapshot),
            CreatedDate = now, UpdatedDate = now };
        _db.InsuranceInvoiceCoverages.Add(coverage); await _db.SaveChangesAsync();
        var items = (saved.InvoiceLines ?? new List<InvoiceLineWriteDto>()).ToList();
        var patientRemaining = snapshot.PatientAmount; var insurerRemaining = snapshot.InsurerAmount;
        var totalRemaining = snapshot.InvoiceTotal;
        var discountRemaining = snapshot.ContractualDiscountAmount;
        var weightRemaining = snapshot.InvoiceTotal > 0m ? snapshot.InvoiceTotal : items.Sum(x => x.Quantity * x.UnitPrice);
        foreach (var line in items)
        {
            var gross = line.Quantity * line.UnitPrice - line.LineDiscountAmount;
            var last = line == items.Last();
            var patient = last ? patientRemaining : totalRemaining == 0 ? 0
                : Math.Clamp(Math.Round(patientRemaining * gross / totalRemaining, 6),
                    Math.Max(0m, gross - insurerRemaining), Math.Min(gross, patientRemaining));
            var weight = snapshot.InvoiceTotal > 0m ? gross : line.Quantity * line.UnitPrice;
            var contractual = last ? discountRemaining : weightRemaining == 0m ? 0m
                : Math.Clamp(Math.Round(discountRemaining * weight / weightRemaining, 6), 0m, discountRemaining);
            discountRemaining -= contractual; weightRemaining -= weight;
            var insurer = last ? insurerRemaining : gross - patient;
            patientRemaining -= patient; insurerRemaining -= insurer; totalRemaining -= gross;
            _db.InsuranceInvoiceCoverageLines.Add(new InsuranceInvoiceCoverageLine {
                InsuranceInvoiceCoverageId = coverage.Id, InvoiceLineId = line.Id, ProductId = line.ProductId,
                Quantity = line.Quantity, ApprovedQuantity = line.Quantity, GrossAmount = gross + contractual,
                InsuranceDiscountPercent = snapshot.ContractualDiscountPercent, InsuranceDiscountAmount = contractual,
                ContractedUnitPrice = line.Quantity == 0 ? 0 : gross / line.Quantity,
                PatientAmount = patient, CoveredAmount = insurer,
                CreatedDate = now, UpdatedDate = now });
        }
        if (snapshot.InsurerAmount > 0m)
        {
            var claim = new InsuranceClaim { ClaimNumber = $"CLM-{saved.Id}-{Guid.NewGuid():N}",
                InvoiceId = saved.Id, InsuranceInvoiceCoverageId = coverage.Id,
                InsuranceCompanyId = snapshot.InsuranceCompanyId, Status = InsuranceClaimStatus.Prepared,
                SubmittedAmount = snapshot.InsurerAmount, OutstandingAmount = snapshot.InsurerAmount,
                CreatedDate = now, UpdatedDate = now };
            _db.InsuranceClaims.Add(claim); await _db.SaveChangesAsync();
            foreach (var line in _db.InsuranceInvoiceCoverageLines.Local.Where(x => x.InsuranceInvoiceCoverageId == coverage.Id))
                _db.InsuranceClaimLines.Add(new InsuranceClaimLine { InsuranceClaimId = claim.Id,
                    InvoiceLineId = line.InvoiceLineId, SubmittedAmount = line.CoveredAmount,
                    CreatedDate = now, UpdatedDate = now });
        }
        await _db.SaveChangesAsync();
    }

    public async Task PostCompanyMovementAsync(IAccountingService accounting, int companyId, decimal amount,
        string referenceType, int referenceId, string referenceNumber, DateTime date, bool receipt)
    {
        if (amount == 0m) return;
        var company = await _db.InsuranceCompanies.SingleAsync(x => x.Id == companyId);
        var receivable = await EnsureReceivableAccountAsync(companyId);
        var otherId = receipt ? company.PaymentAccountId : company.DeductionAccountId;
        if (!otherId.HasValue)
        {
            var key = receipt ? AccountingService.BankAccountCodeKey : AccountingService.SalesDiscountAccountCodeKey;
            var setting = await _db.AppSettings.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key);
            var code = setting?.Value ?? (receipt ? "1130000000" : "4130000000");
            otherId = await _db.Set<Account>().Where(x => x.Code == code).Select(x => (int?)x.Id).SingleOrDefaultAsync();
        }
        var other = await _db.Set<Account>().SingleOrDefaultAsync(x => x.Id == otherId);
        if (other == null || !other.IsActive || !other.IsPosting || other.Id == receivable)
            throw new InvalidOperationException("Configure the insurer payment/deduction ledger account first.");
        var positive = amount > 0m; var value = Math.Abs(amount);
        var result = await accounting.PostJournalEntryAsync(new JournalEntryWriteDto {
            EntryNumber = $"INS-{Guid.NewGuid():N}",
            EntryDate = date, Description = $"{referenceType} {referenceNumber} / {company.Name}",
            ReferenceType = referenceType, ReferenceId = referenceId,
            Lines = new List<JournalEntryLineWriteDto> {
                new() { AccountId = other.Id, Debit = positive ? value : 0m, Credit = positive ? 0m : value },
                new() { AccountId = receivable, Credit = positive ? value : 0m, Debit = positive ? 0m : value }
            }
        });
        if (!result.Success) throw new InvalidOperationException(result.Message);
    }
}
