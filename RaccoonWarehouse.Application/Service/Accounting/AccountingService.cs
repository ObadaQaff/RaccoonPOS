using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Sales;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.Accounts;
using RaccoonWarehouse.Domain.Accounting.Accounts.DTOs;
using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Accounting.JournalEntries;
using RaccoonWarehouse.Domain.Accounting.JournalEntries.DTOs;
using RaccoonWarehouse.Domain.Accounting.TaxRates;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.FinancialTransactions.DTOs;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Reports.Accounting.Dtos;
using RaccoonWarehouse.Domain.Reports.Accounting.Filters;
using RaccoonWarehouse.Domain.Settings;
using RaccoonWarehouse.Domain.StockAdjustments.DTOs;
using RaccoonWarehouse.Domain.StockDocuments.DTOs;
using RaccoonWarehouse.Domain.Vouchers.DTOs;
using System.Diagnostics;

namespace RaccoonWarehouse.Application.Service.Accounting
{
    public class AccountingService : IAccountingService
    {
        public const string PostingLockDateKey = "AccountingPostingLockDate";
        public const string CashMainAccountCodeKey = "Accounting.AccountCode.CashMain";
        public const string BankAccountCodeKey = "Accounting.AccountCode.Bank";
        public const string AccountsReceivableAccountCodeKey = "Accounting.AccountCode.AccountsReceivable";
        public const string InputTaxAccountCodeKey = "Accounting.AccountCode.InputTax";
        public const string InventoryAccountCodeKey = "Accounting.AccountCode.Inventory";
        public const string ChecksInHandAccountCodeKey = "Accounting.AccountCode.ChecksInHand";
        public const string IssuedChecksPayableAccountCodeKey = "Accounting.AccountCode.IssuedChecksPayable";
        public const string OtherReceivablesAccountCodeKey = "Accounting.AccountCode.OtherReceivables";
        public const string AccountsPayableAccountCodeKey = "Accounting.AccountCode.AccountsPayable";
        public const string OutputTaxAccountCodeKey = "Accounting.AccountCode.OutputTax";
        public const string OtherPayablesAccountCodeKey = "Accounting.AccountCode.OtherPayables";
        public const string SalesRevenueAccountCodeKey = "Accounting.AccountCode.SalesRevenue";
        public const string SalesReturnsAccountCodeKey = "Accounting.AccountCode.SalesReturns";
        public const string SalesDiscountAccountCodeKey = "Accounting.AccountCode.SalesDiscount";
        public const string PurchaseDiscountAccountCodeKey = "Accounting.AccountCode.PurchaseDiscount";
        public const string StockGainAccountCodeKey = "Accounting.AccountCode.StockGain";
        public const string CostOfGoodsSoldAccountCodeKey = "Accounting.AccountCode.Cogs";
        public const string GeneralExpenseAccountCodeKey = "Accounting.AccountCode.GeneralExpense";
        public const string StockLossAccountCodeKey = "Accounting.AccountCode.StockLoss";
        public const string PosCashAccountCodeKey = "Accounting.AccountCode.PosCash";
        public const string InternalConsumptionAccountCodeKey = "Accounting.AccountCode.InternalConsumption";

        private readonly ApplicationDbContext _context;
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        private readonly CurrencyService _currencyService;

        public AccountingService(ApplicationDbContext context, IUOW uow, IMapper mapper, CurrencyService currencyService)
        {
            _context = context;
            _uow = uow;
            _mapper = mapper;
            _currencyService = currencyService;
        }

        public async Task<Result<AccountWriteDto>> CreateAccountAsync(AccountWriteDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Name))
            {
                return Result<AccountWriteDto>.Fail("Account code and name are required.");
            }

            var codeExists = await _uow.Accounts.GetAllAsQueryable()
                .AnyAsync(x => x.Code == dto.Code && x.Id != dto.Id);
            if (codeExists)
            {
                return Result<AccountWriteDto>.Fail("Account code already exists.");
            }

            if (dto.ParentAccountId.HasValue)
            {
                var parent = await _uow.Accounts.GetByIdAsync(dto.ParentAccountId.Value);
                if (parent == null)
                {
                    return Result<AccountWriteDto>.Fail("Parent account was not found.");
                }
            }

            var account = _mapper.Map<Account>(dto);
            var now = GetJordanNow();
            account.CreatedDate = now;
            account.UpdatedDate = now;

            await _uow.Accounts.AddAsync(account);
            await _uow.CommitAsync();

            return Result<AccountWriteDto>.Ok(_mapper.Map<AccountWriteDto>(account), "Account created successfully.");
        }

        public async Task<Result<List<AccountReadDto>>> GetAccountsAsync(bool activeOnly = true)
        {
            var query = _uow.Accounts.GetAllAsQueryable()
                .Include(x => x.ParentAccount)
                .AsNoTracking();

            if (activeOnly)
            {
                query = query.Where(x => x.IsActive);
            }

            var accounts = await query.OrderBy(x => x.Code).ToListAsync();
            return Result<List<AccountReadDto>>.Ok(_mapper.Map<List<AccountReadDto>>(accounts));
        }

        public async Task<Result<JournalEntryReadDto>> PostJournalEntryAsync(JournalEntryWriteDto dto)
        {
            var totalTiming = Stopwatch.StartNew();
            var stepTiming = Stopwatch.StartNew();
            if (dto.Lines == null || dto.Lines.Count < 2)
            {
                return Result<JournalEntryReadDto>.Fail("Journal entry must contain at least two lines.");
            }

            var effectiveLines = dto.Lines
                .Where(x => x.Debit > 0 || x.Credit > 0)
                .ToList();

            if (effectiveLines.Count < 2)
            {
                return Result<JournalEntryReadDto>.Fail("Journal entry must contain at least two non-zero lines.");
            }

            if (effectiveLines.Any(x => x.Debit > 0 && x.Credit > 0))
            {
                return Result<JournalEntryReadDto>.Fail("A journal line cannot contain both debit and credit values.");
            }

            var totalDebit = effectiveLines.Sum(x => x.Debit);
            var totalCredit = effectiveLines.Sum(x => x.Credit);
            if (totalDebit <= 0 || totalCredit <= 0 || totalDebit != totalCredit)
            {
                return Result<JournalEntryReadDto>.Fail("Journal entry is not balanced.");
            }

            var accountIds = effectiveLines.Select(x => x.AccountId).Distinct().ToList();
            var accounts = await _uow.Accounts.GetAllAsQueryable()
                .Where(x => accountIds.Contains(x.Id))
                .ToListAsync();

            if (accounts.Count != accountIds.Count)
            {
                return Result<JournalEntryReadDto>.Fail("One or more accounts do not exist.");
            }

            if (accounts.Any(x => !x.IsPosting || !x.IsActive))
            {
                return Result<JournalEntryReadDto>.Fail("Journal lines must use active posting accounts only.");
            }

            var now = GetJordanNow();
            var entryDate = dto.EntryDate == default ? now : dto.EntryDate;
            var postingLockDate = await GetPostingLockDateAsync();
            if (postingLockDate.HasValue && entryDate.Date <= postingLockDate.Value.Date)
            {
                return Result<JournalEntryReadDto>.Fail($"Posting is locked through {postingLockDate:yyyy-MM-dd}.");
            }

            var taxRateIds = effectiveLines
                .Where(x => x.TaxRateId.HasValue)
                .Select(x => x.TaxRateId!.Value)
                .Distinct()
                .ToList();

            var taxRates = taxRateIds.Count == 0
                ? new Dictionary<int, TaxRate>()
                : await _context.TaxRates
                    .Where(x => taxRateIds.Contains(x.Id) && x.IsActive)
                    .ToDictionaryAsync(x => x.Id);

            if (taxRateIds.Count > 0 && taxRates.Count != taxRateIds.Count)
            {
                return Result<JournalEntryReadDto>.Fail("One or more tax rates were not found or inactive.");
            }

            LogAccountingTiming("journal validation and account loading", totalTiming, stepTiming);

            var entry = new JournalEntry
            {
                EntryNumber = string.IsNullOrWhiteSpace(dto.EntryNumber) ? GenerateEntryNumber(now) : dto.EntryNumber,
                EntryDate = entryDate,
                Description = AccountingTextLocalizer.ToArabic(dto.Description),
                Status = JournalEntryStatus.Posted,
                ReferenceType = dto.ReferenceType,
                ReferenceId = dto.ReferenceId,
                CreatedDate = now,
                UpdatedDate = now,
                Lines = new List<JournalEntryLine>()
            };

            foreach (var line in effectiveLines)
            {
                decimal debit = line.Debit;
                decimal credit = line.Credit;
                decimal? fxRate = null;
                decimal? foreignAmount = null;

                if (line.CurrencyId.HasValue)
                {
                    fxRate = line.ExchangeRate.HasValue && line.ExchangeRate.Value > 0
                        ? line.ExchangeRate.Value
                        : await _currencyService.GetRateAsync(line.CurrencyId.Value, entryDate);

                    foreignAmount = line.ForeignAmount ?? (line.Debit != 0m ? line.Debit : line.Credit);
                    var convertedAmount = foreignAmount.Value * fxRate.Value;
                    debit = line.Debit != 0m ? convertedAmount : 0m;
                    credit = line.Credit != 0m ? convertedAmount : 0m;
                }

                entry.Lines.Add(new JournalEntryLine
                {
                    AccountId = line.AccountId,
                    PartyUserId = line.PartyUserId,
                    CustomerId = line.CustomerId,
                    SupplierId = line.SupplierId,
                    Debit = debit,
                    Credit = credit,
                    CostCenterId = line.CostCenterId,
                    TaxRateId = line.TaxRateId,
                    TaxAmount = line.TaxAmount,
                    CurrencyId = line.CurrencyId,
                    ForeignAmount = foreignAmount,
                    ExchangeRate = fxRate,
                    Description = AccountingTextLocalizer.ToArabic(line.Description),
                    CreatedDate = now,
                    UpdatedDate = now
                });

                if (line.TaxRateId.HasValue)
                {
                    var taxRate = taxRates[line.TaxRateId.Value];
                    var taxAmount = line.TaxAmount ?? 0m;
                    if (taxAmount < 0)
                    {
                        return Result<JournalEntryReadDto>.Fail("Tax amount cannot be negative.");
                    }

                    if (taxAmount > 0)
                    {
                        entry.Lines.Add(new JournalEntryLine
                        {
                            AccountId = taxRate.TaxAccountId,
                            Debit = debit > 0 ? taxAmount : 0m,
                            Credit = credit > 0 ? taxAmount : 0m,
                            CostCenterId = line.CostCenterId,
                            TaxRateId = line.TaxRateId,
                            TaxAmount = taxAmount,
                            CurrencyId = line.CurrencyId,
                            ForeignAmount = line.ForeignAmount,
                            ExchangeRate = line.ExchangeRate,
                            Description = $"Tax - {line.Description}",
                            CreatedDate = now,
                            UpdatedDate = now
                        });
                    }
                }
            }

            LogAccountingTiming("journal line construction", totalTiming, stepTiming);

            await _uow.JournalEntries.AddAsync(entry);
            await _uow.CommitAsync();
            LogAccountingTiming("journal database save", totalTiming, stepTiming);

            var savedEntry = await _uow.JournalEntries.GetAllAsQueryable()
                .Include(x => x.Lines)
                .ThenInclude(x => x.Account)
                .AsNoTracking()
                .FirstAsync(x => x.Id == entry.Id);
            LogAccountingTiming("journal reload", totalTiming, stepTiming);

            var result = _mapper.Map<JournalEntryReadDto>(savedEntry);
            result.TotalDebit = savedEntry.Lines.Sum(x => x.Debit);
            result.TotalCredit = savedEntry.Lines.Sum(x => x.Credit);
            return Result<JournalEntryReadDto>.Ok(result, "Journal entry posted successfully.");
        }

        public async Task<DateTime?> GetPostingLockDateAsync()
        {
            var value = await _context.AppSettings
                .AsNoTracking()
                .Where(x => x.Key == PostingLockDateKey)
                .Select(x => x.Value)
                .FirstOrDefaultAsync();

            return DateTime.TryParse(value, out var parsed) ? parsed.Date : null;
        }

        public async Task<Result<DateTime?>> SetPostingLockDateAsync(DateTime? lockDate)
        {
            var setting = await _context.AppSettings.FirstOrDefaultAsync(x => x.Key == PostingLockDateKey);
            var now = GetJordanNow();

            if (setting == null)
            {
                setting = new AppSetting
                {
                    Key = PostingLockDateKey,
                    Description = "Prevents posting accounting entries on or before this date.",
                    CreatedDate = now,
                    UpdatedDate = now
                };
                _context.AppSettings.Add(setting);
            }

            setting.Value = lockDate?.Date.ToString("yyyy-MM-dd");
            setting.UpdatedDate = now;
            await _context.SaveChangesAsync();

            return Result<DateTime?>.Ok(lockDate?.Date, lockDate.HasValue
                ? $"Posting lock date updated to {lockDate:yyyy-MM-dd}."
                : "Posting lock date cleared.");
        }

        public async Task EnsureDefaultAccountsAsync()
        {
            await EnsureStandardDefaultAccountsAsync();
            await CleanupLegacyAccountChartAsync();
            return;

            var now = GetJordanNow();
            var defaults = new[]
            {
                new { Code = "1", ParentCode = (string?)null, Name = "الأصول", EnglishName = "Assets", Description = "الحساب الرئيسي للأصول", AccountType = AccountType.Asset, NormalBalanceType = NormalBalanceType.Debit, Level = 1, IsPosting = false, AllowManualEntry = false },
                new { Code = "11", ParentCode = (string?)"1", Name = "الأصول المتداولة", EnglishName = "Current Assets", Description = "الأصول المتداولة", AccountType = AccountType.Asset, NormalBalanceType = NormalBalanceType.Debit, Level = 2, IsPosting = false, AllowManualEntry = false },
                new { Code = "1101", ParentCode = (string?)"11", Name = "الصندوق الرئيسي", EnglishName = "Main Cash", Description = "الصندوق الرئيسي للمنشأة", AccountType = AccountType.Asset, NormalBalanceType = NormalBalanceType.Debit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "1102", ParentCode = (string?)"11", Name = "صندوق نقطة البيع", EnglishName = "POS Cash", Description = "صندوق نقطة البيع", AccountType = AccountType.Asset, NormalBalanceType = NormalBalanceType.Debit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "1103", ParentCode = (string?)"11", Name = "البنك", EnglishName = "Bank", Description = "الحسابات البنكية", AccountType = AccountType.Asset, NormalBalanceType = NormalBalanceType.Debit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "1104", ParentCode = (string?)"11", Name = "الذمم المدينة - الزبائن", EnglishName = "Accounts Receivable - Customers", Description = "ذمم الزبائن", AccountType = AccountType.Asset, NormalBalanceType = NormalBalanceType.Debit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "1105", ParentCode = (string?)"11", Name = "المخزون", EnglishName = "Inventory", Description = "قيمة المخزون", AccountType = AccountType.Asset, NormalBalanceType = NormalBalanceType.Debit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "1106", ParentCode = (string?)"11", Name = "ضريبة المدخلات", EnglishName = "Input Tax", Description = "ضريبة مدخلات المشتريات", AccountType = AccountType.Asset, NormalBalanceType = NormalBalanceType.Debit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "1107", ParentCode = (string?)"11", Name = "ذمم مدينة أخرى", EnglishName = "Other Receivables", Description = "ذمم مدينة أخرى", AccountType = AccountType.Asset, NormalBalanceType = NormalBalanceType.Debit, Level = 3, IsPosting = true, AllowManualEntry = true },

                new { Code = "2", ParentCode = (string?)null, Name = "الالتزامات", EnglishName = "Liabilities", Description = "الحساب الرئيسي للالتزامات", AccountType = AccountType.Liability, NormalBalanceType = NormalBalanceType.Credit, Level = 1, IsPosting = false, AllowManualEntry = false },
                new { Code = "21", ParentCode = (string?)"2", Name = "الالتزامات المتداولة", EnglishName = "Current Liabilities", Description = "الالتزامات المتداولة", AccountType = AccountType.Liability, NormalBalanceType = NormalBalanceType.Credit, Level = 2, IsPosting = false, AllowManualEntry = false },
                new { Code = "2101", ParentCode = (string?)"21", Name = "الذمم الدائنة - الموردين", EnglishName = "Accounts Payable - Suppliers", Description = "ذمم الموردين", AccountType = AccountType.Liability, NormalBalanceType = NormalBalanceType.Credit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "2102", ParentCode = (string?)"21", Name = "ضريبة مستحقة", EnglishName = "Output Tax", Description = "ضريبة مستحقة على المبيعات", AccountType = AccountType.Liability, NormalBalanceType = NormalBalanceType.Credit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "2103", ParentCode = (string?)"21", Name = "ذمم دائنة أخرى", EnglishName = "Other Payables", Description = "ذمم دائنة أخرى", AccountType = AccountType.Liability, NormalBalanceType = NormalBalanceType.Credit, Level = 3, IsPosting = true, AllowManualEntry = true },

                new { Code = "3", ParentCode = (string?)null, Name = "حقوق الملكية", EnglishName = "Equity", Description = "الحساب الرئيسي لحقوق الملكية", AccountType = AccountType.Equity, NormalBalanceType = NormalBalanceType.Credit, Level = 1, IsPosting = false, AllowManualEntry = false },
                new { Code = "31", ParentCode = (string?)"3", Name = "حقوق الملكية", EnglishName = "Owner Equity", Description = "مجموعة حقوق الملكية", AccountType = AccountType.Equity, NormalBalanceType = NormalBalanceType.Credit, Level = 2, IsPosting = false, AllowManualEntry = false },
                new { Code = "3101", ParentCode = (string?)"31", Name = "رأس المال", EnglishName = "Capital", Description = "رأس مال المنشأة", AccountType = AccountType.Equity, NormalBalanceType = NormalBalanceType.Credit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "3102", ParentCode = (string?)"31", Name = "الأرباح المحتجزة", EnglishName = "Retained Earnings", Description = "الأرباح المرحلة", AccountType = AccountType.Equity, NormalBalanceType = NormalBalanceType.Credit, Level = 3, IsPosting = true, AllowManualEntry = true },

                new { Code = "4", ParentCode = (string?)null, Name = "الإيرادات", EnglishName = "Revenue", Description = "الحساب الرئيسي للإيرادات", AccountType = AccountType.Revenue, NormalBalanceType = NormalBalanceType.Credit, Level = 1, IsPosting = false, AllowManualEntry = false },
                new { Code = "41", ParentCode = (string?)"4", Name = "إيرادات التشغيل", EnglishName = "Operating Revenue", Description = "إيرادات النشاط", AccountType = AccountType.Revenue, NormalBalanceType = NormalBalanceType.Credit, Level = 2, IsPosting = false, AllowManualEntry = false },
                new { Code = "4101", ParentCode = (string?)"41", Name = "المبيعات", EnglishName = "Sales", Description = "إيراد المبيعات", AccountType = AccountType.Revenue, NormalBalanceType = NormalBalanceType.Credit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "4102", ParentCode = (string?)"41", Name = "مردودات المبيعات", EnglishName = "Sales Returns", Description = "مردودات المبيعات", AccountType = AccountType.Revenue, NormalBalanceType = NormalBalanceType.Debit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "4103", ParentCode = (string?)"41", Name = "خصومات المبيعات", EnglishName = "Sales Discounts", Description = "خصومات المبيعات", AccountType = AccountType.Revenue, NormalBalanceType = NormalBalanceType.Debit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "4104", ParentCode = (string?)"41", Name = "أرباح تسويات المخزون", EnglishName = "Inventory Adjustment Gains", Description = "أرباح تسويات المخزون", AccountType = AccountType.Revenue, NormalBalanceType = NormalBalanceType.Credit, Level = 3, IsPosting = true, AllowManualEntry = true },

                new { Code = "5", ParentCode = (string?)null, Name = "تكلفة المبيعات", EnglishName = "Cost of Goods Sold", Description = "الحساب الرئيسي لتكلفة المبيعات", AccountType = AccountType.Expense, NormalBalanceType = NormalBalanceType.Debit, Level = 1, IsPosting = false, AllowManualEntry = false },
                new { Code = "51", ParentCode = (string?)"5", Name = "تكلفة المبيعات", EnglishName = "Cost of Sales", Description = "مجموعة تكلفة المبيعات", AccountType = AccountType.Expense, NormalBalanceType = NormalBalanceType.Debit, Level = 2, IsPosting = false, AllowManualEntry = false },
                new { Code = "5101", ParentCode = (string?)"51", Name = "تكلفة البضاعة المباعة", EnglishName = "Cost of Goods Sold", Description = "تكلفة البضاعة المباعة", AccountType = AccountType.Expense, NormalBalanceType = NormalBalanceType.Debit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "5102", ParentCode = (string?)"51", Name = "خسائر التالف", EnglishName = "Damaged Stock Loss", Description = "خسائر التالف والمخزون الهالك", AccountType = AccountType.Expense, NormalBalanceType = NormalBalanceType.Debit, Level = 3, IsPosting = true, AllowManualEntry = true },

                new { Code = "6", ParentCode = (string?)null, Name = "المصروفات", EnglishName = "Expenses", Description = "الحساب الرئيسي للمصروفات", AccountType = AccountType.Expense, NormalBalanceType = NormalBalanceType.Debit, Level = 1, IsPosting = false, AllowManualEntry = false },
                new { Code = "61", ParentCode = (string?)"6", Name = "المصروفات التشغيلية", EnglishName = "Operating Expenses", Description = "مجموعة المصروفات التشغيلية", AccountType = AccountType.Expense, NormalBalanceType = NormalBalanceType.Debit, Level = 2, IsPosting = false, AllowManualEntry = false },
                new { Code = "6101", ParentCode = (string?)"61", Name = "المصروفات العامة", EnglishName = "General Expenses", Description = "المصروفات العامة", AccountType = AccountType.Expense, NormalBalanceType = NormalBalanceType.Debit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "6102", ParentCode = (string?)"61", Name = "استهلاك داخلي", EnglishName = "Internal Consumption", Description = "استهلاك داخلي للمخزون", AccountType = AccountType.Expense, NormalBalanceType = NormalBalanceType.Debit, Level = 3, IsPosting = true, AllowManualEntry = true }
            };

            var accountsByCode = await _uow.Accounts.GetAllAsQueryable()
                .ToDictionaryAsync(x => x.Code);

            foreach (var item in defaults)
            {
                if (!accountsByCode.TryGetValue(item.Code, out var account))
                {
                    account = new Account
                    {
                        Code = item.Code,
                        CreatedDate = now
                    };

                    await _uow.Accounts.AddAsync(account);
                    accountsByCode[item.Code] = account;
                }

                account.Name = item.Name;
                account.ArabicName = item.Name;
                account.EnglishName = item.EnglishName;
                account.Description = item.Description;
                account.AccountType = item.AccountType;
                account.NormalBalanceType = item.NormalBalanceType;
                account.IsPosting = item.IsPosting;
                account.IsActive = true;
                account.IsSystemGenerated = true;
                account.AllowManualEntry = item.AllowManualEntry;
                account.Level = item.Level;
                account.ParentAccount = item.ParentCode is null ? null : accountsByCode[item.ParentCode];
                account.UpdatedDate = now;
            }

            await _uow.CommitAsync();
            await EnsureDefaultAccountSettingsAsync(now);
        }

        public async Task<Result<JournalEntryReadDto>> PostInvoiceEntryAsync(InvoiceWriteDto invoice)
        {
            var totalTiming = Stopwatch.StartNew();
            var stepTiming = Stopwatch.StartNew();
            if (invoice.Id <= 0)
                return Result<JournalEntryReadDto>.Fail("Invoice id is required.");

            if (invoice.Status is InvoiceStatus.OnHold
                or InvoiceStatus.Draft
                or InvoiceStatus.Cancelled
                or InvoiceStatus.Unknown
                or InvoiceStatus.InProcess)
                return Result<JournalEntryReadDto>.Ok(new JournalEntryReadDto(), "Invoice is not in a postable status.");

            if (await TryGetExistingEntryAsync("Invoice", invoice.Id) is { } existing)
                return Result<JournalEntryReadDto>.Ok(existing, "Journal entry already exists for this invoice.");

            var lines = new List<JournalEntryLineWriteDto>();
            var entryDate = invoice.CreatedDate == default ? GetJordanNow() : invoice.CreatedDate;
            var accountIds = await ResolveInvoiceAccountIdsAsync(invoice);
            var settlementAccountId = accountIds[GetSettlementAccountCodeKey(invoice.PaymentType, invoice.InvoiceType is InvoiceType.Purchase or InvoiceType.PurchaseReturn)];
            var salesRevenueId = accountIds[SalesRevenueAccountCodeKey];
            var salesReturnsId = accountIds[SalesReturnsAccountCodeKey];
            var salesDiscountId = accountIds[SalesDiscountAccountCodeKey];
            var purchaseDiscountId = accountIds[PurchaseDiscountAccountCodeKey];
            var inventoryId = accountIds[InventoryAccountCodeKey];
            var cogsId = accountIds[CostOfGoodsSoldAccountCodeKey];
            var outputTaxId = accountIds[OutputTaxAccountCodeKey];
            var inputTaxId = accountIds[InputTaxAccountCodeKey];
            LogAccountingTiming("invoice accounting account resolution", totalTiming, stepTiming);

            switch (invoice.InvoiceType)
            {
                case InvoiceType.Sale:
                    AddDebit(lines, settlementAccountId, invoice.TotalAmount, $"Invoice #{invoice.InvoiceNumber} collection",
                        customerId: invoice.PaymentType == PaymentType.Credit ? invoice.CustomerId : null);
                    if ((invoice.DiscountAmount ?? 0m) > 0)
                        AddDebit(lines, salesDiscountId, invoice.DiscountAmount!.Value, $"Invoice #{invoice.InvoiceNumber} discount");
                    AddCredit(lines, salesRevenueId, invoice.SubTotal, $"Invoice #{invoice.InvoiceNumber} sales");
                    if (invoice.TotalTax > 0)
                        AddCredit(lines, outputTaxId, invoice.TotalTax, $"Invoice #{invoice.InvoiceNumber} tax");
                    if (invoice.TotalCOGS > 0)
                    {
                        AddDebit(lines, cogsId, invoice.TotalCOGS, $"Invoice #{invoice.InvoiceNumber} cost of goods sold");
                        AddCredit(lines, inventoryId, invoice.TotalCOGS, $"Invoice #{invoice.InvoiceNumber} inventory release");
                    }
                    break;

                case InvoiceType.Return:
                    var mixedLines = invoice.InvoiceLines?.ToList() ?? new List<InvoiceLineWriteDto>();
                    var saleLines = mixedLines.Where(line => line.Quantity > 0).ToList();
                    var returnLines = mixedLines.Where(line => line.Quantity < 0).ToList();

                    var saleSubtotal = saleLines.Sum(line => line.LineSubTotal);
                    var saleTax = saleLines.Sum(line => line.TaxAmount);
                    var saleTotal = saleLines.Sum(line => line.Quantity * line.UnitPrice);
                    var saleCogs = saleLines.Sum(line => line.Quantity * line.UnitCost);

                    var returnSubtotal = Math.Abs(returnLines.Sum(line => line.LineSubTotal));
                    var returnTax = Math.Abs(returnLines.Sum(line => line.TaxAmount));
                    var returnTotal = Math.Abs(returnLines.Sum(line => line.Quantity * line.UnitPrice));
                    var returnCogs = Math.Abs(returnLines.Sum(line => line.Quantity * line.UnitCost));

                    var discount = Math.Max(0m, invoice.DiscountAmount ?? 0m);
                    saleSubtotal = Math.Max(0m, saleSubtotal - discount);
                    saleTotal = Math.Max(0m, saleTotal - discount);

                    if (saleTotal > 0)
                        AddDebit(lines, settlementAccountId, saleTotal, $"Return invoice #{invoice.InvoiceNumber} new item sale collection");
                    if (saleSubtotal > 0)
                        AddCredit(lines, salesRevenueId, saleSubtotal, $"Return invoice #{invoice.InvoiceNumber} new item sales");
                    if (saleTax > 0)
                        AddCredit(lines, outputTaxId, saleTax, $"Return invoice #{invoice.InvoiceNumber} new item sales tax");
                    if (saleCogs > 0)
                    {
                        AddDebit(lines, cogsId, saleCogs, $"Return invoice #{invoice.InvoiceNumber} new item cost of goods sold");
                        AddCredit(lines, inventoryId, saleCogs, $"Return invoice #{invoice.InvoiceNumber} new item inventory release");
                    }

                    if (returnSubtotal > 0)
                        AddDebit(lines, salesReturnsId, returnSubtotal, $"Sales return #{invoice.InvoiceNumber}");
                    if (returnTax > 0)
                        AddDebit(lines, outputTaxId, returnTax, $"Sales return #{invoice.InvoiceNumber} tax reversal");
                    if (returnTotal > 0)
                        AddCredit(lines, settlementAccountId, returnTotal, $"Sales return #{invoice.InvoiceNumber} refund",
                            customerId: invoice.PaymentType == PaymentType.Credit ? invoice.CustomerId : null);
                    if (returnCogs > 0)
                    {
                        AddDebit(lines, inventoryId, returnCogs, $"Sales return #{invoice.InvoiceNumber} inventory recovery");
                        AddCredit(lines, cogsId, returnCogs, $"Sales return #{invoice.InvoiceNumber} cost reversal");
                    }
                    break;

                case InvoiceType.Exchange:
                    var exchangeLines = invoice.InvoiceLines?.ToList() ?? new List<InvoiceLineWriteDto>();
                    var exchangeSaleLines = exchangeLines.Where(line => line.Quantity > 0).ToList();
                    var exchangeReturnLines = exchangeLines.Where(line => line.Quantity < 0).ToList();

                    var exchangeSalesSubTotal = exchangeSaleLines.Sum(line => line.LineSubTotal);
                    var exchangeSalesTax = exchangeSaleLines.Sum(line => line.TaxAmount);
                    var exchangeSalesTotal = exchangeSaleLines.Sum(line => line.Quantity * line.UnitPrice);
                    var exchangeSalesCogs = exchangeSaleLines.Sum(line => line.Quantity * line.UnitCost);

                    var exchangeReturnsSubTotal = Math.Abs(exchangeReturnLines.Sum(line => line.LineSubTotal));
                    var exchangeReturnsTax = Math.Abs(exchangeReturnLines.Sum(line => line.TaxAmount));
                    var exchangeReturnsTotal = Math.Abs(exchangeReturnLines.Sum(line => line.Quantity * line.UnitPrice));
                    var exchangeReturnsCogs = Math.Abs(exchangeReturnLines.Sum(line => line.Quantity * line.UnitCost));

                    AddDebit(lines, settlementAccountId, exchangeSalesTotal, $"Exchange #{invoice.InvoiceNumber} sale collection");
                    AddCredit(lines, salesRevenueId, exchangeSalesSubTotal, $"Exchange #{invoice.InvoiceNumber} sales");
                    if (exchangeSalesTax > 0)
                        AddCredit(lines, outputTaxId, exchangeSalesTax, $"Exchange #{invoice.InvoiceNumber} sales tax");
                    if (exchangeSalesCogs > 0)
                    {
                        AddDebit(lines, cogsId, exchangeSalesCogs, $"Exchange #{invoice.InvoiceNumber} cost of goods sold");
                        AddCredit(lines, inventoryId, exchangeSalesCogs, $"Exchange #{invoice.InvoiceNumber} inventory release");
                    }

                    AddDebit(lines, salesReturnsId, exchangeReturnsSubTotal, $"Exchange #{invoice.InvoiceNumber} return");
                    if (exchangeReturnsTax > 0)
                        AddDebit(lines, outputTaxId, exchangeReturnsTax, $"Exchange #{invoice.InvoiceNumber} return tax reversal");
                    AddCredit(lines, settlementAccountId, exchangeReturnsTotal, $"Exchange #{invoice.InvoiceNumber} return refund");
                    if (exchangeReturnsCogs > 0)
                    {
                        AddDebit(lines, inventoryId, exchangeReturnsCogs, $"Exchange #{invoice.InvoiceNumber} inventory recovery");
                        AddCredit(lines, cogsId, exchangeReturnsCogs, $"Exchange #{invoice.InvoiceNumber} cost reversal");
                    }
                    break;

                case InvoiceType.Purchase:
                    AddDebit(lines, inventoryId, invoice.SubTotal, $"Purchase invoice #{invoice.InvoiceNumber} inventory");
                    if (invoice.TotalTax > 0)
                        AddDebit(lines, inputTaxId, invoice.TotalTax, $"Purchase invoice #{invoice.InvoiceNumber} input tax");
                    if ((invoice.DiscountAmount ?? 0m) > 0)
                        AddCredit(lines, purchaseDiscountId, invoice.DiscountAmount!.Value, $"Purchase invoice #{invoice.InvoiceNumber} discount");
                    AddCredit(lines, settlementAccountId, invoice.TotalAmount, $"Purchase invoice #{invoice.InvoiceNumber} payment",
                        supplierId: invoice.PaymentType == PaymentType.Credit ? invoice.SupplierId : null);
                    break;

                case InvoiceType.PurchaseReturn:
                    var purchaseReturnTax = Math.Abs(invoice.TotalTax);
                    var purchaseReturnTotal = Math.Abs(invoice.TotalAmount);
                    var purchaseReturnNetSales = Math.Abs(invoice.NetSales);

                    AddDebit(lines, settlementAccountId, purchaseReturnTotal, $"Purchase return #{invoice.InvoiceNumber} refund",
                        supplierId: invoice.PaymentType == PaymentType.Credit ? invoice.SupplierId : null);
                    if (purchaseReturnTax > 0)
                        AddCredit(lines, inputTaxId, purchaseReturnTax, $"Purchase return #{invoice.InvoiceNumber} input tax reversal");
                    AddCredit(lines, inventoryId, purchaseReturnNetSales, $"Purchase return #{invoice.InvoiceNumber} inventory reversal");
                    break;

                default:
                    return Result<JournalEntryReadDto>.Ok(new JournalEntryReadDto(), "Invoice type does not create an accounting entry.");
            }

            LogAccountingTiming("invoice accounting line construction", totalTiming, stepTiming);
            var result = await PostJournalEntryAsync(new JournalEntryWriteDto
            {
                EntryDate = entryDate,
                Description = BuildInvoiceDescription(invoice),
                ReferenceType = "Invoice",
                ReferenceId = invoice.Id,
                Lines = lines
            });
            LogAccountingTiming("invoice accounting total", totalTiming, stepTiming);
            return result;
        }

        private static void LogAccountingTiming(string step, Stopwatch totalTiming, Stopwatch stepTiming)
        {
            var stepMilliseconds = stepTiming.ElapsedMilliseconds;
            var totalMilliseconds = totalTiming.ElapsedMilliseconds;
            PosPerformanceLogger.Write(step, stepMilliseconds, totalMilliseconds);
            Debug.WriteLine($"[POS timing] {step}: {stepMilliseconds} ms (total {totalMilliseconds} ms)");
            stepTiming.Restart();
        }

        public async Task<Result<JournalEntryReadDto>> PostVoucherEntryAsync(VoucherWriteDto voucher)
        {
            if (voucher.Id <= 0)
                return Result<JournalEntryReadDto>.Fail("Voucher id is required.");

            if (await TryGetExistingEntryAsync("Voucher", voucher.Id) is { } existing)
                return Result<JournalEntryReadDto>.Ok(existing, "Journal entry already exists for this voucher.");

            if (voucher.Amount <= 0)
                return Result<JournalEntryReadDto>.Ok(new JournalEntryReadDto(), "Voucher amount is zero.");

            if (voucher.VoucherType is not VoucherType.Receipt and not VoucherType.Payment)
                return Result<JournalEntryReadDto>.Ok(new JournalEntryReadDto(), "Voucher type does not create an accounting entry.");

            var settlementAccountId = await ResolveVoucherSettlementAccountIdAsync(voucher);
            var counterpartAccountId = await ResolveVoucherCounterpartAccountIdAsync(voucher);
            var description = BuildVoucherDescription(voucher);
            var lines = new List<JournalEntryLineWriteDto>();

            if (voucher.VoucherType == VoucherType.Receipt)
            {
                AddDebit(lines, settlementAccountId, voucher.Amount, description);
                AddCredit(lines, counterpartAccountId, voucher.Amount, description,
                    customerId: voucher.CustomerId);
            }
            else
            {
                AddDebit(lines, counterpartAccountId, voucher.Amount, description,
                    supplierId: voucher.SupplierId);
                AddCredit(lines, settlementAccountId, voucher.Amount, description);
            }

            return await PostJournalEntryAsync(new JournalEntryWriteDto
            {
                EntryDate = voucher.CreatedDate == default ? GetJordanNow() : voucher.CreatedDate,
                Description = description,
                ReferenceType = "Voucher",
                ReferenceId = voucher.Id,
                Lines = lines
            });
        }

        public async Task<Result<JournalEntryReadDto>> PostStockDocumentEntryAsync(StockDocumentWriteDto document)
        {
            if (document.Id <= 0)
                return Result<JournalEntryReadDto>.Fail("Stock document id is required.");

            if (await TryGetExistingEntryAsync("StockDocument", document.Id) is { } existing)
                return Result<JournalEntryReadDto>.Ok(existing, "Journal entry already exists for this stock document.");

            if (document.Items == null || document.Items.Count == 0)
                return Result<JournalEntryReadDto>.Ok(new JournalEntryReadDto(), "Stock document has no items to post.");

            var grossAmount = document.Items.Sum(x => Math.Max(0m, x.Quantity * x.PurchasePrice - x.LineDiscountAmount));
            var discount = Math.Clamp(document.DiscountAmount ?? 0m, 0m, Math.Max(grossAmount, 0m));
            var totalAmount = grossAmount - discount;
            if (totalAmount <= 0)
                return Result<JournalEntryReadDto>.Ok(new JournalEntryReadDto(), "Stock document amount is zero.");

            var inventoryId = await ResolveSystemAccountIdAsync(InventoryAccountCodeKey, "1150000000");
            var stockGainId = await ResolveSystemAccountIdAsync(StockGainAccountCodeKey, "4140000000");
            var purchaseDiscountId = await ResolveSystemAccountIdAsync(PurchaseDiscountAccountCodeKey, "4150000000");
            var stockLossId = await ResolveSystemAccountIdAsync(StockLossAccountCodeKey, "5120000000");
            var internalConsumptionId = await ResolveSystemAccountIdAsync(InternalConsumptionAccountCodeKey, "5140000000");
            var accountsPayableId = await ResolveSystemAccountIdAsync(AccountsPayableAccountCodeKey, "2110000000");
            var description = BuildStockDocumentDescription(document);
            var lines = new List<JournalEntryLineWriteDto>();

            if (document.Type == StockVoucherType.In)
            {
                AddDebit(lines, inventoryId, totalAmount, description);
                if (discount > 0)
                    AddCredit(lines, purchaseDiscountId, discount, description + " purchase discount");

                var settlementAccountId = document.PaymentType.HasValue
                    ? await ResolveSettlementAccountIdAsync(document.PaymentType, isPurchaseSide: true)
                    : document.SupplierId.HasValue ? accountsPayableId : stockGainId;
                AddCredit(
                    lines,
                    settlementAccountId,
                    totalAmount,
                    description,
                    supplierId: document.PaymentType == PaymentType.Credit ? document.SupplierId : null);
            }
            else if (document.Type == StockVoucherType.Out)
            {
                AddDebit(lines, document.SupplierId.HasValue ? stockLossId : internalConsumptionId, totalAmount, description);
                AddCredit(lines, inventoryId, totalAmount, description);
            }
            else
            {
                return Result<JournalEntryReadDto>.Ok(new JournalEntryReadDto(), "Stock document type does not create an accounting entry.");
            }

            return await PostJournalEntryAsync(new JournalEntryWriteDto
            {
                EntryDate = document.CreatedDate == default ? GetJordanNow() : document.CreatedDate,
                Description = description,
                ReferenceType = "StockDocument",
                ReferenceId = document.Id,
                Lines = lines
            });
        }

        public async Task<Result<JournalEntryReadDto>> PostFinancialTransactionEntryAsync(FinancialPostDto transaction, int persistedTransactionId)
        {
            if (persistedTransactionId <= 0)
                return Result<JournalEntryReadDto>.Fail("Financial transaction id is required.");

            if (ShouldSkipFinancialJournal(transaction.SourceType))
                return Result<JournalEntryReadDto>.Ok(new JournalEntryReadDto(), "Financial transaction source is journaled by another module.");

            if (await TryGetExistingEntryAsync("FinancialTransaction", persistedTransactionId) is { } existing)
                return Result<JournalEntryReadDto>.Ok(existing, "Journal entry already exists for this financial transaction.");

            var cashAccountId = await ResolveCashLikeAccountIdAsync(transaction.Method);
            var counterpartAccountId = await ResolveFinancialCounterpartAccountIdAsync(transaction);
            var description = BuildFinancialDescription(transaction, persistedTransactionId);

            var lines = new List<JournalEntryLineWriteDto>();
            if (transaction.Direction == TransactionDirection.In)
            {
                AddDebit(lines, cashAccountId, transaction.Amount, description);
                AddCredit(lines, counterpartAccountId, transaction.Amount, description);
            }
            else
            {
                AddDebit(lines, counterpartAccountId, transaction.Amount, description);
                AddCredit(lines, cashAccountId, transaction.Amount, description);
            }

            return await PostJournalEntryAsync(new JournalEntryWriteDto
            {
                EntryDate = transaction.TransactionDate == default ? GetJordanNow() : transaction.TransactionDate,
                Description = description,
                ReferenceType = "FinancialTransaction",
                ReferenceId = persistedTransactionId,
                Lines = lines
            });
        }

        public async Task<Result<JournalEntryReadDto>> PostStockAdjustmentEntryAsync(StockAdjustmentWriteDto adjustment)
        {
            if (adjustment.Id <= 0)
                return Result<JournalEntryReadDto>.Fail("Stock adjustment id is required.");

            if (await TryGetExistingEntryAsync("StockAdjustment", adjustment.Id) is { } existing)
                return Result<JournalEntryReadDto>.Ok(existing, "Journal entry already exists for this stock adjustment.");

            if (adjustment.AdjustmentType is StockAdjustmentType.Replace or StockAdjustmentType.CloseAndRecreate)
                return Result<JournalEntryReadDto>.Ok(new JournalEntryReadDto(), "Replacement adjustments do not create value-impacting journal entries.");

            var amount = Math.Abs(adjustment.BaseQuantityDelta) * (adjustment.PurchasePrice ?? 0m);
            if (amount <= 0)
                return Result<JournalEntryReadDto>.Ok(new JournalEntryReadDto(), "Adjustment amount is zero.");

            var inventoryId = await ResolveSystemAccountIdAsync(InventoryAccountCodeKey, "1150000000");
            var stockGainId = await ResolveSystemAccountIdAsync(StockGainAccountCodeKey, "4140000000");
            var stockLossId = await ResolveSystemAccountIdAsync(StockLossAccountCodeKey, "5120000000");
            var description = $"Stock adjustment #{adjustment.Id} - {adjustment.AdjustmentType}";
            var lines = new List<JournalEntryLineWriteDto>();

            if (adjustment.AdjustmentType == StockAdjustmentType.Increase)
            {
                AddDebit(lines, inventoryId, amount, description);
                AddCredit(lines, stockGainId, amount, description);
            }
            else if (adjustment.AdjustmentType == StockAdjustmentType.Decrease)
            {
                AddDebit(lines, stockLossId, amount, description);
                AddCredit(lines, inventoryId, amount, description);
            }
            else
            {
                return Result<JournalEntryReadDto>.Ok(new JournalEntryReadDto(), "Adjustment type does not create an accounting entry.");
            }

            return await PostJournalEntryAsync(new JournalEntryWriteDto
            {
                EntryDate = adjustment.AdjustmentDate == default ? GetJordanNow() : adjustment.AdjustmentDate,
                Description = description,
                ReferenceType = "StockAdjustment",
                ReferenceId = adjustment.Id,
                Lines = lines
            });
        }

        public async Task<Result<(TrialBalanceSummaryDto summary, List<TrialBalanceRowDto> rows)>> GetTrialBalanceAsync(TrialBalanceFilterDto filter)
        {
            if (filter.From > filter.To)
            {
                return Result<(TrialBalanceSummaryDto summary, List<TrialBalanceRowDto> rows)>.Fail("Invalid date range.");
            }

            var accounts = await _uow.Accounts.GetAllAsQueryable()
                .AsNoTracking()
                .OrderBy(x => x.Code)
                .ToListAsync();

            var lines = await BuildLedgerLineQuery(filter.IncludePostedOnly)
                .Where(x => x.EntryDate <= filter.To)
                .ToListAsync();

            var rows = accounts
                .Select(account =>
                {
                    var accountLines = lines.Where(x => x.AccountId == account.Id).ToList();
                    var openingBalance = accountLines
                        .Where(x => x.EntryDate < filter.From)
                        .Sum(x => x.Debit - x.Credit);
                    var periodDebit = accountLines
                        .Where(x => x.EntryDate >= filter.From && x.EntryDate <= filter.To)
                        .Sum(x => x.Debit);
                    var periodCredit = accountLines
                        .Where(x => x.EntryDate >= filter.From && x.EntryDate <= filter.To)
                        .Sum(x => x.Credit);
                    var closingBalance = openingBalance + periodDebit - periodCredit;

                    return new TrialBalanceRowDto
                    {
                        AccountId = account.Id,
                        AccountCode = account.Code,
                        AccountName = account.Name,
                        AccountType = account.AccountType,
                        OpeningBalance = openingBalance,
                        Debit = periodDebit,
                        Credit = periodCredit,
                        ClosingBalance = closingBalance,
                        ClosingDebit = closingBalance > 0 ? closingBalance : 0m,
                        ClosingCredit = closingBalance < 0 ? Math.Abs(closingBalance) : 0m
                    };
                })
                .Where(x => filter.IncludeZeroBalances || x.OpeningBalance != 0m || x.Debit != 0m || x.Credit != 0m || x.ClosingBalance != 0m)
                .ToList();

            var summary = new TrialBalanceSummaryDto
            {
                TotalOpeningBalance = rows.Sum(x => x.OpeningBalance),
                TotalDebit = rows.Sum(x => x.Debit),
                TotalCredit = rows.Sum(x => x.Credit),
                TotalClosingDebit = rows.Sum(x => x.ClosingDebit),
                TotalClosingCredit = rows.Sum(x => x.ClosingCredit)
            };

            return Result<(TrialBalanceSummaryDto summary, List<TrialBalanceRowDto> rows)>.Ok((summary, rows));
        }

        public async Task<Result<List<GeneralLedgerAccountDto>>> GetGeneralLedgerAsync(GeneralLedgerFilterDto filter)
        {
            if (filter.From > filter.To)
            {
                return Result<List<GeneralLedgerAccountDto>>.Fail("Invalid date range.");
            }

            if (filter.AccountId.HasValue)
            {
                var selectedAccount = await _uow.Accounts.GetAllAsQueryable()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == filter.AccountId.Value);

                if (selectedAccount == null)
                {
                    return Result<List<GeneralLedgerAccountDto>>.Ok(new List<GeneralLedgerAccountDto>());
                }

                var allAccounts = await _uow.Accounts.GetAllAsQueryable()
                    .AsNoTracking()
                    .Select(x => new AccountScopeNode
                    {
                        Id = x.Id,
                        ParentAccountId = x.ParentAccountId
                    })
                    .ToListAsync();

                var scopedAccountIds = ResolveAccountScopeIds(selectedAccount.Id, allAccounts);
                var scopedLines = await BuildLedgerLineQuery(filter.IncludePostedOnly)
                    .Where(x => scopedAccountIds.Contains(x.AccountId))
                    .Where(x => x.EntryDate <= filter.To)
                    .OrderBy(x => x.EntryDate)
                    .ThenBy(x => x.EntryNumber)
                    .ToListAsync();

                var openingBalance = scopedLines
                    .Where(x => x.EntryDate < filter.From)
                    .Sum(x => x.Debit - x.Credit);

                var rows = new List<GeneralLedgerRowDto>();
                var runningBalance = openingBalance;

                if (filter.IncludeOpeningBalance)
                {
                    rows.Add(new GeneralLedgerRowDto
                    {
                        EntryDate = filter.From.Date,
                        EntryNumber = "OPENING",
                        Description = "الرصيد الافتتاحي",
                        RunningBalance = runningBalance,
                        IsOpeningBalance = true
                    });
                }

                foreach (var line in scopedLines.Where(x => x.EntryDate >= filter.From && x.EntryDate <= filter.To))
                {
                    runningBalance += line.Debit - line.Credit;
                    rows.Add(new GeneralLedgerRowDto
                    {
                        EntryDate = line.EntryDate,
                        EntryNumber = line.EntryNumber,
                        Description = AccountingTextLocalizer.ToArabic(string.IsNullOrWhiteSpace(line.LineDescription) ? line.EntryDescription : line.LineDescription!),
                        ReferenceType = line.ReferenceType,
                        ReferenceId = line.ReferenceId,
                        Debit = line.Debit,
                        Credit = line.Credit,
                        RunningBalance = runningBalance
                    });
                }

                if (!rows.Any() && openingBalance == 0m)
                {
                    return Result<List<GeneralLedgerAccountDto>>.Ok(new List<GeneralLedgerAccountDto>());
                }

                return Result<List<GeneralLedgerAccountDto>>.Ok(new List<GeneralLedgerAccountDto>
                {
                    new GeneralLedgerAccountDto
                    {
                        AccountId = selectedAccount.Id,
                        AccountCode = selectedAccount.Code,
                        AccountName = selectedAccount.Name,
                        AccountType = selectedAccount.AccountType,
                        OpeningBalance = openingBalance,
                        TotalDebit = rows.Where(x => !x.IsOpeningBalance).Sum(x => x.Debit),
                        TotalCredit = rows.Where(x => !x.IsOpeningBalance).Sum(x => x.Credit),
                        ClosingBalance = runningBalance,
                        Rows = rows
                    }
                });
            }

            var accounts = await _uow.Accounts.GetAllAsQueryable()
                .AsNoTracking()
                .OrderBy(x => x.Code)
                .ToListAsync();
            if (!accounts.Any())
            {
                return Result<List<GeneralLedgerAccountDto>>.Ok(new List<GeneralLedgerAccountDto>());
            }

            var accountIds = accounts.Select(x => x.Id).ToList();
            var lines = await BuildLedgerLineQuery(filter.IncludePostedOnly)
                .Where(x => accountIds.Contains(x.AccountId))
                .Where(x => x.EntryDate <= filter.To)
                .ToListAsync();

            var ledgers = new List<GeneralLedgerAccountDto>();
            foreach (var account in accounts)
            {
                var accountLines = lines.Where(x => x.AccountId == account.Id)
                    .OrderBy(x => x.EntryDate)
                    .ThenBy(x => x.EntryNumber)
                    .ToList();

                var openingBalance = accountLines
                    .Where(x => x.EntryDate < filter.From)
                    .Sum(x => x.Debit - x.Credit);

                var rows = new List<GeneralLedgerRowDto>();
                var runningBalance = openingBalance;

                if (filter.IncludeOpeningBalance)
                {
                    rows.Add(new GeneralLedgerRowDto
                    {
                        EntryDate = filter.From.Date,
                        EntryNumber = "OPENING",
                        Description = "الرصيد الافتتاحي",
                        RunningBalance = runningBalance,
                        IsOpeningBalance = true
                    });
                }

                foreach (var line in accountLines.Where(x => x.EntryDate >= filter.From && x.EntryDate <= filter.To))
                {
                    runningBalance += line.Debit - line.Credit;
                    rows.Add(new GeneralLedgerRowDto
                    {
                        EntryDate = line.EntryDate,
                        EntryNumber = line.EntryNumber,
                        Description = AccountingTextLocalizer.ToArabic(string.IsNullOrWhiteSpace(line.LineDescription) ? line.EntryDescription : line.LineDescription!),
                        ReferenceType = line.ReferenceType,
                        ReferenceId = line.ReferenceId,
                        Debit = line.Debit,
                        Credit = line.Credit,
                        RunningBalance = runningBalance
                    });
                }

                if (!rows.Any() && openingBalance == 0m)
                {
                    continue;
                }

                ledgers.Add(new GeneralLedgerAccountDto
                {
                    AccountId = account.Id,
                    AccountCode = account.Code,
                    AccountName = account.Name,
                    AccountType = account.AccountType,
                    OpeningBalance = openingBalance,
                    TotalDebit = rows.Where(x => !x.IsOpeningBalance).Sum(x => x.Debit),
                    TotalCredit = rows.Where(x => !x.IsOpeningBalance).Sum(x => x.Credit),
                    ClosingBalance = runningBalance,
                    Rows = rows
                });
            }

            return Result<List<GeneralLedgerAccountDto>>.Ok(ledgers);
        }

        public async Task<Result<BalanceSheetDto>> GetBalanceSheetAsync(BalanceSheetFilterDto filter)
        {
            var accountsQuery = _uow.Accounts.GetAllAsQueryable().AsNoTracking();
            if (!filter.IncludeInactiveAccounts)
            {
                accountsQuery = accountsQuery.Where(x => x.IsActive);
            }

            var accounts = await accountsQuery.OrderBy(x => x.Code).ToListAsync();
            var lines = await BuildLedgerLineQuery(filter.IncludePostedOnly)
                .Where(x => x.EntryDate <= filter.AsOfDate)
                .ToListAsync();

            var balances = accounts.Select(account => new AccountBalanceProjection
            {
                Account = account,
                Balance = lines
                    .Where(x => x.AccountId == account.Id)
                    .Sum(x => x.Debit - x.Credit)
            }).ToList();

            var netIncome = balances
                .Where(x => x.Account.AccountType == AccountType.Revenue || x.Account.AccountType == AccountType.Expense)
                .Sum(x => -x.Balance);

            var dto = new BalanceSheetDto
            {
                AsOfDate = filter.AsOfDate,
                Assets = BuildBalanceSheetSection("الأصول", balances, AccountType.Asset, filter.IncludeZeroBalances, false),
                Liabilities = BuildBalanceSheetSection("الالتزامات", balances, AccountType.Liability, filter.IncludeZeroBalances, true),
                Equity = BuildBalanceSheetSection("حقوق الملكية", balances, AccountType.Equity, filter.IncludeZeroBalances, true)
            };

            if (filter.IncludeZeroBalances || netIncome != 0m)
            {
                dto.Equity.Rows.Add(new BalanceSheetRowDto
                {
                    AccountId = 0,
                    AccountCode = "CURRENT-EARNINGS",
                    AccountName = "Current Period Earnings",
                    Balance = netIncome
                });
                dto.Equity.Total += netIncome;
            }

            return Result<BalanceSheetDto>.Ok(dto);
        }

        public async Task<Result<List<JournalEntryReadDto>>> GetJournalEntriesAsync(JournalEntryFilterDto filter)
        {
            var query = _uow.JournalEntries.GetAllAsQueryable()
                .Include(x => x.Lines)
                    .ThenInclude(x => x.Account)
                .AsNoTracking();

            if (filter.From.HasValue)
            {
                query = query.Where(x => x.EntryDate >= filter.From.Value.Date);
            }

            if (filter.To.HasValue)
            {
                query = query.Where(x => x.EntryDate <= filter.To.Value);
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(x => x.Status == filter.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.ReferenceType))
            {
                query = query.Where(x => x.ReferenceType == filter.ReferenceType);
            }

            if (!string.IsNullOrWhiteSpace(filter.ReferenceSearch))
            {
                var referenceSearch = filter.ReferenceSearch.Trim();
                if (int.TryParse(referenceSearch, out var referenceId))
                {
                    query = query.Where(x =>
                        (x.ReferenceType != null && x.ReferenceType.Contains(referenceSearch)) ||
                        (x.ReferenceNumber != null && x.ReferenceNumber.Contains(referenceSearch)) ||
                        x.ReferenceId == referenceId ||
                        (x.Description != null && x.Description.Contains(referenceSearch)));
                }
                else
                {
                    query = query.Where(x =>
                        (x.ReferenceType != null && x.ReferenceType.Contains(referenceSearch)) ||
                        (x.ReferenceNumber != null && x.ReferenceNumber.Contains(referenceSearch)) ||
                        (x.Description != null && x.Description.Contains(referenceSearch)));
                }
            }

            if (!string.IsNullOrWhiteSpace(filter.AccountSearch))
            {
                var accountSearch = filter.AccountSearch.Trim();
                var matchingAccountIds = await _uow.Accounts.GetAllAsQueryable()
                    .Where(account => account.Code.Contains(accountSearch) ||
                                      (account.ArabicName != null && account.ArabicName.Contains(accountSearch)) ||
                                      (account.EnglishName != null && account.EnglishName.Contains(accountSearch)) ||
                                      account.Name.Contains(accountSearch))
                    .Select(account => account.Id)
                    .ToListAsync();

                query = matchingAccountIds.Count == 0
                    ? query.Where(_ => false)
                    : query.Where(x => x.Lines.Any(line => matchingAccountIds.Contains(line.AccountId)));
            }

            var entries = await query
                .OrderByDescending(x => x.EntryDate)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            var result = _mapper.Map<List<JournalEntryReadDto>>(entries);
            return Result<List<JournalEntryReadDto>>.Ok(result);
        }

        public async Task<Result<JournalEntryReadDto>> ReverseJournalEntryAsync(int journalEntryId, string reason)
        {
            if (journalEntryId <= 0)
            {
                return Result<JournalEntryReadDto>.Fail("Journal entry id is required.");
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return Result<JournalEntryReadDto>.Fail("Reversal reason is required.");
            }

            var entry = await _uow.JournalEntries.GetAllAsQueryable()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == journalEntryId);

            if (entry == null)
            {
                return Result<JournalEntryReadDto>.Fail("Journal entry was not found.");
            }

            if (entry.Status != JournalEntryStatus.Posted)
            {
                return Result<JournalEntryReadDto>.Fail("Only posted journal entries can be reversed.");
            }

            if (string.Equals(entry.ReferenceType, "Reversal", StringComparison.OrdinalIgnoreCase))
            {
                return Result<JournalEntryReadDto>.Fail("Reversal entries cannot be reversed again.");
            }

            var existingReversal = await _uow.JournalEntries.GetAllAsQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ReferenceType == "Reversal" && x.ReferenceId == entry.Id);

            if (existingReversal != null)
            {
                return Result<JournalEntryReadDto>.Fail("This journal entry has already been reversed.");
            }

            var now = GetJordanNow();
            var postingLockDate = await GetPostingLockDateAsync();
            if (postingLockDate.HasValue && now.Date <= postingLockDate.Value.Date)
            {
                return Result<JournalEntryReadDto>.Fail($"Posting is locked through {postingLockDate:yyyy-MM-dd}.");
            }

            entry.Status = JournalEntryStatus.Reversed;
            entry.UpdatedDate = now;

            var reversal = new JournalEntry
            {
                EntryNumber = GenerateEntryNumber(now),
                EntryDate = now,
                Description = $"Reversal of {entry.EntryNumber}: {reason.Trim()}",
                Status = JournalEntryStatus.Posted,
                ReferenceType = "Reversal",
                ReferenceId = entry.Id,
                CreatedDate = now,
                UpdatedDate = now,
                Lines = entry.Lines.Select(line => new JournalEntryLine
                {
                    AccountId = line.AccountId,
                    PartyUserId = line.PartyUserId,
                    CustomerId = line.CustomerId,
                    SupplierId = line.SupplierId,
                    Debit = line.Credit,
                    Credit = line.Debit,
                    CostCenterId = line.CostCenterId,
                    CurrencyId = line.CurrencyId,
                    ForeignAmount = line.ForeignAmount,
                    ExchangeRate = line.ExchangeRate,
                    Description = $"Reversal - {line.Description ?? entry.Description}",
                    CreatedDate = now,
                    UpdatedDate = now
                }).ToList()
            };

            await _uow.JournalEntries.AddAsync(reversal);
            await _uow.CommitAsync();

            var savedReversal = await _uow.JournalEntries.GetAllAsQueryable()
                .Include(x => x.Lines)
                    .ThenInclude(x => x.Account)
                .AsNoTracking()
                .FirstAsync(x => x.Id == reversal.Id);

            var result = _mapper.Map<JournalEntryReadDto>(savedReversal);
            result.TotalDebit = savedReversal.Lines.Sum(x => x.Debit);
            result.TotalCredit = savedReversal.Lines.Sum(x => x.Credit);
            return Result<JournalEntryReadDto>.Ok(result, "Journal entry reversed successfully.");
        }

        public async Task<Result<JournalEntryReadDto>> ReverseJournalByReferenceAsync(string referenceType, int referenceId, string reason)
        {
            if (string.IsNullOrWhiteSpace(referenceType))
                return Result<JournalEntryReadDto>.Fail("Reference type is required.");

            if (referenceId <= 0)
                return Result<JournalEntryReadDto>.Fail("Reference id is required.");

            var entry = await _context.JournalEntries
                .AsNoTracking()
                .Where(x => x.ReferenceType == referenceType
                    && x.ReferenceId == referenceId
                    && x.Status == JournalEntryStatus.Posted)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (entry == null)
                return Result<JournalEntryReadDto>.Ok(new JournalEntryReadDto(), "No journal entry found for this reference.");

            return await ReverseJournalEntryAsync(entry.Id, reason);
        }

        private BalanceSheetSectionDto BuildBalanceSheetSection(
            string title,
            IEnumerable<AccountBalanceProjection> balances,
            AccountType accountType,
            bool includeZeroBalances,
            bool invertSign)
        {
            var rows = balances
                .Where(x => x.Account.AccountType == accountType)
                .Select(x =>
                {
                    decimal balance = x.Balance;
                    if (invertSign)
                    {
                        balance = -balance;
                    }

                    return new BalanceSheetRowDto
                    {
                        AccountId = x.Account.Id,
                        AccountCode = x.Account.Code,
                        AccountName = x.Account.Name,
                        Balance = balance
                    };
                })
                .Where(x => includeZeroBalances || x.Balance != 0m)
                .ToList();

            return new BalanceSheetSectionDto
            {
                Title = title,
                Total = rows.Sum(x => x.Balance),
                Rows = rows
            };
        }

        private IQueryable<LedgerLineProjection> BuildLedgerLineQuery(bool includePostedOnly)
        {
            var query = _context.JournalEntryLines
                .AsNoTracking()
                .Include(x => x.JournalEntry)
                .Select(x => new LedgerLineProjection
                {
                    AccountId = x.AccountId,
                    EntryDate = x.JournalEntry.EntryDate,
                    EntryNumber = x.JournalEntry.EntryNumber,
                    EntryDescription = x.JournalEntry.Description,
                    LineDescription = x.Description,
                    ReferenceType = x.JournalEntry.ReferenceType,
                    ReferenceId = x.JournalEntry.ReferenceId,
                    Debit = x.Debit,
                    Credit = x.Credit,
                    Status = x.JournalEntry.Status
                });

            if (includePostedOnly)
            {
                query = query.Where(x => x.Status == JournalEntryStatus.Posted);
            }

            return query;
        }

        private sealed class LedgerLineProjection
        {
            public int AccountId { get; set; }
            public DateTime EntryDate { get; set; }
            public string EntryNumber { get; set; } = string.Empty;
            public string EntryDescription { get; set; } = string.Empty;
            public string? LineDescription { get; set; }
            public string? ReferenceType { get; set; }
            public int? ReferenceId { get; set; }
            public decimal Debit { get; set; }
            public decimal Credit { get; set; }
            public JournalEntryStatus Status { get; set; }
        }

        private sealed class AccountBalanceProjection
        {
            public Account Account { get; set; } = null!;
            public decimal Balance { get; set; }
        }

        private static string GenerateEntryNumber(DateTime now)
        {
            return $"JE-{now:yyyyMMdd-HHmmssfff}";
        }

        private async Task<JournalEntryReadDto?> TryGetExistingEntryAsync(string referenceType, int referenceId)
        {
            var entry = await _uow.JournalEntries.GetAllAsQueryable()
                .Include(x => x.Lines)
                    .ThenInclude(x => x.Account)
                .AsNoTracking()
                .Where(x => x.ReferenceType == referenceType && x.ReferenceId == referenceId && x.Status != JournalEntryStatus.Reversed)
                .OrderByDescending(x => x.CreatedDate)
                .FirstOrDefaultAsync();

            return entry == null ? null : _mapper.Map<JournalEntryReadDto>(entry);
        }

        private async Task<int> ResolveSettlementAccountIdAsync(PaymentType? paymentType, bool isPurchaseSide)
        {
            if (paymentType == PaymentType.Credit)
                return await ResolveSystemAccountIdAsync(
                    isPurchaseSide ? AccountsPayableAccountCodeKey : AccountsReceivableAccountCodeKey,
                    isPurchaseSide ? "2110000000" : "1140000000");

            if (paymentType == PaymentType.Check && isPurchaseSide)
                return await ResolveSystemAccountIdAsync(IssuedChecksPayableAccountCodeKey, "2140000000");

            return await ResolveCashLikeAccountIdAsync(MapPaymentTypeToMethod(paymentType));
        }

        private async Task<Dictionary<string, int>> ResolveInvoiceAccountIdsAsync(InvoiceWriteDto invoice)
        {
            var settlementKey = GetSettlementAccountCodeKey(
                invoice.PaymentType,
                invoice.InvoiceType is InvoiceType.Purchase or InvoiceType.PurchaseReturn);
            var requiredCodes = new Dictionary<string, string>
            {
                [settlementKey] = settlementKey switch
                {
                    AccountsPayableAccountCodeKey => "2110000000",
                    AccountsReceivableAccountCodeKey => "1140000000",
                    IssuedChecksPayableAccountCodeKey => "2140000000",
                    ChecksInHandAccountCodeKey => "1180000000",
                    BankAccountCodeKey => "1130000000",
                    _ => "1110000000"
                },
                [SalesRevenueAccountCodeKey] = "4110000000",
                [SalesReturnsAccountCodeKey] = "4120000000",
                [SalesDiscountAccountCodeKey] = "4130000000",
                [PurchaseDiscountAccountCodeKey] = "4150000000",
                [InventoryAccountCodeKey] = "1150000000",
                [CostOfGoodsSoldAccountCodeKey] = "5110000000",
                [OutputTaxAccountCodeKey] = "2120000000",
                [InputTaxAccountCodeKey] = "1160000000"
            };

            var settings = await _context.AppSettings
                .AsNoTracking()
                .Where(setting => requiredCodes.Keys.Contains(setting.Key))
                .ToDictionaryAsync(setting => setting.Key, setting => setting.Value);

            var normalizedCodesByKey = requiredCodes.ToDictionary(
                pair => pair.Key,
                pair => NormalizeAccountCode(
                    settings.TryGetValue(pair.Key, out var configuredCode) && !string.IsNullOrWhiteSpace(configuredCode)
                        ? configuredCode.Trim()
                        : pair.Value));
            var normalizedCodes = normalizedCodesByKey.Values.Distinct().ToList();

            var accounts = await _uow.Accounts.GetAllAsQueryable()
                .Where(account => normalizedCodes.Contains(account.Code) || normalizedCodes.Contains(account.AccountCode))
                .Where(account => account.IsActive && account.IsPosting)
                .ToListAsync();

            var accountIds = new Dictionary<string, int>();
            foreach (var pair in normalizedCodesByKey)
            {
                var account = accounts.FirstOrDefault(item =>
                    item.Code == pair.Value || item.AccountCode == pair.Value);
                if (account == null)
                    throw new InvalidOperationException($"Accounting setup is incomplete. Account code '{pair.Value}' is missing.");

                accountIds[pair.Key] = account.Id;
            }

            return accountIds;
        }

        private static string GetSettlementAccountCodeKey(PaymentType? paymentType, bool isPurchaseSide)
        {
            if (paymentType == PaymentType.Credit)
                return isPurchaseSide ? AccountsPayableAccountCodeKey : AccountsReceivableAccountCodeKey;

            if (paymentType == PaymentType.Check && isPurchaseSide)
                return IssuedChecksPayableAccountCodeKey;

            return MapPaymentTypeToMethod(paymentType) switch
            {
                PaymentMethod.Cash => CashMainAccountCodeKey,
                PaymentMethod.Credit => AccountsReceivableAccountCodeKey,
                PaymentMethod.Check => ChecksInHandAccountCodeKey,
                _ => BankAccountCodeKey
            };
        }

        private async Task<int> ResolveCashLikeAccountIdAsync(PaymentMethod? method)
        {
            return method switch
            {
                PaymentMethod.Cash => await ResolveSystemAccountIdAsync(CashMainAccountCodeKey, "1110000000"),
                PaymentMethod.Credit => await ResolveSystemAccountIdAsync(AccountsReceivableAccountCodeKey, "1140000000"),
                PaymentMethod.Check => await ResolveSystemAccountIdAsync(ChecksInHandAccountCodeKey, "1180000000"),
                _ => await ResolveSystemAccountIdAsync(BankAccountCodeKey, "1130000000")
            };
        }

        private async Task<int> ResolveFinancialCounterpartAccountIdAsync(FinancialPostDto transaction)
        {
            return transaction.SourceType switch
            {
                FinancialSourceType.ReceiptVoucher => await ResolveSystemAccountIdAsync(AccountsReceivableAccountCodeKey, "1140000000"),
                FinancialSourceType.PaymentVoucher => await ResolveSystemAccountIdAsync(AccountsPayableAccountCodeKey, "2110000000"),
                FinancialSourceType.Expense => await ResolveSystemAccountIdAsync(GeneralExpenseAccountCodeKey, "5130000000"),
                FinancialSourceType.SessionOpening => await ResolveSystemAccountIdAsync(OtherPayablesAccountCodeKey, "2130000000"),
                FinancialSourceType.SessionClosing => await ResolveSystemAccountIdAsync(OtherPayablesAccountCodeKey, "2130000000"),
                FinancialSourceType.Manual when transaction.Direction == TransactionDirection.In => await ResolveSystemAccountIdAsync(OtherReceivablesAccountCodeKey, "1170000000"),
                FinancialSourceType.Manual => await ResolveSystemAccountIdAsync(OtherPayablesAccountCodeKey, "2130000000"),
                _ when transaction.Direction == TransactionDirection.In => await ResolveSystemAccountIdAsync(OtherReceivablesAccountCodeKey, "1170000000"),
                _ => await ResolveSystemAccountIdAsync(GeneralExpenseAccountCodeKey, "5130000000")
            };
        }

        private static bool ShouldSkipFinancialJournal(FinancialSourceType sourceType)
        {
            return sourceType is FinancialSourceType.SaleInvoice
                or FinancialSourceType.PosSaleInvoice
                or FinancialSourceType.PurchaseInvoice
                or FinancialSourceType.SaleReturn
                or FinancialSourceType.PurchaseReturn
                or FinancialSourceType.ReceiptVoucher
                or FinancialSourceType.PaymentVoucher;
        }

        private static string BuildInvoiceDescription(InvoiceWriteDto invoice)
        {
            var channel = invoice.IsPOS == true ? "POS" : "Invoice";
            return $"{channel} {invoice.InvoiceType} #{invoice.InvoiceNumber}";
        }

        private static string BuildFinancialDescription(FinancialPostDto transaction, int transactionId)
        {
            return $"{transaction.SourceType} #{transactionId}";
        }

        private static string BuildVoucherDescription(VoucherWriteDto voucher)
        {
            return $"Voucher {voucher.VoucherType} #{voucher.VoucherNumber ?? voucher.Id.ToString()}";
        }

        private static string BuildStockDocumentDescription(StockDocumentWriteDto document)
        {
            return $"Stock document {document.Type} #{document.DocumentNumber}";
        }

        private static PaymentMethod? MapPaymentTypeToMethod(PaymentType? paymentType)
        {
            return paymentType switch
            {
                PaymentType.Cash => PaymentMethod.Cash,
                PaymentType.Visa => PaymentMethod.Visa,
                PaymentType.Master => PaymentMethod.Master,
                PaymentType.Debit => PaymentMethod.BankTransfer,
                PaymentType.Check => PaymentMethod.Check,
                PaymentType.MobilePayment => PaymentMethod.MobilePayment,
                PaymentType.Credit => PaymentMethod.Credit,
                _ => PaymentMethod.Cash
            };
        }

        private async Task<int> GetAccountIdByCodeAsync(string code)
        {
            code = NormalizeAccountCode(code);

            var accountId = await _uow.Accounts.GetAllAsQueryable()
                .Where(x => (x.Code == code || x.AccountCode == code) && x.IsActive && x.IsPosting)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            if (accountId == 0)
                throw new InvalidOperationException($"Accounting setup is incomplete. Account code '{code}' is missing.");

            return accountId;
        }

        private async Task<int> ResolveSystemAccountIdAsync(string key, string fallbackCode)
        {
            var configuredCode = await _context.AppSettings
                .AsNoTracking()
                .Where(x => x.Key == key)
                .Select(x => x.Value)
                .FirstOrDefaultAsync();

            var codeToUse = string.IsNullOrWhiteSpace(configuredCode) ? fallbackCode : configuredCode.Trim();
            codeToUse = NormalizeAccountCode(codeToUse);
            return await GetAccountIdByCodeAsync(codeToUse);
        }

        private static string NormalizeAccountCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return code;

            var trimmed = code.Trim();
            if (AccountCodeHelper.IsFlatAccountCode(trimmed))
                return trimmed;

            return LegacyAccountCodeMap.TryGetValue(trimmed, out var mappedCode)
                ? mappedCode
                : trimmed;
        }

        private async Task EnsureDefaultAccountSettingsAsync(DateTime now)
        {
            var defaults = new Dictionary<string, (string Value, string Description)>
            {
                [CashMainAccountCodeKey] = ("1110000000", "Default cash account code for accounting posting."),
                [BankAccountCodeKey] = ("1130000000", "Default bank account code for accounting posting."),
                [AccountsReceivableAccountCodeKey] = ("1140000000", "Default accounts receivable account code for accounting posting."),
                [InputTaxAccountCodeKey] = ("1160000000", "Default input tax account code for accounting posting."),
                [InventoryAccountCodeKey] = ("1150000000", "Default inventory account code for accounting posting."),
                [ChecksInHandAccountCodeKey] = ("1180000000", "Default checks in hand account code for accounting posting."),
                [IssuedChecksPayableAccountCodeKey] = ("2140000000", "Default issued checks payable account code for accounting posting."),
                [OtherReceivablesAccountCodeKey] = ("1170000000", "Default other receivables account code for accounting posting."),
                [AccountsPayableAccountCodeKey] = ("2110000000", "Default accounts payable account code for accounting posting."),
                [OutputTaxAccountCodeKey] = ("2120000000", "Default output tax account code for accounting posting."),
                [OtherPayablesAccountCodeKey] = ("2130000000", "Default other payables account code for accounting posting."),
                [SalesRevenueAccountCodeKey] = ("4110000000", "Default sales revenue account code for accounting posting."),
                [SalesReturnsAccountCodeKey] = ("4120000000", "Default sales returns account code for accounting posting."),
                [SalesDiscountAccountCodeKey] = ("4130000000", "Default sales discount account code for accounting posting."),
                [PurchaseDiscountAccountCodeKey] = ("4150000000", "Default purchase discount account code for accounting posting."),
                [StockGainAccountCodeKey] = ("4140000000", "Default stock gain account code for accounting posting."),
                [CostOfGoodsSoldAccountCodeKey] = ("5110000000", "Default cost of goods sold account code for accounting posting."),
                [GeneralExpenseAccountCodeKey] = ("5130000000", "Default general expense account code for accounting posting."),
                [StockLossAccountCodeKey] = ("5120000000", "Default stock loss account code for accounting posting."),
                [PosCashAccountCodeKey] = ("1120000000", "Default POS cash account code for accounting posting."),
                [InternalConsumptionAccountCodeKey] = ("5140000000", "Default internal consumption account code for stock out posting.")
            };

            var legacyDefaultCodes = new HashSet<string>
            {
                "1000", "1100", "1200", "1210", "1300", "1400", "2000", "2100", "2200",
                "4000", "4100", "4200", "4300", "5000", "6000", "6100",
                "0000000001.0000000001.0000000001", "0000000001.0000000001.0000000002", "0000000001.0000000001.0000000003",
                "0000000001.0000000001.0000000004", "0000000001.0000000001.0000000005", "0000000001.0000000001.0000000006",
                "0000000001.0000000001.0000000007", "0000000002.0000000001.0000000001", "0000000002.0000000001.0000000002",
                "0000000002.0000000001.0000000003", "0000000004.0000000001.0000000001", "0000000004.0000000001.0000000002",
                "0000000004.0000000001.0000000003", "0000000004.0000000001.0000000004", "0000000005.0000000001.0000000001",
                "0000000005.0000000001.0000000002", "0000000005.0000000001.0000000003", "0000000005.0000000001.0000000004"
            };

            var existing = await _context.AppSettings
                .Where(x => defaults.Keys.Contains(x.Key))
                .ToDictionaryAsync(x => x.Key, x => x);

            foreach (var pair in defaults)
            {
                if (existing.ContainsKey(pair.Key))
                {
                    var setting = existing[pair.Key];
                    if (string.IsNullOrWhiteSpace(setting.Value) || legacyDefaultCodes.Contains(setting.Value))
                    {
                        setting.Value = pair.Value.Value;
                        setting.Description = pair.Value.Description;
                        setting.UpdatedDate = now;
                    }

                    continue;
                }

                _context.AppSettings.Add(new AppSetting
                {
                    Key = pair.Key,
                    Value = pair.Value.Value,
                    Description = pair.Value.Description,
                    CreatedDate = now,
                    UpdatedDate = now
                });
            }

            await _context.SaveChangesAsync();
        }

        private async Task<int> ResolveVoucherSettlementAccountIdAsync(VoucherWriteDto voucher)
        {
            var method = MapPaymentTypeToMethod(voucher.PaymentType);
            if (voucher.VoucherType == VoucherType.Payment && method == PaymentMethod.Check)
                return await ResolveSystemAccountIdAsync(IssuedChecksPayableAccountCodeKey, "2140000000");
            if (voucher.CashierSessionId.HasValue && method == PaymentMethod.Cash)
                return await ResolveSystemAccountIdAsync(PosCashAccountCodeKey, "1120000000");

            return await ResolveCashLikeAccountIdAsync(method);
        }

        private async Task<int> ResolveVoucherCounterpartAccountIdAsync(VoucherWriteDto voucher)
        {
            if (voucher.VoucherType == VoucherType.Receipt && voucher.CustomerId.HasValue)
                return await ResolveSystemAccountIdAsync(AccountsReceivableAccountCodeKey, "1140000000");

            if (voucher.VoucherType == VoucherType.Payment && voucher.SupplierId.HasValue)
                return await ResolveSystemAccountIdAsync(AccountsPayableAccountCodeKey, "2110000000");

            return voucher.VoucherType switch
            {
                VoucherType.Receipt => await ResolveSystemAccountIdAsync(OtherReceivablesAccountCodeKey, "1170000000"),
                VoucherType.Payment => await ResolveSystemAccountIdAsync(GeneralExpenseAccountCodeKey, "5130000000"),
                _ => await ResolveSystemAccountIdAsync(GeneralExpenseAccountCodeKey, "5130000000")
            };
        }

        private static void AddDebit(
            List<JournalEntryLineWriteDto> lines,
            int accountId,
            decimal amount,
            string description,
            int? customerId = null,
            int? supplierId = null)
        {
            if (amount <= 0)
                return;

            lines.Add(new JournalEntryLineWriteDto
            {
                AccountId = accountId,
                CustomerId = customerId,
                SupplierId = supplierId,
                Debit = amount,
                Credit = 0m,
                Description = description
            });
        }

        private static void AddCredit(
            List<JournalEntryLineWriteDto> lines,
            int accountId,
            decimal amount,
            string description,
            int? customerId = null,
            int? supplierId = null)
        {
            if (amount <= 0)
                return;

            lines.Add(new JournalEntryLineWriteDto
            {
                AccountId = accountId,
                CustomerId = customerId,
                SupplierId = supplierId,
                Debit = 0m,
                Credit = amount,
                Description = description
            });
        }

        private async Task EnsureStandardDefaultAccountsAsync()
        {
            var now = GetJordanNow();
            var seeds = new[]
            {
                new { Code = "1000000000", LegacyCode = "1", ParentCode = (string?)null, NameAr = "الأصول", NameEn = "Assets", Description = "الحساب الرئيسي للأصول", AccountType = AccountType.Asset, NormalBalance = NormalBalanceType.Debit, Level = 1, IsPosting = false, AllowManualEntry = false },
                new { Code = "1100000000", LegacyCode = "11", ParentCode = (string?)"1000000000", NameAr = "الأصول المتداولة", NameEn = "Current Assets", Description = "الأصول المتداولة", AccountType = AccountType.Asset, NormalBalance = NormalBalanceType.Debit, Level = 2, IsPosting = false, AllowManualEntry = false },
                new { Code = "1110000000", LegacyCode = "1101", ParentCode = (string?)"1100000000", NameAr = "الصندوق الرئيسي", NameEn = "Main Cash", Description = "الصندوق الرئيسي للمنشأة", AccountType = AccountType.Asset, NormalBalance = NormalBalanceType.Debit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "1120000000", LegacyCode = "1102", ParentCode = (string?)"1100000000", NameAr = "صندوق نقطة البيع", NameEn = "POS Cash", Description = "صندوق نقطة البيع", AccountType = AccountType.Asset, NormalBalance = NormalBalanceType.Debit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "1130000000", LegacyCode = "1103", ParentCode = (string?)"1100000000", NameAr = "البنك", NameEn = "Bank", Description = "الحسابات البنكية", AccountType = AccountType.Asset, NormalBalance = NormalBalanceType.Debit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "1180000000", LegacyCode = "1118", ParentCode = (string?)"1100000000", NameAr = "الشيكات في اليد", NameEn = "Checks in Hand", Description = "الشيكات المحصلة قبل الإيداع", AccountType = AccountType.Asset, NormalBalance = NormalBalanceType.Debit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "1140000000", LegacyCode = "1104", ParentCode = (string?)"1100000000", NameAr = "الذمم المدينة - الزبائن", NameEn = "Accounts Receivable - Customers", Description = "ذمم الزبائن", AccountType = AccountType.Asset, NormalBalance = NormalBalanceType.Debit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "1150000000", LegacyCode = "1105", ParentCode = (string?)"1100000000", NameAr = "المخزون", NameEn = "Inventory", Description = "قيمة المخزون", AccountType = AccountType.Asset, NormalBalance = NormalBalanceType.Debit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "1160000000", LegacyCode = "1106", ParentCode = (string?)"1100000000", NameAr = "ضريبة المدخلات", NameEn = "Input Tax", Description = "ضريبة مدخلات المشتريات", AccountType = AccountType.Asset, NormalBalance = NormalBalanceType.Debit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "1170000000", LegacyCode = "1107", ParentCode = (string?)"1100000000", NameAr = "ذمم مدينة أخرى", NameEn = "Other Receivables", Description = "ذمم مدينة أخرى", AccountType = AccountType.Asset, NormalBalance = NormalBalanceType.Debit, Level = 3, IsPosting = true, AllowManualEntry = true },

                new { Code = "2000000000", LegacyCode = "2", ParentCode = (string?)null, NameAr = "الخصوم", NameEn = "Liabilities", Description = "الحساب الرئيسي للخصوم", AccountType = AccountType.Liability, NormalBalance = NormalBalanceType.Credit, Level = 1, IsPosting = false, AllowManualEntry = false },
                new { Code = "2100000000", LegacyCode = "21", ParentCode = (string?)"2000000000", NameAr = "الخصوم المتداولة", NameEn = "Current Liabilities", Description = "الخصوم المتداولة", AccountType = AccountType.Liability, NormalBalance = NormalBalanceType.Credit, Level = 2, IsPosting = false, AllowManualEntry = false },
                new { Code = "2110000000", LegacyCode = "2101", ParentCode = (string?)"2100000000", NameAr = "الذمم الدائنة - الموردين", NameEn = "Accounts Payable - Suppliers", Description = "ذمم الموردين", AccountType = AccountType.Liability, NormalBalance = NormalBalanceType.Credit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "2120000000", LegacyCode = "2102", ParentCode = (string?)"2100000000", NameAr = "ضريبة مستحقة", NameEn = "Output Tax", Description = "ضريبة مستحقة على المبيعات", AccountType = AccountType.Liability, NormalBalance = NormalBalanceType.Credit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "2130000000", LegacyCode = "2103", ParentCode = (string?)"2100000000", NameAr = "ذمم دائنة أخرى", NameEn = "Other Payables", Description = "ذمم دائنة أخرى", AccountType = AccountType.Liability, NormalBalance = NormalBalanceType.Credit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "2140000000", LegacyCode = "2104", ParentCode = (string?)"2100000000", NameAr = "شيكات صادرة مستحقة", NameEn = "Issued Checks Payable", Description = "الشيكات الصادرة التي لم تتم تصفيتها بعد", AccountType = AccountType.Liability, NormalBalance = NormalBalanceType.Credit, Level = 3, IsPosting = true, AllowManualEntry = true },

                new { Code = "3000000000", LegacyCode = "3", ParentCode = (string?)null, NameAr = "حقوق الملكية", NameEn = "Equity", Description = "الحساب الرئيسي لحقوق الملكية", AccountType = AccountType.Equity, NormalBalance = NormalBalanceType.Credit, Level = 1, IsPosting = false, AllowManualEntry = false },
                new { Code = "3100000000", LegacyCode = "31", ParentCode = (string?)"3000000000", NameAr = "حقوق الملكية", NameEn = "Owner Equity", Description = "مجموعة حقوق الملكية", AccountType = AccountType.Equity, NormalBalance = NormalBalanceType.Credit, Level = 2, IsPosting = false, AllowManualEntry = false },
                new { Code = "3110000000", LegacyCode = "3101", ParentCode = (string?)"3100000000", NameAr = "رأس المال", NameEn = "Capital", Description = "رأس مال المنشأة", AccountType = AccountType.Equity, NormalBalance = NormalBalanceType.Credit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "3120000000", LegacyCode = "3102", ParentCode = (string?)"3100000000", NameAr = "الأرباح المحتجزة", NameEn = "Retained Earnings", Description = "الأرباح المرحلة", AccountType = AccountType.Equity, NormalBalance = NormalBalanceType.Credit, Level = 3, IsPosting = true, AllowManualEntry = true },

                new { Code = "4000000000", LegacyCode = "4", ParentCode = (string?)null, NameAr = "الإيرادات", NameEn = "Revenue", Description = "الحساب الرئيسي للإيرادات", AccountType = AccountType.Revenue, NormalBalance = NormalBalanceType.Credit, Level = 1, IsPosting = false, AllowManualEntry = false },
                new { Code = "4100000000", LegacyCode = "41", ParentCode = (string?)"4000000000", NameAr = "إيرادات التشغيل", NameEn = "Operating Revenue", Description = "إيرادات النشاط", AccountType = AccountType.Revenue, NormalBalance = NormalBalanceType.Credit, Level = 2, IsPosting = false, AllowManualEntry = false },
                new { Code = "4110000000", LegacyCode = "4101", ParentCode = (string?)"4100000000", NameAr = "المبيعات", NameEn = "Sales", Description = "إيراد المبيعات", AccountType = AccountType.Revenue, NormalBalance = NormalBalanceType.Credit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "4120000000", LegacyCode = "4102", ParentCode = (string?)"4100000000", NameAr = "مردودات المبيعات", NameEn = "Sales Returns", Description = "مردودات المبيعات", AccountType = AccountType.Revenue, NormalBalance = NormalBalanceType.Debit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "4130000000", LegacyCode = "4103", ParentCode = (string?)"4100000000", NameAr = "خصومات المبيعات", NameEn = "Sales Discounts", Description = "خصومات المبيعات", AccountType = AccountType.Revenue, NormalBalance = NormalBalanceType.Debit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "4140000000", LegacyCode = "4104", ParentCode = (string?)"4100000000", NameAr = "أرباح تسويات المخزون", NameEn = "Inventory Adjustment Gains", Description = "أرباح تسويات المخزون", AccountType = AccountType.Revenue, NormalBalance = NormalBalanceType.Credit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "4150000000", LegacyCode = "4105", ParentCode = (string?)"4100000000", NameAr = "خصومات المشتريات", NameEn = "Purchase Discounts", Description = "خصومات المشتريات", AccountType = AccountType.Revenue, NormalBalance = NormalBalanceType.Credit, Level = 3, IsPosting = true, AllowManualEntry = true },

                new { Code = "5000000000", LegacyCode = "5", ParentCode = (string?)null, NameAr = "المصروفات", NameEn = "Expenses", Description = "الحساب الرئيسي للمصروفات", AccountType = AccountType.Expense, NormalBalance = NormalBalanceType.Debit, Level = 1, IsPosting = false, AllowManualEntry = false },
                new { Code = "5100000000", LegacyCode = "51", ParentCode = (string?)"5000000000", NameAr = "المصروفات التشغيلية", NameEn = "Operating Expenses", Description = "مجموعة المصروفات التشغيلية", AccountType = AccountType.Expense, NormalBalance = NormalBalanceType.Debit, Level = 2, IsPosting = false, AllowManualEntry = false },
                new { Code = "5110000000", LegacyCode = "5101", ParentCode = (string?)"5100000000", NameAr = "تكلفة البضاعة المباعة", NameEn = "Cost of Goods Sold", Description = "تكلفة البضاعة المباعة", AccountType = AccountType.Expense, NormalBalance = NormalBalanceType.Debit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "5120000000", LegacyCode = "5102", ParentCode = (string?)"5100000000", NameAr = "خسائر التالف", NameEn = "Damaged Stock Loss", Description = "خسائر التالف والمخزون الهالك", AccountType = AccountType.Expense, NormalBalance = NormalBalanceType.Debit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "5130000000", LegacyCode = "6101", ParentCode = (string?)"5100000000", NameAr = "المصروفات العامة", NameEn = "General Expenses", Description = "المصروفات العامة", AccountType = AccountType.Expense, NormalBalance = NormalBalanceType.Debit, Level = 3, IsPosting = true, AllowManualEntry = true },
                new { Code = "5140000000", LegacyCode = "6102", ParentCode = (string?)"5100000000", NameAr = "استهلاك داخلي", NameEn = "Internal Consumption", Description = "استهلاك داخلي للمخزون", AccountType = AccountType.Expense, NormalBalance = NormalBalanceType.Debit, Level = 3, IsPosting = true, AllowManualEntry = true }
            };

            var existingAccounts = await _context.Accounts.ToListAsync();

            Account? FindMatchingAccount(string code)
            {
                return existingAccounts.FirstOrDefault(x =>
                    string.Equals(NormalizeAccountCode(x.Code), code, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(NormalizeAccountCode(x.AccountCode), code, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var seed in seeds)
            {
                var account = FindMatchingAccount(seed.Code);

                if (account == null)
                {
                    account = new Account();
                    _context.Accounts.Add(account);
                    existingAccounts.Add(account);
                }

                account.Code = seed.Code;
                account.AccountCode = seed.Code;
                account.AccountLevel = seed.Level;
                account.Level = seed.Level;
                account.AccountType = seed.AccountType;
                account.NormalBalanceType = seed.NormalBalance;
                account.IsPosting = seed.IsPosting;
                account.IsActive = true;
                account.AllowManualEntry = seed.AllowManualEntry;
                account.Name = seed.NameAr;
                account.ArabicName = seed.NameAr;
                account.EnglishName = seed.NameEn;
                account.Description = seed.Description;
                account.AccountNature = seed.NormalBalance == NormalBalanceType.Debit ? "Debit" : "Credit";
                account.AccountCategory = seed.AccountType.ToString();
                account.AccountTypeCode = seed.AccountType is AccountType.Revenue or AccountType.Expense ? "PL" : "BS";
            }

            foreach (var seed in seeds)
            {
                var account = FindMatchingAccount(seed.Code);
                if (account == null)
                    continue;

                account.ParentAccount = seed.ParentCode is null
                    ? null
                    : FindMatchingAccount(seed.ParentCode);
            }

            await EnsureDefaultAccountSettingsAsync(now);
            await _context.SaveChangesAsync();
        }

        private sealed class AccountScopeNode
        {
            public int Id { get; set; }
            public int? ParentAccountId { get; set; }
        }

        private static HashSet<int> ResolveAccountScopeIds(int rootId, List<AccountScopeNode> nodes)
        {
            var lookup = nodes
                .Where(x => x.ParentAccountId.HasValue)
                .GroupBy(x => x.ParentAccountId!.Value)
                .ToDictionary(x => x.Key, x => x.Select(n => n.Id).ToList());

            var result = new HashSet<int>();

            void Visit(int id)
            {
                if (!result.Add(id))
                    return;

                if (lookup.TryGetValue(id, out var children))
                {
                    foreach (var childId in children)
                    {
                        Visit(childId);
                    }
                }
            }

            Visit(rootId);
            return result;
        }

        private static DateTime GetJordanNow()
        {
            var jordanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, jordanTimeZone);
        }

        private async Task CleanupLegacyAccountChartAsync()
        {
            var allAccounts = await _context.Accounts
                .AsNoTracking()
                .Select(x => new { x.Id, x.Code, x.AccountCode, x.ParentAccountId })
                .ToListAsync();

            var legacyAccountsExist = allAccounts.Any(x => !AccountCodeHelper.IsFlatAccountCode(x.Code));

            if (!legacyAccountsExist)
            {
                return;
            }

            var newAccountIdsByCode = allAccounts
                .Where(x => AccountCodeHelper.IsFlatAccountCode(x.Code))
                .ToDictionary(x => x.Code, x => x.Id, StringComparer.OrdinalIgnoreCase);

            foreach (var (legacyCode, newCode) in LegacyAccountCodeMap)
            {
                if (!newAccountIdsByCode.TryGetValue(newCode, out var newAccountId))
                {
                    continue;
                }

                var legacyAccountIds = allAccounts
                    .Where(x => string.Equals(x.Code, legacyCode, StringComparison.OrdinalIgnoreCase)
                             || string.Equals(x.AccountCode, legacyCode, StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.Id)
                    .ToList();

                foreach (var legacyAccountId in legacyAccountIds)
                {
                    await _context.JournalEntryLines
                        .Where(x => x.AccountId == legacyAccountId)
                        .ExecuteUpdateAsync(s => s.SetProperty(x => x.AccountId, newAccountId));

                    await _context.AccountOpeningBalances
                        .Where(x => x.AccountId == legacyAccountId)
                        .ExecuteUpdateAsync(s => s.SetProperty(x => x.AccountId, newAccountId));

                    await _context.RecurringJournalLines
                        .Where(x => x.AccountId == legacyAccountId)
                        .ExecuteUpdateAsync(s => s.SetProperty(x => x.AccountId, newAccountId));

                    await _context.BankAccounts
                        .Where(x => x.GlAccountId == legacyAccountId)
                        .ExecuteUpdateAsync(s => s.SetProperty(x => x.GlAccountId, newAccountId));

                    await _context.TaxRates
                        .Where(x => x.TaxAccountId == legacyAccountId)
                        .ExecuteUpdateAsync(s => s.SetProperty(x => x.TaxAccountId, newAccountId));
                }
            }

            var legacyIds = allAccounts
                .Where(x => !AccountCodeHelper.IsFlatAccountCode(x.Code))
                .Select(x => x.Id)
                .ToList();

            if (legacyIds.Count > 0)
            {
                await _context.Accounts
                    .Where(x => legacyIds.Contains(x.Id))
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.ParentAccountId, (int?)null));

                await _context.Accounts
                    .Where(x => legacyIds.Contains(x.Id))
                    .ExecuteDeleteAsync();
            }
        }

        private static readonly Dictionary<string, string> LegacyAccountCodeMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["1"] = "1000000000",
            ["11"] = "1100000000",
            ["1101"] = "1110000000",
            ["1102"] = "1120000000",
            ["1103"] = "1130000000",
            ["1104"] = "1140000000",
            ["1105"] = "1150000000",
            ["1106"] = "1160000000",
            ["1107"] = "1170000000",
            ["2"] = "2000000000",
            ["21"] = "2100000000",
            ["2101"] = "2110000000",
            ["2102"] = "2120000000",
            ["2103"] = "2130000000",
            ["3"] = "3000000000",
            ["31"] = "3100000000",
            ["3101"] = "3110000000",
            ["3102"] = "3120000000",
            ["4"] = "4000000000",
            ["41"] = "4100000000",
            ["4101"] = "4110000000",
            ["4102"] = "4120000000",
            ["4103"] = "4130000000",
            ["4104"] = "4140000000",
            ["5"] = "5000000000",
            ["51"] = "5100000000",
            ["5101"] = "5110000000",
            ["5102"] = "5120000000",
            ["6101"] = "5130000000",
            ["6102"] = "5140000000",
            ["0000000001"] = "1000000000",
            ["0000000001.0000000001"] = "1100000000",
            ["0000000001.0000000001.0000000001"] = "1110000000",
            ["0000000001.0000000001.0000000002"] = "1120000000",
            ["0000000001.0000000001.0000000003"] = "1130000000",
            ["0000000001.0000000001.0000000004"] = "1140000000",
            ["0000000001.0000000001.0000000005"] = "1150000000",
            ["0000000001.0000000001.0000000006"] = "1160000000",
            ["0000000001.0000000001.0000000007"] = "1170000000",
            ["0000000002"] = "2000000000",
            ["0000000002.0000000001"] = "2100000000",
            ["0000000002.0000000001.0000000001"] = "2110000000",
            ["0000000002.0000000001.0000000002"] = "2120000000",
            ["0000000002.0000000001.0000000003"] = "2130000000",
            ["0000000003"] = "3000000000",
            ["0000000003.0000000001"] = "3100000000",
            ["0000000003.0000000001.0000000001"] = "3110000000",
            ["0000000003.0000000001.0000000002"] = "3120000000",
            ["0000000004"] = "4000000000",
            ["0000000004.0000000001"] = "4100000000",
            ["0000000004.0000000001.0000000001"] = "4110000000",
            ["0000000004.0000000001.0000000002"] = "4120000000",
            ["0000000004.0000000001.0000000003"] = "4130000000",
            ["0000000004.0000000001.0000000004"] = "4140000000",
            ["0000000005"] = "5000000000",
            ["0000000005.0000000001"] = "5100000000",
            ["0000000005.0000000001.0000000001"] = "5110000000",
            ["0000000005.0000000001.0000000002"] = "5120000000",
            ["0000000005.0000000001.0000000003"] = "5130000000",
            ["0000000005.0000000001.0000000004"] = "5140000000"
        };
    }

    public interface IAccountingService
    {
        Task<Result<AccountWriteDto>> CreateAccountAsync(AccountWriteDto dto);
        Task<Result<List<AccountReadDto>>> GetAccountsAsync(bool activeOnly = true);
        Task<Result<JournalEntryReadDto>> PostJournalEntryAsync(JournalEntryWriteDto dto);
        Task<DateTime?> GetPostingLockDateAsync();
        Task<Result<DateTime?>> SetPostingLockDateAsync(DateTime? lockDate);
        Task EnsureDefaultAccountsAsync();
        Task<Result<JournalEntryReadDto>> PostInvoiceEntryAsync(InvoiceWriteDto invoice);
        Task<Result<JournalEntryReadDto>> PostVoucherEntryAsync(VoucherWriteDto voucher);
        Task<Result<JournalEntryReadDto>> PostStockDocumentEntryAsync(StockDocumentWriteDto document);
        Task<Result<JournalEntryReadDto>> PostFinancialTransactionEntryAsync(FinancialPostDto transaction, int persistedTransactionId);
        Task<Result<JournalEntryReadDto>> PostStockAdjustmentEntryAsync(StockAdjustmentWriteDto adjustment);
        Task<Result<(TrialBalanceSummaryDto summary, List<TrialBalanceRowDto> rows)>> GetTrialBalanceAsync(TrialBalanceFilterDto filter);
        Task<Result<List<GeneralLedgerAccountDto>>> GetGeneralLedgerAsync(GeneralLedgerFilterDto filter);
        Task<Result<BalanceSheetDto>> GetBalanceSheetAsync(BalanceSheetFilterDto filter);
        Task<Result<List<JournalEntryReadDto>>> GetJournalEntriesAsync(JournalEntryFilterDto filter);
        Task<Result<JournalEntryReadDto>> ReverseJournalEntryAsync(int journalEntryId, string reason);
        Task<Result<JournalEntryReadDto>> ReverseJournalByReferenceAsync(string referenceType, int referenceId, string reason);
    }
}
