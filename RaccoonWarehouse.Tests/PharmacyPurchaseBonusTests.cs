using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Sales;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Domain.Cashiers.DTOs;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Reflection;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RaccoonWarehouse.Application.Helper;
using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.Purchases;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Data.Repository;
using RaccoonWarehouse.Domain.Accounting.JournalEntries;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.InvoiceLines;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.StockLots;
using RaccoonWarehouse.Domain.StockTransactions;
using RaccoonWarehouse.Domain.Units;
using Xunit;

namespace RaccoonWarehouse.Tests;

public class PharmacyPurchaseBonusTests
{
    [Theory]
    [InlineData(12, "3+1", 4)]
    [InlineData(10, "2+1", 5)]
    [InlineData(10, "1+2", 20)]
    [InlineData(10, "3+1", 3)]
    [InlineData(12, "1+1", 12)]
    [InlineData(12, "5+2", 4)]
    [InlineData(12, "10+3", 3)]
    [InlineData(12, "12+1", 1)]
    [InlineData(2, "3+1", 0)]
    public void Ratios_CalculateWholeQualifyingGroups(int paid, string ratio, int free)
    {
        var line = new InvoiceLineWriteDto { Quantity = paid, BonusRatio = ratio };
        Assert.True(line.ApplyBonus()); Assert.Equal(free, line.FreeQuantity);
        Assert.Equal(paid + free, line.TotalReceivedQuantity);
        Assert.True(line.ApplyBonus()); Assert.Equal(free, line.FreeQuantity);
    }
    [Theory]
    [InlineData("0+1")][InlineData("3+0")][InlineData("-3+1")][InlineData("3+-1")]
    [InlineData("3.5+1")][InlineData("abc")][InlineData("3+1+2")][InlineData("")]
    [InlineData("99999999999999999999999999999999+1")]
    public void InvalidRatios_AreRejected(string ratio)
        => Assert.False(PurchaseBonusRatio.TryCalculate(ratio, 12m, out _));
    [Fact]
    public void QuantityChanges_Recalculate_AndRatioChangesRequireApply()
    {
        var line = new InvoiceLineWriteDto { Quantity = 12m, BonusRatio = "3+1" };
        line.ApplyBonus(); line.Quantity = 10m; Assert.Equal(3m, line.FreeQuantity);
        line.BonusRatio = "2+1"; Assert.True(line.BonusNeedsApply);
        Assert.Throws<ArgumentException>(() => PurchaseBonusCosting.Validate(new[] { line }));
        line.ApplyBonus(); Assert.False(line.BonusNeedsApply); Assert.Equal(5m, line.FreeQuantity);
    }
    [Fact]
    public void ManualFreeQuantity_AndOrdinaryPurchasesRemainSupported()
    {
        var ordinary = Line(10, 2, 0); var manual = Line(10, 2, 5);
        PurchaseBonusCosting.Allocate(new[] { ordinary }, 0m, false);
        Assert.Null(ordinary.PurchaseAcquisitionCost);
        PurchaseBonusCosting.Allocate(new[] { manual }, 0m, false);
        Assert.Equal(20m, manual.PurchaseAcquisitionCost);
        Assert.Equal(1.333333m, manual.EffectiveLotUnitCost);
    }
    [Fact]
    public void NetCost_ExcludesInputVat_AndIncludesBothDiscounts()
    {
        var line = Line(12, 2.32m, 4); line.TaxRate = 16m; line.TaxExempt = false;
        line.LineDiscountAmount = 2.32m;
        PurchaseBonusCosting.Allocate(new[] { line }, 2m, true);
        Assert.Equal(20m, line.PurchaseAcquisitionCost); Assert.Equal(1.25m, line.EffectiveLotUnitCost);
        var exclusive = Line(12, 2m, 4); exclusive.TaxRate = 16m; exclusive.TaxExempt = false;
        exclusive.LineDiscountAmount = 2m;
        PurchaseBonusCosting.Allocate(new[] { exclusive }, 2m, false);
        Assert.Equal(line.PurchaseAcquisitionCost, exclusive.PurchaseAcquisitionCost);
    }
    [Fact]
    public void HeaderDiscount_IsAllocatedExactlyAcrossMixedLines()
    {
        var a = Line(1, 1, 1);var b = Line(1, 2, 0);var c = Line(1, 4, 0);
        PurchaseBonusCosting.Allocate(new[] { a,b,c }, 1m, false);
        Assert.Equal(6m, new[]{a,b,c}.Sum(x=>x.PurchaseAcquisitionCost));
        Assert.All(new[]{a,b,c},x=>Assert.True(x.PurchaseAcquisitionCost.HasValue));
    }
    [Fact]
    public async Task IndependentReceipts_KeepSeparateLotsAndCosts_WithoutChangingProductPrices()
    {
        await using var db = Db(); Seed(db); await db.SaveChangesAsync();var stock = Stock(db);
        Assert.True((await stock.PostMovementsAsync(new[] { Receive(15m,20m,101), Receive(20m,30m,102) })).Success);
        var lots = await db.Set<StockLot>().OrderBy(x=>x.Id).ToListAsync();Assert.Equal(2,lots.Count);
        Assert.Equal(15m,lots[0].Quantity);Assert.Equal(1.333333m,lots[0].PurchasePrice);Assert.Equal(20m,lots[0].PurchaseAcquisitionCost);
        Assert.Equal(20m,lots[1].Quantity);Assert.Equal(1.5m,lots[1].PurchasePrice);Assert.Equal(30m,lots[1].PurchaseAcquisitionCost);
        var unit = await db.Set<ProductUnit>().SingleAsync(); Assert.Equal(9m,unit.PurchasePrice);Assert.Equal(12m,unit.SalePrice);
    }
    [Fact]
    public async Task SaleFromBonusLot_UsesOwnCost_EvenWithExistingAverageSetting()
    {
        await using var db=Db();Seed(db);await db.SaveChangesAsync();var stock=Stock(db);
        await stock.PostMovementsAsync(new[]{Receive(16m,24m,101),Receive(20m,60m,102)});
        var result=await stock.AllocateOutgoingAsync(new[]{new StockAllocationRequestDto{ProductId=1,ProductUnitId=1,Quantity=3m}});
        Assert.True(result.Success,result.Message);var allocation=Assert.Single(result.Data!);
        Assert.Equal(1.5m,allocation.PurchasePrice);Assert.Equal(4.5m,allocation.Quantity*allocation.PurchasePrice);
        Assert.True(allocation.HasAllocatedPurchaseCost);
    }
    [Fact]
    public async Task SaleFromZeroAcquisitionLot_DoesNotFallBackToProductPrice()
    {
        await using var db=Db();Seed(db);await db.SaveChangesAsync();var stock=Stock(db);
        await stock.PostMovementsAsync(new[]{Receive(5m,0m,101)});
        var result=await stock.AllocateOutgoingAsync(new[]{new StockAllocationRequestDto{ProductId=1,ProductUnitId=1,Quantity=1m}});
        Assert.Equal(0m,Assert.Single(result.Data!).PurchasePrice);
    }
    [Fact]
    public async Task SourceSpecificReturn_NeverConsumesAnotherReceipt()
    {
        await using var db=Db();Seed(db);await db.SaveChangesAsync();var stock=Stock(db);
        await stock.PostMovementsAsync(new[]{Receive(16m,24m,101),Receive(20m,60m,102)});
        var reverse=Receive(-3m,0m,101);reverse.PurchaseAcquisitionCost=null;reverse.TransactionType=TransactionType.Return;
        Assert.True((await stock.PostMovementsAsync(new[]{reverse})).Success);
        Assert.Equal(0m,(await db.Set<StockTransaction>().SingleAsync(x=>x.BaseQuantity<0m)).UnitPrice);
        var lots=await db.Set<StockLot>().OrderBy(x=>x.Id).ToListAsync();Assert.Equal(13m,lots[0].RemainingQuantity);Assert.Equal(20m,lots[1].RemainingQuantity);
        reverse.Quantity=-14m;reverse.BaseQuantity=-14m;
        Assert.False((await stock.PostMovementsAsync(new[]{reverse})).Success);
        Assert.Equal(13m,lots[0].RemainingQuantity);Assert.Equal(20m,lots[1].RemainingQuantity);
    }
    [Fact]
    public async Task Returns_SeparateSupplierRefundFromInventoryValue_AndRejectOverReturn()
    {
        await using var db=Db();var source=await SeedInvoice(db);
        var paid=Return(source,2m,0m);await PurchaseBonusReturns.PrepareAsync(db,paid);
        var paidLine=Assert.Single(paid.InvoiceLines!);Assert.Equal(2m,paidLine.UnitPrice);Assert.Equal(3m,paidLine.PurchaseAcquisitionCost);
        var free=Return(source,0m,2m);await PurchaseBonusReturns.PrepareAsync(db,free);
        var freeLine=Assert.Single(free.InvoiceLines!);Assert.Equal(0m,Math.Abs(freeLine.Quantity)*freeLine.UnitPrice);Assert.Equal(3m,freeLine.PurchaseAcquisitionCost);
        var tooMany=Return(source,13m,0m);await Assert.ThrowsAsync<ArgumentException>(()=>PurchaseBonusReturns.PrepareAsync(db,tooMany));
        tooMany=Return(source,0m,5m);await Assert.ThrowsAsync<ArgumentException>(()=>PurchaseBonusReturns.PrepareAsync(db,tooMany));
        paid.InvoiceLines!.Single().SourcePurchaseLineId=999;
        await Assert.ThrowsAsync<ArgumentException>(()=>PurchaseBonusReturns.PrepareAsync(db,paid));
    }
    [Fact]
    public async Task FullReturn_ReversesOriginalAcquisitionAndVat()
    {
        await using var db=Db();var source=await SeedInvoice(db);
        source.TaxExempt=false;source.TaxRate=16m;source.TaxAmount=3.84m;await db.SaveChangesAsync();
        var returned=Return(source,12m,4m);await PurchaseBonusReturns.PrepareAsync(db,returned);
        var row=Assert.Single(returned.InvoiceLines!);Assert.Equal(27.84m,Math.Abs(row.Quantity)*row.UnitPrice);Assert.Equal(24m,row.PurchaseAcquisitionCost);
    }
    [Fact]
    public async Task PreviousReturns_BlockDuplicatePaidAndFreeQuantities()
    {
        await using var db=Db();var source=await SeedInvoice(db);
        db.Add(new Invoice{InvoiceNumber="12101",InvoiceType=InvoiceType.PurchaseReturn,Status=InvoiceStatus.Completed,
            OriginalInvoiceId="12001",InvoiceLines=new List<InvoiceLine>{new(){SourcePurchaseLineId=source.Id,ProductId=1,ProductUnitId=1,Quantity=-11m,FreeQuantity=3m}}});await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ArgumentException>(()=>PurchaseBonusReturns.PrepareAsync(db,Return(source,2m,0m)));
        await Assert.ThrowsAsync<ArgumentException>(()=>PurchaseBonusReturns.PrepareAsync(db,Return(source,0m,2m)));
        await PurchaseBonusReturns.PrepareAsync(db,Return(source,1m,1m));
    }
    [Fact]
    public async Task InvoicePersistence_PreservesRatio_FreeAndAcquisitionSnapshots()
    {
        await using var db=Db();Seed(db);await db.SaveChangesAsync();var mapper=Mapper();var service=new InvoiceService(db,new UOW(db,mapper),mapper);
        var line=Line(12m,2m,4m);line.BonusRatio="3+1";
        var invoice=new InvoiceWriteDto{InvoiceNumber="12345",InvoiceType=InvoiceType.Purchase,Status=InvoiceStatus.Completed,
            PaymentType=PaymentType.Cash,CostPriceIncludesTax=false,InvoiceLines=new[]{line}};
        var result=await service.CreateAsync(invoice);Assert.True(result.Success,result.Message);
        var saved=await db.Set<InvoiceLine>().SingleAsync();Assert.Equal("3+1",saved.BonusRatio);Assert.Equal(4m,saved.FreeQuantity);Assert.Equal(24m,saved.PurchaseAcquisitionCost);
        Assert.Equal(saved.Id,line.Id);Assert.Equal(24m,result.Data!.TotalAmount);
        var read=mapper.Map<InvoiceLineReadDto>(saved);Assert.Equal("3+1",read.BonusRatio);
        var ordinary=new InvoiceLine{Quantity=2m,UnitCost=9m};Assert.Null(ordinary.BonusRatio);Assert.Null(ordinary.PurchaseAcquisitionCost);
    }
    [Fact]
    public async Task HistoricalLots_AreNotRevaluedByNewBonusReceipt()
    {
        await using var db=Db();Seed(db);var old=new StockLot{ProductId=1,ProductUnitId=1,Quantity=10,RemainingQuantity=10,BaseQuantity=10,RemainingBaseQuantity=10,QuantityPerUnitSnapshot=1,PurchasePrice=7,ExpiryDate=DateTime.Today.AddYears(1),Status=BatchStatus.Active};db.Add(old);await db.SaveChangesAsync();
        await Stock(db).PostMovementsAsync(new[]{Receive(16m,24m,101)});
        Assert.Equal(7m,old.PurchasePrice);Assert.Null(old.PurchaseAcquisitionCost);
    }
    [Theory]
    [InlineData(2,0,4,3)]
    [InlineData(0,2,0,3)]
    [InlineData(12,4,24,24)]
    public async Task PurchaseReturnJournal_BalancesRefundInventoryAndBonusReversal(int paid,int free,int refund,int inventory)
    {
        await using var db=Db();var mapper=Mapper();var accounting=new AccountingService(db,new UOW(db,mapper),mapper,new CurrencyService(db));await accounting.EnsureDefaultAccountsAsync();
        var result=await accounting.PostInvoiceEntryAsync(new InvoiceWriteDto{Id=100+paid+free,InvoiceNumber="12400",InvoiceType=InvoiceType.PurchaseReturn,
            PaymentType=PaymentType.Cash,TotalAmount=refund,NetSales=refund,TotalCOGS=inventory,
            InvoiceLines=new[]{new InvoiceLineWriteDto{Quantity=-paid,FreeQuantity=free,SourcePurchaseLineId=1,PurchaseAcquisitionCost=inventory}}});
        Assert.True(result.Success,result.Message);var journal=await db.Set<JournalEntryLine>().ToListAsync();
        Assert.Equal(journal.Sum(x=>x.Debit),journal.Sum(x=>x.Credit));
        var inventoryAccount=await db.Set<RaccoonWarehouse.Domain.Accounting.Accounts.Account>().SingleAsync(x=>x.Code=="1150000000");
        Assert.Equal(inventory,journal.Where(x=>x.AccountId==inventoryAccount.Id).Sum(x=>x.Credit));
    }
    [Fact]
    public async Task FreeOnlyReturn_PersistsWithZeroSupplierRefund_AndPositiveInventoryReversal()
    {
        await using var db=Db();Seed(db);var source=await SeedInvoice(db);
        var stock=Stock(db);await stock.PostMovementsAsync(new[]{Receive(16m,24m,source.Id)});
        var mapper=Mapper();var service=new InvoiceService(db,new UOW(db,mapper),mapper);
        var returned=Return(source,0m,2m);returned.InvoiceNumber="12456";returned.Status=InvoiceStatus.Completed;returned.PaymentType=PaymentType.Cash;
        var result=await service.CreateAsync(returned);Assert.True(result.Success,result.Message);
        Assert.Equal(0m,result.Data!.TotalAmount);Assert.Equal(3m,result.Data.TotalCOGS);
        Assert.DoesNotContain(result.Data.Payments??Array.Empty<InvoicePaymentWriteDto>(), x=>x.Amount>0m);
    }
    [Fact]
    public async Task PartialReturns_CumulativeRounding_RefundExactlyOriginalAmount()
    {
        await using var db=Db();var source=await SeedInvoice(db);
        source.Quantity=3m;source.FreeQuantity=1m;source.PurchaseAcquisitionCost=1m;source.TaxAmount=0.16m;source.TaxExempt=false;source.TaxRate=16m;
        await db.SaveChangesAsync();var mapper=Mapper();var service=new InvoiceService(db,new UOW(db,mapper),mapper);
        decimal refunds=0m,costs=0m;
        for(var i=0;i<3;i++)
        {
            var returned=Return(source,1m,i==2?1m:0m);returned.InvoiceNumber=(12500+i).ToString();returned.Status=InvoiceStatus.Completed;returned.PaymentType=PaymentType.Cash;
            var result=await service.CreateAsync(returned);Assert.True(result.Success,result.Message);
            refunds+=result.Data!.TotalAmount;costs+=result.Data.TotalCOGS;
        }
        Assert.Equal(1.16m,refunds);Assert.Equal(1m,costs);
    }
    [Fact]
    public async Task SourceUnitFactor_IsUsedForBonusCostAndSales()
    {
        await using var db=Db();Seed(db);await db.SaveChangesAsync();
        var unit=await db.Set<ProductUnit>().SingleAsync();unit.QuantityPerUnit=10m;await db.SaveChangesAsync();
        var receipt=Receive(16m,24m,101);receipt.QuantityPerUnitSnapshot=10m;receipt.BaseQuantity=160m;
        var stock=Stock(db);Assert.True((await stock.PostMovementsAsync(new[]{receipt})).Success);
        var allocation=await stock.AllocateOutgoingAsync(new[]{new StockAllocationRequestDto{ProductId=1,ProductUnitId=1,Quantity=3m}});
        Assert.Equal(1.5m,Assert.Single(allocation.Data!).PurchasePrice);
    }
    [Fact]
    public async Task ReopeningAndUpdatingUnconsumedInvoice_PreservesRatioAndNewLineIdentity()
    {
        await using var db=Db();Seed(db);await db.SaveChangesAsync();var mapper=Mapper();var service=new InvoiceService(db,new UOW(db,mapper),mapper);
        var line=Line(12,2,4);line.BonusRatio="3+1";
        var invoice=new InvoiceWriteDto{InvoiceNumber="12600",InvoiceType=InvoiceType.Purchase,Status=InvoiceStatus.Completed,PaymentType=PaymentType.Cash,InvoiceLines=new[]{line}};
        Assert.True((await service.CreateAsync(invoice)).Success);var oldId=line.Id;
        line.Quantity=9m;invoice.Payments=null;Assert.Equal(3m,line.FreeQuantity);
        var result=await service.UpdateAsync(invoice);Assert.True(result.Success,result.Message);Assert.NotEqual(oldId,line.Id);
        var saved=await db.Set<InvoiceLine>().SingleAsync();Assert.Equal("3+1",saved.BonusRatio);Assert.Equal(18m,saved.PurchaseAcquisitionCost);
    }
    [Fact]
    public void ModelSnapshot_ContainsOnlyExpectedBonusAdditions()
    {
        using var db=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer("Server=localhost;Database=MetadataOnly;Trusted_Connection=True;TrustServerCertificate=True").Options);
        var model=db.Model;
        Assert.Equal(40,model.FindEntityType(typeof(InvoiceLine))!.FindProperty(nameof(InvoiceLine.BonusRatio))!.GetMaxLength());
        Assert.True(model.FindEntityType(typeof(StockLot))!.FindProperty(nameof(StockLot.PurchaseAcquisitionCost))!.IsNullable);
        Assert.Equal(6,model.FindEntityType(typeof(StockLot))!.FindProperty(nameof(StockLot.PurchaseAcquisitionCost))!.GetScale());
        var snapshot = db.GetService<IMigrationsAssembly>().ModelSnapshot!;
        var snapshotModel = db.GetService<IModelRuntimeInitializer>().Initialize(snapshot.Model, designTime: true);
        var differences = db.GetService<IMigrationsModelDiffer>().GetDifferences(
            snapshotModel.GetRelationalModel(), db.GetService<IDesignTimeModel>().Model.GetRelationalModel());
        Assert.DoesNotContain(differences, operation =>
        {
            var name = operation.GetType().GetProperty("Name")?.GetValue(operation) as string;
            return name is "BonusRatio" or "PurchaseAcquisitionCost" or "SourcePurchaseLineId"
                || name?.EndsWith("_SourcePurchaseLineId") == true;
        });
        // Accessing metadata does not open a database connection.
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReturnWorkflow_UsesOriginalSourceAndCorrectSettlement(bool credit)
    {
        await using var db=Db();Seed(db);var source=await SeedInvoice(db);
        var original=source.Invoice!;original.TotalAmount=24m;original.PaymentType=credit?PaymentType.Credit:PaymentType.Cash;await db.SaveChangesAsync();
        var mapper=Mapper();var invoices=new InvoiceService(db,new UOW(db,mapper),mapper);var stock=Stock(db);
        Assert.True((await stock.PostMovementsAsync(new[]{Receive(16m,24m,source.Id)})).Success);
        var checkout=new BonusCheckoutProbe(async request=>
        {
            var saved=await invoices.CreateAsync(request.Invoice);
            Assert.True(saved.Success,saved.Message);
            var movement=await stock.PostMovementsAsync(request.StockMovementsFactory(saved.Data!.Id));
            Assert.True(movement.Success,movement.Message);
            return Result<SaleCheckoutResult>.Ok(new SaleCheckoutResult{SavedInvoice=saved.Data});
        });
        var permissions=DispatchProxy.Create<IPermissionService,BonusPermissionProxy>();
        var service=new PurchaseBonusReturnService(db,checkout,permissions);
        var row=Assert.Single(await service.GetRowsAsync(original.Id));row.PaidToReturn=2m;row.FreeToReturn=1m;
        var result=await service.CompleteAsync(original.Id,new[]{row},credit?PaymentType.Credit:PaymentType.Cash,new CashierSessionReadDto{Id=1,CashierId=1});
        Assert.True(result.Success,result.Message);Assert.Equal(4m,result.Data!.SavedInvoice.TotalAmount);
        Assert.Equal(4.5m,result.Data.SavedInvoice.TotalCOGS);
        Assert.Equal(13m,(await db.Set<StockLot>().SingleAsync()).RemainingQuantity);
        row=Assert.Single(await service.GetRowsAsync(original.Id));Assert.Equal(10m,row.PaidAvailable);Assert.Equal(3m,row.FreeAvailable);
    }
    public sealed class BonusCheckoutProbe : ISaleCheckoutService
    {
        private readonly Func<SaleCheckoutRequest,Task<Result<SaleCheckoutResult>>> _run;
        public BonusCheckoutProbe(Func<SaleCheckoutRequest,Task<Result<SaleCheckoutResult>>> run)=>_run=run;
        public Task<Result<SaleCheckoutResult>> CompleteAsync(SaleCheckoutRequest request)=>_run(request);
    }
    public class BonusPermissionProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? method,object?[]? args)=>Task.FromResult(true);
    }
    [Fact]
    public void TinyHeaderDiscount_DoesNotCreateNegativeDiscountShares()
    {
        var lines=Enumerable.Range(0,33).Select(_=>Line(1,1,1)).ToArray();
        PurchaseBonusCosting.Allocate(lines,0.001m,false);
        Assert.Equal(32.999m,lines.Sum(x=>x.PurchaseAcquisitionCost));
        Assert.All(lines,line=>Assert.InRange(line.PurchaseAcquisitionCost!.Value,0m,1m));
    }
    [Fact]
    public async Task Return_UsesOriginalUnitFactorAfterProductUnitChanges()
    {
        await using var db=Db();Seed(db);var source=await SeedInvoice(db);
        var unit=await db.Set<ProductUnit>().SingleAsync();unit.QuantityPerUnit=10m;await db.SaveChangesAsync();
        var returned=Return(source,2m,1m);returned.InvoiceNumber="12700";returned.Status=InvoiceStatus.Completed;returned.PaymentType=PaymentType.Cash;
        var mapper=Mapper();var service=new InvoiceService(db,new UOW(db,mapper),mapper);
        var result=await service.CreateAsync(returned);Assert.True(result.Success,result.Message);
        Assert.Equal(1m,Assert.Single(result.Data!.InvoiceLines!).QuantityPerUnitSnapshot);
    }
    [Fact]
    public async Task SalesReturnReceipt_RetainsEffectiveCostAndCreatesIndependentLot()
    {
        await using var db=Db();Seed(db);await db.SaveChangesAsync();var stock=Stock(db);
        var returned=Receive(3m,4.5m,101);returned.SourcePurchaseLineId=null;returned.TransactionType=TransactionType.Return;
        Assert.True((await stock.PostMovementsAsync(new[]{returned})).Success);
        var lot=await db.Set<StockLot>().SingleAsync();Assert.Equal(1.5m,lot.PurchasePrice);Assert.Equal(4.5m,lot.PurchaseAcquisitionCost);
    }
    [Fact]
    public async Task ReturnLookup_ListsBonusPurchases_AndKeepsFreeUnitsReturnableAfterPaidReturn()
    {
        await using var db=Db();var source=await SeedInvoice(db);var mapper=Mapper();var service=new InvoiceService(db,new UOW(db,mapper),mapper);
        db.Add(new Invoice{InvoiceNumber="12800",OriginalInvoiceId="12001",InvoiceType=InvoiceType.PurchaseReturn,Status=InvoiceStatus.Completed,
            InvoiceLines=new List<InvoiceLine>{new(){ProductId=1,ProductUnitId=1,Quantity=-12m,SourcePurchaseLineId=source.Id}}});await db.SaveChangesAsync();
        var result=await service.SearchReturnInvoiceHeadersAsync(null,null,invoiceNumber:"12001");Assert.True(result.Success,result.Message);
        Assert.False(Assert.Single(result.Data!).IsFullyReturned);
        db.Add(new Invoice{InvoiceNumber="12801",OriginalInvoiceId="12001",InvoiceType=InvoiceType.PurchaseReturn,Status=InvoiceStatus.Completed,
            InvoiceLines=new List<InvoiceLine>{new(){ProductId=1,ProductUnitId=1,FreeQuantity=4m,SourcePurchaseLineId=source.Id}}});await db.SaveChangesAsync();
        result=await service.SearchReturnInvoiceHeadersAsync(null,null,invoiceNumber:"12001");Assert.True(result.Success,result.Message);
        Assert.True(Assert.Single(result.Data!).IsFullyReturned);
    }
    [Fact]
    public async Task TaxableSale_ProfitUsesNetBonusCostWithoutRemovingVatTwice()
    {
        await using var db=Db();Seed(db);await db.SaveChangesAsync();var mapper=Mapper();var service=new InvoiceService(db,new UOW(db,mapper),mapper);
        var line=Line(3m,2.32m,0m);line.UnitCost=1.5m;line.PurchaseAcquisitionCost=4.5m;line.TaxExempt=false;line.TaxRate=16m;
        var invoice=new InvoiceWriteDto{InvoiceNumber="12900",InvoiceType=InvoiceType.Sale,Status=InvoiceStatus.Completed,PaymentType=PaymentType.Cash,InvoiceLines=new[]{line}};
        var result=await service.CreateAsync(invoice);Assert.True(result.Success,result.Message);
        Assert.Equal(4.5m,result.Data!.TotalCOGS);Assert.Equal(1.5m,line.ProfitBeforeTax);Assert.Equal(1.5m,result.Data.GrossProfit);
    }
    private static InvoiceLineWriteDto Line(decimal paid,decimal price,decimal free)
        => new(){ProductId=1,ProductUnitId=1,Quantity=paid,UnitPrice=price,UnitCost=price,FreeQuantity=free,TaxExempt=true,ExpiryDate=DateTime.Today.AddYears(1)};
    private static StockMovementPostDto Receive(decimal qty,decimal cost,int source)
        =>new(){ProductId=1,ProductUnitId=1,Quantity=qty,BaseQuantity=qty,QuantityPerUnitSnapshot=1m,PurchaseAcquisitionCost=cost,SourcePurchaseLineId=source,SalePrice=12m,ExpiryDate=DateTime.Today.AddYears(1),TransactionType=TransactionType.Purchase,UpdateCatalogAverageCost=true};
    private static InvoiceWriteDto Return(InvoiceLine source,decimal paid,decimal free)
        =>new(){InvoiceType=InvoiceType.PurchaseReturn,OriginalInvoiceId="12001",SupplierId=3,InvoiceLines=new[]{new InvoiceLineWriteDto{ProductId=1,ProductUnitId=1,Quantity=-paid,FreeQuantity=free,SourcePurchaseLineId=source.Id}}};
    private static async Task<InvoiceLine> SeedInvoice(ApplicationDbContext db)
    {
        var source=new InvoiceLine{ProductId=1,ProductUnitId=1,Quantity=12m,FreeQuantity=4m,PurchaseAcquisitionCost=24m,UnitPrice=2m,UnitCost=2m,TaxExempt=true,QuantityPerUnitSnapshot=1m,ExpiryDate=DateTime.Today.AddYears(1)};
        db.Add(new Invoice{InvoiceNumber="12001",InvoiceType=InvoiceType.Purchase,SupplierId=3,Status=InvoiceStatus.Completed,InvoiceLines=new List<InvoiceLine>{source}});await db.SaveChangesAsync();return source;
    }
    private static IMapper Mapper()=>new MapperConfiguration(c=>c.AddProfile<MappingProfile>()).CreateMapper();
    private static StockService Stock(ApplicationDbContext db)=>new(db,new UOW(db,Mapper()),Mapper(),null,new InventoryCostingSettingsService(db));
    private static void Seed(ApplicationDbContext db)
    {
        db.Add(new Product{Id=1,Name="Medicine A",ITEMCODE=123});db.Add(new Unit{Id=1,Name="Piece"});
        db.Add(new ProductUnit{Id=1,ProductId=1,UnitId=1,QuantityPerUnit=1m,PurchasePrice=9m,SalePrice=12m,IsBaseUnit=true});
    }
    private static ApplicationDbContext Db()=>new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase("bonus-"+Guid.NewGuid()).ConfigureWarnings(w=>w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
}
