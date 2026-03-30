using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.Accounts;
using RaccoonWarehouse.Domain.Accounting.Accounts.DTOs;
using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Accounting.JournalEntries;
using RaccoonWarehouse.Domain.Accounting.JournalEntries.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.FinancialTransactions.DTOs;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Reports.Accounting.Dtos;
using RaccoonWarehouse.Domain.Reports.Accounting.Filters;
using RaccoonWarehouse.Domain.Settings;
using RaccoonWarehouse.Domain.StockAdjustments.DTOs;

namespace RaccoonWarehouse.Application.Service.Accounting
{
    public class AccountingService : IAccountingService
    {
        public const string PostingLockDateKey = "AccountingPostingLockDate";

        private readonly ApplicationDbContext _context;
        private readonly IUOW _uow;
        private readonly IMapper _mapper;

        public AccountingService(ApplicationDbContext context, IUOW uow, IMapper mapper)
        {
            _context = context;
            _uow = uow;
            _mapper = mapper;
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

            var entry = new JournalEntry
            {
                EntryNumber = string.IsNullOrWhiteSpace(dto.EntryNumber) ? GenerateEntryNumber(now) : dto.EntryNumber,
                EntryDate = entryDate,
                Description = dto.Description,
                Status = JournalEntryStatus.Posted,
                ReferenceType = dto.ReferenceType,
                ReferenceId = dto.ReferenceId,
                CreatedDate = now,
                UpdatedDate = now,
                Lines = effectiveLines.Select(line => new JournalEntryLine
                {
                    AccountId = line.AccountId,
                    Debit = line.Debit,
                    Credit = line.Credit,
                    Description = line.Description,
                    CreatedDate = now,
                    UpdatedDate = now
                }).ToList()
            };

            await _uow.JournalEntries.AddAsync(entry);
            await _uow.CommitAsync();

            var savedEntry = await _uow.JournalEntries.GetAllAsQueryable()
                .Include(x => x.Lines)
                .ThenInclude(x => x.Account)
                .AsNoTracking()
                .FirstAsync(x => x.Id == entry.Id);

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
            var now = GetJordanNow();
            var defaults = new[]
            {
                new Account { Code = "1000", Name = "الصندوق", AccountType = AccountType.Asset, IsPosting = true, IsActive = true, CreatedDate = now, UpdatedDate = now },
                new Account { Code = "1100", Name = "البنك", AccountType = AccountType.Asset, IsPosting = true, IsActive = true, CreatedDate = now, UpdatedDate = now },
                new Account { Code = "1200", Name = "العملاء", AccountType = AccountType.Asset, IsPosting = true, IsActive = true, CreatedDate = now, UpdatedDate = now },
                new Account { Code = "1210", Name = "ضريبة المدخلات", AccountType = AccountType.Asset, IsPosting = true, IsActive = true, CreatedDate = now, UpdatedDate = now },
                new Account { Code = "1300", Name = "المخزون", AccountType = AccountType.Asset, IsPosting = true, IsActive = true, CreatedDate = now, UpdatedDate = now },
                new Account { Code = "1400", Name = "ذمم مدينة أخرى", AccountType = AccountType.Asset, IsPosting = true, IsActive = true, CreatedDate = now, UpdatedDate = now },
                new Account { Code = "2000", Name = "الموردون", AccountType = AccountType.Liability, IsPosting = true, IsActive = true, CreatedDate = now, UpdatedDate = now },
                new Account { Code = "2100", Name = "ضريبة مستحقة", AccountType = AccountType.Liability, IsPosting = true, IsActive = true, CreatedDate = now, UpdatedDate = now },
                new Account { Code = "2200", Name = "ذمم دائنة أخرى", AccountType = AccountType.Liability, IsPosting = true, IsActive = true, CreatedDate = now, UpdatedDate = now },
                new Account { Code = "3000", Name = "حقوق الملكية", AccountType = AccountType.Equity, IsPosting = true, IsActive = true, CreatedDate = now, UpdatedDate = now },
                new Account { Code = "3100", Name = "الأرباح المحتجزة", AccountType = AccountType.Equity, IsPosting = true, IsActive = true, CreatedDate = now, UpdatedDate = now },
                new Account { Code = "4000", Name = "إيرادات المبيعات", AccountType = AccountType.Revenue, IsPosting = true, IsActive = true, CreatedDate = now, UpdatedDate = now },
                new Account { Code = "4100", Name = "مردودات المبيعات", AccountType = AccountType.Revenue, IsPosting = true, IsActive = true, CreatedDate = now, UpdatedDate = now },
                new Account { Code = "4200", Name = "خصومات المبيعات", AccountType = AccountType.Revenue, IsPosting = true, IsActive = true, CreatedDate = now, UpdatedDate = now },
                new Account { Code = "4300", Name = "أرباح تسويات المخزون", AccountType = AccountType.Revenue, IsPosting = true, IsActive = true, CreatedDate = now, UpdatedDate = now },
                new Account { Code = "5000", Name = "تكلفة البضاعة المباعة", AccountType = AccountType.Expense, IsPosting = true, IsActive = true, CreatedDate = now, UpdatedDate = now },
                new Account { Code = "6000", Name = "المصاريف التشغيلية", AccountType = AccountType.Expense, IsPosting = true, IsActive = true, CreatedDate = now, UpdatedDate = now },
                new Account { Code = "6100", Name = "خسائر تسويات المخزون", AccountType = AccountType.Expense, IsPosting = true, IsActive = true, CreatedDate = now, UpdatedDate = now }
            };

            var existingCodes = await _uow.Accounts.GetAllAsQueryable()
                .Select(x => x.Code)
                .ToListAsync();

            foreach (var account in defaults)
            {
                if (existingCodes.Contains(account.Code))
                {
                    continue;
                }

                await _uow.Accounts.AddAsync(account);
            }

            await _uow.CommitAsync();
        }

        public async Task<Result<JournalEntryReadDto>> PostInvoiceEntryAsync(InvoiceWriteDto invoice)
        {
            if (invoice.Id <= 0)
                return Result<JournalEntryReadDto>.Fail("Invoice id is required.");

            if (invoice.Status is InvoiceStatus.OnHold or InvoiceStatus.Draft or InvoiceStatus.Cancelled)
                return Result<JournalEntryReadDto>.Ok(new JournalEntryReadDto(), "Invoice is not in a postable status.");

            if (await TryGetExistingEntryAsync("Invoice", invoice.Id) is { } existing)
                return Result<JournalEntryReadDto>.Ok(existing, "Journal entry already exists for this invoice.");

            var lines = new List<JournalEntryLineWriteDto>();
            var entryDate = invoice.CreatedDate == default ? GetJordanNow() : invoice.CreatedDate;
            var settlementAccountId = await ResolveSettlementAccountIdAsync(invoice.PaymentType, invoice.InvoiceType is InvoiceType.Purchase or InvoiceType.PurchaseReturn);
            var salesRevenueId = await GetAccountIdByCodeAsync("4000");
            var salesReturnsId = await GetAccountIdByCodeAsync("4100");
            var salesDiscountId = await GetAccountIdByCodeAsync("4200");
            var inventoryId = await GetAccountIdByCodeAsync("1300");
            var cogsId = await GetAccountIdByCodeAsync("5000");
            var outputTaxId = await GetAccountIdByCodeAsync("2100");
            var inputTaxId = await GetAccountIdByCodeAsync("1210");

            switch (invoice.InvoiceType)
            {
                case InvoiceType.Sale:
                    AddDebit(lines, settlementAccountId, invoice.TotalAmount, $"Invoice #{invoice.InvoiceNumber} settlement");
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
                    AddDebit(lines, salesReturnsId, invoice.NetSales, $"Sales return #{invoice.InvoiceNumber}");
                    if (invoice.TotalTax > 0)
                        AddDebit(lines, outputTaxId, invoice.TotalTax, $"Sales return #{invoice.InvoiceNumber} tax reversal");
                    AddCredit(lines, settlementAccountId, invoice.TotalAmount, $"Sales return #{invoice.InvoiceNumber} settlement");
                    if (invoice.TotalCOGS > 0)
                    {
                        AddDebit(lines, inventoryId, invoice.TotalCOGS, $"Sales return #{invoice.InvoiceNumber} inventory recovery");
                        AddCredit(lines, cogsId, invoice.TotalCOGS, $"Sales return #{invoice.InvoiceNumber} cost reversal");
                    }
                    break;

                case InvoiceType.Purchase:
                    AddDebit(lines, inventoryId, invoice.NetSales, $"Purchase invoice #{invoice.InvoiceNumber} inventory");
                    if (invoice.TotalTax > 0)
                        AddDebit(lines, inputTaxId, invoice.TotalTax, $"Purchase invoice #{invoice.InvoiceNumber} input tax");
                    AddCredit(lines, settlementAccountId, invoice.TotalAmount, $"Purchase invoice #{invoice.InvoiceNumber} settlement");
                    break;

                case InvoiceType.PurchaseReturn:
                    AddDebit(lines, settlementAccountId, invoice.TotalAmount, $"Purchase return #{invoice.InvoiceNumber} settlement");
                    if (invoice.TotalTax > 0)
                        AddCredit(lines, inputTaxId, invoice.TotalTax, $"Purchase return #{invoice.InvoiceNumber} input tax reversal");
                    AddCredit(lines, inventoryId, invoice.NetSales, $"Purchase return #{invoice.InvoiceNumber} inventory reversal");
                    break;

                default:
                    return Result<JournalEntryReadDto>.Ok(new JournalEntryReadDto(), "Invoice type does not create an accounting entry.");
            }

            return await PostJournalEntryAsync(new JournalEntryWriteDto
            {
                EntryDate = entryDate,
                Description = BuildInvoiceDescription(invoice),
                ReferenceType = "Invoice",
                ReferenceId = invoice.Id,
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

            var inventoryId = await GetAccountIdByCodeAsync("1300");
            var stockGainId = await GetAccountIdByCodeAsync("4300");
            var stockLossId = await GetAccountIdByCodeAsync("6100");
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

            var accountsQuery = _uow.Accounts.GetAllAsQueryable()
                .AsNoTracking()
                .OrderBy(x => x.Code);

            if (filter.AccountId.HasValue)
            {
                accountsQuery = accountsQuery.Where(x => x.Id == filter.AccountId.Value)
                    .OrderBy(x => x.Code);
            }

            var accounts = await accountsQuery.ToListAsync();
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
                        Description = string.IsNullOrWhiteSpace(line.LineDescription) ? line.EntryDescription : line.LineDescription!,
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
            });

            var dto = new BalanceSheetDto
            {
                AsOfDate = filter.AsOfDate,
                Assets = BuildBalanceSheetSection("الأصول", balances, AccountType.Asset, filter.IncludeZeroBalances, false),
                Liabilities = BuildBalanceSheetSection("الالتزامات", balances, AccountType.Liability, filter.IncludeZeroBalances, true),
                Equity = BuildBalanceSheetSection("حقوق الملكية", balances, AccountType.Equity, filter.IncludeZeroBalances, true)
            };

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
                    Debit = line.Credit,
                    Credit = line.Debit,
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
                .FirstOrDefaultAsync(x => x.ReferenceType == referenceType && x.ReferenceId == referenceId);

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
                return await GetAccountIdByCodeAsync(isPurchaseSide ? "2000" : "1200");

            return await ResolveCashLikeAccountIdAsync(MapPaymentTypeToMethod(paymentType));
        }

        private async Task<int> ResolveCashLikeAccountIdAsync(PaymentMethod? method)
        {
            return method switch
            {
                PaymentMethod.Cash => await GetAccountIdByCodeAsync("1000"),
                PaymentMethod.Credit => await GetAccountIdByCodeAsync("1200"),
                _ => await GetAccountIdByCodeAsync("1100")
            };
        }

        private async Task<int> ResolveFinancialCounterpartAccountIdAsync(FinancialPostDto transaction)
        {
            return transaction.SourceType switch
            {
                FinancialSourceType.ReceiptVoucher => await GetAccountIdByCodeAsync("1200"),
                FinancialSourceType.PaymentVoucher => await GetAccountIdByCodeAsync("2000"),
                FinancialSourceType.Expense => await GetAccountIdByCodeAsync("6000"),
                FinancialSourceType.SessionOpening => await GetAccountIdByCodeAsync("2200"),
                FinancialSourceType.SessionClosing => await GetAccountIdByCodeAsync("2200"),
                _ when transaction.Direction == TransactionDirection.In => await GetAccountIdByCodeAsync("1400"),
                _ => await GetAccountIdByCodeAsync("6000")
            };
        }

        private static bool ShouldSkipFinancialJournal(FinancialSourceType sourceType)
        {
            return sourceType is FinancialSourceType.SaleInvoice
                or FinancialSourceType.PosSaleInvoice
                or FinancialSourceType.PurchaseInvoice
                or FinancialSourceType.SaleReturn
                or FinancialSourceType.PurchaseReturn;
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
            var accountId = await _uow.Accounts.GetAllAsQueryable()
                .Where(x => x.Code == code && x.IsActive && x.IsPosting)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            if (accountId == 0)
                throw new InvalidOperationException($"Accounting setup is incomplete. Account code '{code}' is missing.");

            return accountId;
        }

        private static void AddDebit(List<JournalEntryLineWriteDto> lines, int accountId, decimal amount, string description)
        {
            if (amount <= 0)
                return;

            lines.Add(new JournalEntryLineWriteDto
            {
                AccountId = accountId,
                Debit = amount,
                Credit = 0m,
                Description = description
            });
        }

        private static void AddCredit(List<JournalEntryLineWriteDto> lines, int accountId, decimal amount, string description)
        {
            if (amount <= 0)
                return;

            lines.Add(new JournalEntryLineWriteDto
            {
                AccountId = accountId,
                Debit = 0m,
                Credit = amount,
                Description = description
            });
        }

        private static DateTime GetJordanNow()
        {
            var jordanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, jordanTimeZone);
        }
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
