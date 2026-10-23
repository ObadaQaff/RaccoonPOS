using System.Linq.Expressions;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Accounting.JournalEntries.DTOs;
using System.Reflection;
using RaccoonWarehouse.Application.Service.Sales;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Core.Audit;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Domain.FinancialTransactions.DTOs;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RaccoonWarehouse.Application.Helper;
using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Application.Service.Insurance;
using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Data.Repository;
using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Accounting.JournalEntries;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Insurance;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Reports.Accounting.Filters;
using Xunit;

namespace RaccoonWarehouse.Tests;

public sealed class PosInsuranceBillingTests
{
    private static ApplicationDbContext Db()=>new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase("pos-insurance-"+Guid.NewGuid())
        .ConfigureWarnings(w=>w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
    private static AccountingService Accounting(ApplicationDbContext db)
    {
        var mapper=new MapperConfiguration(c=>c.AddProfile<MappingProfile>()).CreateMapper();
        return new AccountingService(db,new UOW(db,mapper),mapper,new CurrencyService(db));
    }
    private static async Task<InsuranceCompany> Company(ApplicationDbContext db)
    {
        var c=new InsuranceCompany { Code="TEST",Name="Insurer",ContractualDiscountPercent=10m,Status=InsuranceRecordStatus.Active };
        db.Add(c);await db.SaveChangesAsync();return c;
    }
    private static InvoiceWriteDto Invoice(decimal cash=80m)=>new() { InvoiceNumber="54321",InvoiceType=InvoiceType.Sale,
        Status=InvoiceStatus.Completed,CreatedDate=DateTime.Now,TotalAmount=100m,PaymentType=PaymentType.Cash,
        InvoiceLines=new List<InvoiceLineWriteDto> { new() { ProductId=1,Quantity=1m,UnitPrice=100m,TaxExempt=true } },
        Payments=cash>0m?new List<InvoicePaymentWriteDto> { new() { PaymentType=PaymentType.Cash,Amount=cash } }:new() };
    private static PosInsuranceBillingDto Details(int id,decimal percent=20m)=>new() {
        InsuranceCompanyId=id,CustomerName="Patient",MemberNumber="MEM-1",CustomerDiscountPercent=percent,ContractualDiscountPercent=10m };

    [Fact] public void ApprovedExampleReconciles()
    {
        var c=PosInsuranceBillingService.Calculate(100m,20m,10m);
        Assert.Equal(80m,c.PatientAmount);Assert.Equal(10m,c.ContractualDiscountAmount);
        Assert.Equal(10m,c.InsurerAmount);Assert.Equal(90m,c.InvoiceTotal);
    }
    [Fact] public void PercentagesUseSameOriginalTotal()
    {
        var c=PosInsuranceBillingService.Calculate(200m,35m,15m);
        Assert.Equal(130m,c.PatientAmount);Assert.Equal(30m,c.ContractualDiscountAmount);Assert.Equal(40m,c.InsurerAmount);
    }
    [Fact] public void InvalidPercentagesAreRejected()
    {
        foreach(var pair in new[]{(-1m,0m),(101m,0m),(20m,-1m),(20m,101m),(10m,20m)})
            Assert.Throws<ArgumentException>(()=>PosInsuranceBillingService.Calculate(100m,pair.Item1,pair.Item2));
    }
    [Fact] public void RoundingPreservesEquation()
    {
        for(var i=1;i<1000;i++)
        {
            var c=PosInsuranceBillingService.Calculate(i/1000m,33.333333m,12.345678m);
            Assert.Equal(c.InvoiceTotal,c.PatientAmount+c.InsurerAmount);
            Assert.Equal(c.OriginalTotal,c.PatientAmount+c.InsurerAmount+c.ContractualDiscountAmount);
            Assert.True(c.InsurerAmount>=0m);
        }
    }
    [Fact] public async Task PrepareDoesNotMutateBasketAndRetryDoesNotDoubleDiscount()
    {
        await using var db=Db();var company=await Company(db);var svc=new PosInsuranceBillingService(db);var original=Invoice();
        var a=await svc.PrepareAsync(original,Details(company.Id));var b=await svc.PrepareAsync(original,Details(company.Id));
        Assert.True(a.Success,a.Message);Assert.True(b.Success,b.Message);Assert.Equal(100m,original.TotalAmount);
        Assert.Equal(0m,original.InvoiceLines!.Single().LineDiscountAmount);
        Assert.Equal(10m,a.Data!.Invoice.InvoiceLines!.Single().LineDiscountAmount);
        Assert.Equal(90m,b.Data!.Invoice.TotalAmount);Assert.Equal(1,await db.Set<RaccoonWarehouse.Domain.Accounting.Accounts.Account>().CountAsync());
    }
    [Fact] public async Task SplitPaymentsIncludeOnlyPatientMoneyPlusInsuranceReceivable()
    {
        await using var db=Db();var c=await Company(db);var invoice=Invoice();
        invoice.Payments=new List<InvoicePaymentWriteDto> {new(){PaymentType=PaymentType.Cash,Amount=30m},new(){PaymentType=PaymentType.Visa,Amount=50m}};
        var result=await new PosInsuranceBillingService(db).PrepareAsync(invoice,Details(c.Id));
        Assert.True(result.Success,result.Message);Assert.Equal(90m,result.Data!.Invoice.Payments!.Sum(x=>x.Amount));
        Assert.Equal(10m,result.Data.Invoice.Payments!.Single(x=>x.PaymentType==PaymentType.InsuranceReceivable).Amount);
    }
    [Fact] public async Task WrongPatientPaymentIsRejected()
    {
        await using var db=Db();var c=await Company(db);
        Assert.False((await new PosInsuranceBillingService(db).PrepareAsync(Invoice(100m),Details(c.Id))).Success);
    }
    [Fact] public async Task InactiveCompanyAndChangedDiscountAreRejected()
    {
        await using var db=Db();var c=await Company(db);var svc=new PosInsuranceBillingService(db);
        c.Status=InsuranceRecordStatus.Inactive;await db.SaveChangesAsync();Assert.False((await svc.PrepareAsync(Invoice(),Details(c.Id))).Success);
        c.Status=InsuranceRecordStatus.Active;c.ContractualDiscountPercent=11m;await db.SaveChangesAsync();
        Assert.False((await svc.PrepareAsync(Invoice(),Details(c.Id))).Success);
    }
    [Fact] public async Task MissingCustomerDetailsAreRejected()
    {
        await using var db=Db();var c=await Company(db);var d=Details(c.Id);d.CustomerName=" ";
        Assert.False((await new PosInsuranceBillingService(db).PrepareAsync(Invoice(),d)).Success);
        d.CustomerName="Patient";d.MemberNumber="";Assert.False((await new PosInsuranceBillingService(db).PrepareAsync(Invoice(),d)).Success);
    }
    [Fact] public async Task ZeroPatientAndZeroInsurerCasesReconcile()
    {
        await using var db=Db();var c=await Company(db);var svc=new PosInsuranceBillingService(db);
        var a=await svc.PrepareAsync(Invoice(0m),Details(c.Id,100m));Assert.True(a.Success,a.Message);
        Assert.Equal(90m,a.Data!.Invoice.InsuranceResponsibility);Assert.Equal(0m,a.Data.Invoice.PatientResponsibility);
        var b=await svc.PrepareAsync(Invoice(90m),Details(c.Id,10m));Assert.True(b.Success,b.Message);
        Assert.DoesNotContain(b.Data!.Invoice.Payments!,x=>x.PaymentType==PaymentType.InsuranceReceivable);
    }
    [Fact] public async Task InvoiceServiceRecalculatesTaxAndSavesClaimSnapshot()
    {
        await using var db=Db();var c=await Company(db);var svc=new PosInsuranceBillingService(db);var source=Invoice();
        source.InvoiceLines!.Single().TaxExempt=false;source.InvoiceLines.Single().TaxRate=16m;
        var prepared=(await svc.PrepareAsync(source,Details(c.Id))).Data!;
        var mapper=new MapperConfiguration(cfg=>cfg.AddProfile<MappingProfile>()).CreateMapper();
        var result=await new InvoiceService(db,new UOW(db,mapper),mapper).CreateAsync(prepared.Invoice);
        Assert.True(result.Success,result.Message);Assert.Equal(90m,result.Data!.TotalAmount);
        Assert.Equal(12.414m,result.Data.TotalTax);Assert.Equal(77.586m,result.Data.SubTotal);
        await svc.SaveCoverageAndClaimAsync(result.Data,prepared.Snapshot);
        var claim=await db.InsuranceClaims.SingleAsync();Assert.Equal(10m,claim.SubmittedAmount);Assert.Equal(10m,claim.OutstandingAmount);
        Assert.Null(claim.InsuranceMemberPolicyId);Assert.Contains("MEM-1",(await db.InsuranceInvoiceCoverages.SingleAsync()).SnapshotJson);
        var coverageLine=await db.InsuranceInvoiceCoverageLines.SingleAsync();
        Assert.Equal(10m,coverageLine.CoveredAmount);Assert.Equal(100m,coverageLine.GrossAmount);
        Assert.Equal(10m,coverageLine.InsuranceDiscountAmount);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>svc.SaveCoverageAndClaimAsync(result.Data,prepared.Snapshot));
    }
    [Fact] public async Task ExistingDiscountsAndMultipleFefoLinesArePreserved()
    {
        await using var db=Db();var c=await Company(db);var source=Invoice(72m);source.DiscountAmount=5m;
        source.InvoiceLines=new List<InvoiceLineWriteDto> {new(){ProductId=1,Quantity=1,UnitPrice=50,LineDiscountAmount=5,TaxExempt=true},
            new(){ProductId=1,Quantity=1,UnitPrice=50,TaxExempt=true}};
        var result=await new PosInsuranceBillingService(db).PrepareAsync(source,Details(c.Id));
        Assert.True(result.Success,result.Message);Assert.Equal(81m,result.Data!.Invoice.TotalAmount);
        Assert.Equal(19m,result.Data.Invoice.InvoiceLines!.Sum(x=>x.LineDiscountAmount));
        Assert.Equal(0m,result.Data.Invoice.DiscountAmount);Assert.Equal(5m,source.DiscountAmount);
    }
    [Fact] public async Task AccountingPostsInsurerToDedicatedAccountAndStatementBalances()
    {
        await using var db=Db();var c=await Company(db);var accounting=Accounting(db);await accounting.EnsureDefaultAccountsAsync();
        var prepared=(await new PosInsuranceBillingService(db).PrepareAsync(Invoice(),Details(c.Id))).Data!;
        prepared.Invoice.Id=101;prepared.Invoice.SubTotal=90m;prepared.Invoice.TotalTax=0m;
        var posted=await accounting.PostInvoiceEntryAsync(prepared.Invoice);Assert.True(posted.Success,posted.Message);
        var id=(await db.InsuranceCompanies.SingleAsync()).ReceivableAccountId!.Value;
        var lines=await db.Set<JournalEntryLine>().ToListAsync();
        Assert.Equal(10m,lines.Where(x=>x.AccountId==id).Sum(x=>x.Debit-x.Credit));
        var ledger=await accounting.GetGeneralLedgerAsync(new GeneralLedgerFilterDto {AccountId=id,From=DateTime.Today.AddDays(-1),To=DateTime.Today.AddDays(1),IncludePostedOnly=true});
        Assert.True(ledger.Success,ledger.Message);Assert.Equal(10m,ledger.Data.Single().ClosingBalance);
    }
    [Fact] public async Task ClaimDecisionAndReceiptUpdateSameCompanyLedger()
    {
        await using var db=Db();var c=await Company(db);var accounting=Accounting(db);await accounting.EnsureDefaultAccountsAsync();
        var svc=new PosInsuranceBillingService(db);var prepared=(await svc.PrepareAsync(Invoice(),Details(c.Id))).Data!;
        prepared.Invoice.Id=102;prepared.Invoice.SubTotal=90m;
        Assert.True((await accounting.PostInvoiceEntryAsync(prepared.Invoice)).Success);
        await svc.SaveCoverageAndClaimAsync(prepared.Invoice,prepared.Snapshot);
        var claim=await db.InsuranceClaims.SingleAsync();var manager=new InsuranceManagementService(db,accounting:accounting);
        var decision=await manager.ReviewClaimAsync(new(){ClaimId=claim.Id,Status=InsuranceClaimStatus.PartiallyApproved,ApprovedAmount=8m,DeductionAmount=2m});
        Assert.True(decision.Success,decision.Message);
        var receipt=await manager.RecordPaymentAsync(new(){InsuranceCompanyId=c.Id,ReceiptNumber="REC-1",Amount=3m,
            Allocations=new(){new(){InsuranceClaimId=claim.Id,Amount=3m}}});
        Assert.True(receipt.Success,receipt.Message);Assert.Equal(5m,claim.OutstandingAmount);
        Assert.Equal(receipt.Data!.Id,(await db.InsuranceClaimPaymentAllocations.SingleAsync()).InsuranceClaimPaymentId);
        var balance=await db.Set<JournalEntryLine>().Where(x=>x.AccountId==c.ReceivableAccountId).SumAsync(x=>x.Debit-x.Credit);
        Assert.Equal(5m,balance);
        Assert.False((await manager.RecordPaymentAsync(new(){InsuranceCompanyId=c.Id,ReceiptNumber="REC-2",Amount=6m,
            Allocations=new(){new(){InsuranceClaimId=claim.Id,Amount=6m}}})).Success);
    }
    [Fact] public async Task ClaimReviewCannotReduceApprovedBelowAlreadyPaid()
    {
        await using var db=Db();var c=await Company(db);var claim=new InsuranceClaim {ClaimNumber="TEST",InsuranceCompanyId=c.Id,
            Status=InsuranceClaimStatus.PartiallyPaid,SubmittedAmount=10m,ApprovedAmount=10m,PaidAmount=8m,OutstandingAmount=2m};
        db.Add(claim);await db.SaveChangesAsync();
        var result=await new InsuranceManagementService(db).ReviewClaimAsync(new(){ClaimId=claim.Id,Status=InsuranceClaimStatus.PartiallyApproved,ApprovedAmount=5m,RejectedAmount=5m});
        Assert.False(result.Success);Assert.Equal(2m,claim.OutstandingAmount);
    }
    [Fact] public async Task TinyFefoAmountsNeverCreateNegativeCoverageShares()
    {
        await using var db=Db();var c=await Company(db);var svc=new PosInsuranceBillingService(db);var source=Invoice(0.002m);
        source.InvoiceLines=Enumerable.Range(1,5).Select(i=>new InvoiceLineWriteDto {Id=i,ProductId=1,Quantity=1m,UnitPrice=0.0006m,TaxExempt=true}).ToList();
        var result=await svc.PrepareAsync(source,Details(c.Id,33.333333m));Assert.True(result.Success,result.Message);
        result.Data!.Invoice.Id=301;await svc.SaveCoverageAndClaimAsync(result.Data.Invoice,result.Data.Snapshot);
        var lines=await db.InsuranceInvoiceCoverageLines.ToListAsync();
        Assert.All(lines,x=>Assert.True(x.PatientAmount>=0m&&x.CoveredAmount>=0m));
        Assert.Equal(result.Data.Snapshot.PatientAmount,lines.Sum(x=>x.PatientAmount));
        Assert.Equal(result.Data.Snapshot.InsurerAmount,lines.Sum(x=>x.CoveredAmount));
    }

    public class InterfaceStub : DispatchProxy
    {
        public Func<MethodInfo,object?[]?,object?> Handler { get; set; }=(_,_)=>throw new NotSupportedException();
        protected override object? Invoke(MethodInfo? method,object?[]? args)=>Handler(method!,args);
    }
    private static T Stub<T>(Func<MethodInfo,object?[]?,object?> handler) where T:class
    { var value=DispatchProxy.Create<T,InterfaceStub>();((InterfaceStub)(object)value).Handler=handler;return value; }
    private sealed class TransactionProbe:IUnitOfWorkTransaction
    {
        public bool Committed;public bool RolledBack;
        public Task CommitAsync(){Committed=true;return Task.CompletedTask;}
        public Task RollbackAsync(){RolledBack=true;return Task.CompletedTask;}
        public ValueTask DisposeAsync()=>ValueTask.CompletedTask;
    }
    private static SaleCheckoutService Checkout(ApplicationDbContext db,TransactionProbe tx,List<FinancialPostDto> receipts,
        bool stockFailure=false,bool wrongSavedTotal=false,bool postingFailure=false,
        IAccountingService? accounting=null,List<Invoice>? existingInvoices=null,Action<string>? trace=null)
    {
        var invoices=Stub<IInvoiceService>((method,args)=>
        {
            if(method.Name=="GetAllWriteDtoWithFilteringAndIncludeAsync")
            {
                var predicate=((Expression<Func<Invoice,bool>>)args![0]!).Compile();
                var matches=(existingInvoices??new List<Invoice>()).Where(predicate)
                    .Select(x=>new InvoiceWriteDto {Id=x.Id,InvoiceNumber=x.InvoiceNumber}).ToList();
                return Task.FromResult(Result<List<InvoiceWriteDto>>.Ok(matches));
            }
            if(method.Name is not ("CreateAsync" or "UpdateAsync"))throw new NotSupportedException(method.Name);
            var invoice=(InvoiceWriteDto)args![0]!;trace?.Invoke(method.Name+":"+invoice.Id);invoice.Id=201;
            invoice.SubTotal=invoice.TotalAmount;
            foreach(var line in invoice.InvoiceLines!)line.Id=1;
            if(wrongSavedTotal)invoice.TotalAmount+=1m;
            return Task.FromResult(Result<InvoiceWriteDto>.Ok(invoice));
        });
        var stock=Stub<IStockService>((_,_)=> {
            trace?.Invoke("Stock");return Task.FromResult(stockFailure?Result.Fail("Stock failed"):Result.Ok()); });
        var queue=Stub<IAccountingOperationService>((method,args)=>
        {
            trace?.Invoke(method.Name);
            if(method.Name=="EnqueueInvoiceAsync")Assert.False(((InvoiceWriteDto)args![0]!).IsInsuranceSale);
            if(method.Name=="EnqueueFinancialAsync")receipts.Add((FinancialPostDto)args![0]!);
            return Task.CompletedTask;
        });
        accounting??=Stub<IAccountingService>((method,args)=> {
            Assert.Equal("PostInvoiceEntryAsync",method.Name);trace?.Invoke("PostInvoice");
            var invoice=(InvoiceWriteDto)args![0]!;Assert.True(invoice.IsInsuranceSale);Assert.Equal(201,invoice.Id);
            return Task.FromResult(postingFailure?Result<JournalEntryReadDto>.Fail("Missing ledger account")
                :Result<JournalEntryReadDto>.Ok(new JournalEntryReadDto()));
        });
        return new SaleCheckoutService(invoices,stock,Stub<IFinancialTransactionService>((_,_)=>throw new NotSupportedException()),queue,
            Stub<IUOW>((method,_)=> {
                if(method.Name=="BeginTransactionAsync")return Task.FromResult<IUnitOfWorkTransaction>(tx);
                if(method.Name=="CommitAsync"){trace?.Invoke("FlushQueue");return Task.FromResult(1);}
                throw new NotSupportedException(method.Name);
            }),Stub<IAuditLogService>((_,_)=>Task.CompletedTask),insuranceBilling:new PosInsuranceBillingService(db),
            immediateAccounting:accounting);
    }
    [Fact] public async Task CheckoutCommitsClaimAndQueuesOnlyPatientReceipt()
    {
        await using var db=Db();var c=await Company(db);var tx=new TransactionProbe();var receipts=new List<FinancialPostDto>();
        var result=await Checkout(db,tx,receipts).CompleteAsync(new SaleCheckoutRequest {Invoice=Invoice(),InsuranceDetails=Details(c.Id)});
        Assert.True(result.Success,result.Message);Assert.True(tx.Committed);Assert.False(tx.RolledBack);
        Assert.Equal(80m,Assert.Single(receipts).Amount);Assert.Equal(10m,(await db.InsuranceClaims.SingleAsync()).OutstandingAmount);
    }
    [Fact] public async Task CheckoutStockFailureRequestsRollbackAndQueuesNoReceipts()
    {
        await using var db=Db();var c=await Company(db);var tx=new TransactionProbe();var receipts=new List<FinancialPostDto>();var original=Invoice();
        var result=await Checkout(db,tx,receipts,stockFailure:true).CompleteAsync(new SaleCheckoutRequest {Invoice=original,InsuranceDetails=Details(c.Id)});
        Assert.False(result.Success);Assert.True(tx.RolledBack);Assert.False(tx.Committed);Assert.Empty(receipts);
        Assert.Equal(0m,original.InvoiceLines!.Single().LineDiscountAmount);
    }
    [Fact] public async Task CheckoutCoverageFailureRequestsRollbackAndQueuesNoReceipts()
    {
        await using var db=Db();var c=await Company(db);var tx=new TransactionProbe();var receipts=new List<FinancialPostDto>();
        await Assert.ThrowsAsync<InvalidOperationException>(()=>Checkout(db,tx,receipts,wrongSavedTotal:true)
            .CompleteAsync(new SaleCheckoutRequest {Invoice=Invoice(),InsuranceDetails=Details(c.Id)}));
        Assert.True(tx.RolledBack);Assert.False(tx.Committed);Assert.Empty(receipts);Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact] public async Task InsuranceCheckoutPostsCompanyLedgerBeforeReturningSuccess()
    {
        await using var db=Db();var c=await Company(db);var accounting=Accounting(db);await accounting.EnsureDefaultAccountsAsync();
        var tx=new TransactionProbe();var receipts=new List<FinancialPostDto>();var trace=new List<string>();
        var invoice=Invoice();invoice.DeferAccountingPosting=true;
        var result=await Checkout(db,tx,receipts,accounting:accounting,trace:trace.Add)
            .CompleteAsync(new SaleCheckoutRequest {Invoice=invoice,InsuranceDetails=Details(c.Id)});
        Assert.True(result.Success,result.Message);Assert.True(tx.Committed);
        Assert.DoesNotContain("EnqueueInvoiceAsync",trace);Assert.Contains("FlushQueue",trace);
        Assert.Equal(10m,await db.Set<JournalEntryLine>().Where(x=>x.AccountId==c.ReceivableAccountId).SumAsync(x=>x.Debit-x.Credit));
    }
    [Fact] public async Task InsurancePostingFailureRollsBackBeforeStockOrReceipts()
    {
        await using var db=Db();var c=await Company(db);var tx=new TransactionProbe();var receipts=new List<FinancialPostDto>();var trace=new List<string>();
        var result=await Checkout(db,tx,receipts,postingFailure:true,trace:trace.Add)
            .CompleteAsync(new SaleCheckoutRequest {Invoice=Invoice(),InsuranceDetails=Details(c.Id)});
        Assert.False(result.Success);Assert.Contains("Missing ledger account",result.Message);
        Assert.True(tx.RolledBack);Assert.False(tx.Committed);Assert.Empty(receipts);
        Assert.DoesNotContain("Stock",trace);Assert.Empty(db.ChangeTracker.Entries());
    }
    [Fact] public async Task InsurancePostingExceptionRequestsRollback()
    {
        await using var db=Db();var c=await Company(db);var tx=new TransactionProbe();var receipts=new List<FinancialPostDto>();
        var accounting=Stub<IAccountingService>((_,_)=>throw new InvalidOperationException("Posting failed"));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>Checkout(db,tx,receipts,accounting:accounting)
            .CompleteAsync(new SaleCheckoutRequest {Invoice=Invoice(),InsuranceDetails=Details(c.Id)}));
        Assert.True(tx.RolledBack);Assert.False(tx.Committed);Assert.Empty(receipts);
    }
    [Fact] public async Task MissingInvoiceIdIsNotMistakenForAnotherExistingInvoice()
    {
        await using var db=Db();var tx=new TransactionProbe();var trace=new List<string>();var invoice=Invoice(100m);invoice.Id=999;
        var result=await Checkout(db,tx,new(),existingInvoices:new(){new(){Id=44,InvoiceNumber="11111"}},trace:trace.Add)
            .CompleteAsync(new SaleCheckoutRequest {Invoice=invoice});
        Assert.True(result.Success,result.Message);Assert.Contains("CreateAsync:0",trace);
        Assert.DoesNotContain("UpdateAsync:999",trace);
    }
    [Fact] public async Task DraftLookupRejectsDifferentNumberCashierAndSession()
    {
        await using var db=Db();var tx=new TransactionProbe();var trace=new List<string>();var invoice=Invoice(100m);
        invoice.IsPOS=true;invoice.CasherId=7;invoice.CashierSessionId=9;
        var others=new List<Invoice> {
            new(){Id=41,InvoiceNumber="11111",IsPOS=true,CasherId=7,CashierSessionId=9,Status=InvoiceStatus.Draft},
            new(){Id=42,InvoiceNumber="54321",IsPOS=true,CasherId=8,CashierSessionId=9,Status=InvoiceStatus.Draft},
            new(){Id=43,InvoiceNumber="54321",IsPOS=true,CasherId=7,CashierSessionId=10,Status=InvoiceStatus.OnHold},
            new(){Id=44,InvoiceNumber="54321",IsPOS=true,CasherId=7,CashierSessionId=9,Status=InvoiceStatus.Completed} };
        var result=await Checkout(db,tx,new(),existingInvoices:others,trace:trace.Add).CompleteAsync(new SaleCheckoutRequest{Invoice=invoice});
        Assert.True(result.Success,result.Message);Assert.Contains("CreateAsync:0",trace);
    }
    [Fact] public async Task MatchingHeldInvoiceIsReusedAndOrdinaryPostingRemainsDeferred()
    {
        await using var db=Db();var tx=new TransactionProbe();var trace=new List<string>();var invoice=Invoice(100m);
        invoice.IsPOS=true;invoice.CasherId=7;invoice.CashierSessionId=9;invoice.DeferAccountingPosting=true;
        var existing=new List<Invoice>{new(){Id=45,InvoiceNumber="54321",IsPOS=true,CasherId=7,CashierSessionId=9,Status=InvoiceStatus.OnHold}};
        var result=await Checkout(db,tx,new(),existingInvoices:existing,trace:trace.Add).CompleteAsync(new SaleCheckoutRequest{Invoice=invoice});
        Assert.True(result.Success,result.Message);Assert.Contains("UpdateAsync:45",trace);
        Assert.Contains("EnqueueInvoiceAsync",trace);Assert.DoesNotContain("PostInvoice",trace);
    }

}
