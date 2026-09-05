using AutoMapper;
using RaccoonWarehouse.Domain.Accounting.Accounts;
using RaccoonWarehouse.Domain.Accounting.Accounts.DTOs;
using RaccoonWarehouse.Domain.Accounting.JournalEntries;
using RaccoonWarehouse.Domain.Accounting.JournalEntries.DTOs;
using RaccoonWarehouse.Domain.Brands;
using RaccoonWarehouse.Domain.Brands.DTOs;
using RaccoonWarehouse.Domain.Cashiers;
using RaccoonWarehouse.Domain.Cashiers.DTOs;
using RaccoonWarehouse.Domain.Categories;
using RaccoonWarehouse.Domain.Categories.DTOs;
using RaccoonWarehouse.Domain.Checks;
using RaccoonWarehouse.Domain.Checks.DTOs;
using RaccoonWarehouse.Domain.Delegates.DTOs;
using RaccoonWarehouse.Domain.Employees.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.FinancialTransactions;
using RaccoonWarehouse.Domain.FinancialTransactions.DTOs;
using RaccoonWarehouse.Domain.InvoiceLines;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Permissions;
using RaccoonWarehouse.Domain.Permissions.DTOs;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using RaccoonWarehouse.Domain.Stock;
using RaccoonWarehouse.Domain.Stock.DTOs;
using RaccoonWarehouse.Domain.StockDocuments;
using RaccoonWarehouse.Domain.StockDocuments.DTOs;
using RaccoonWarehouse.Domain.StockAdjustments;
using RaccoonWarehouse.Domain.StockAdjustments.DTOs;
using RaccoonWarehouse.Domain.StockItems;
using RaccoonWarehouse.Domain.StockItems.DTOs;
using RaccoonWarehouse.Domain.StockTransactions;
using RaccoonWarehouse.Domain.StockTransactions.DTOs;
using RaccoonWarehouse.Domain.SubCategories;
using RaccoonWarehouse.Domain.SubCategories.DTOs;
using RaccoonWarehouse.Domain.Units;
using RaccoonWarehouse.Domain.Units.DTOs;
using RaccoonWarehouse.Domain.Users;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Domain.Vouchers;
using RaccoonWarehouse.Domain.Vouchers.DTOs;
using RaccoonWarehouse.Domain.Warehouses;
using RaccoonWarehouse.Domain.Warehouses.DTOs;
using DelegateEntity = RaccoonWarehouse.Domain.Delegates.Delegate;
using EmployeeEntity = RaccoonWarehouse.Domain.Employees.Employee;

namespace RaccoonWarehouse.Application.Helper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<UserWriteDto, User>().ReverseMap();
            CreateMap<User, UserReadDto>().ReverseMap();

            CreateMap<DelegateCreateDto, DelegateEntity>().ReverseMap();
            CreateMap<DelegateUpdateDto, DelegateEntity>().ReverseMap();
            CreateMap<DelegateEntity, DelegateReadDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.Name : null))
                .ForMember(dest => dest.InvoiceCount, opt => opt.MapFrom(src => src.Invoices.Count))
                .ForMember(dest => dest.TotalSales, opt => opt.MapFrom(src => src.Invoices.Sum(i => i.TotalAmount)))
                .ReverseMap();

            CreateMap<EmployeeCreateDto, EmployeeEntity>().ReverseMap();
            CreateMap<EmployeeUpdateDto, EmployeeEntity>().ReverseMap();
            CreateMap<EmployeeEntity, EmployeeReadDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.Name : null))
                .ForMember(dest => dest.ManagerName, opt => opt.MapFrom(src => src.Manager != null ? src.Manager.FullName : null))
                .ReverseMap();

            CreateMap<CategoryWriteDto, Category>().ReverseMap();
            CreateMap<Category, CategoryReadDto>().ReverseMap();

            CreateMap<SubCategoryWriteDto, SubCategory>().ReverseMap();
            CreateMap<SubCategory, SubCategoryReadDto>().ReverseMap();
            CreateMap<SubCategoryReadDto, SubCategoryWriteDto>().ReverseMap();

            CreateMap<ProductWriteDto, Product>().ReverseMap();
            CreateMap<Product, ProductReadDto>().ReverseMap();

            CreateMap<ProductUnitWriteDto, ProductUnit>().ReverseMap();
            CreateMap<ProductUnit, ProductUnitReadDto>().ReverseMap();
            CreateMap<ProductUnit, ProductUnitWriteDto>().ReverseMap();

            CreateMap<UnitWriteDto, Unit>().ReverseMap();
            CreateMap<Unit, UnitReadDto>().ReverseMap();

            CreateMap<WarehouseWriteDto, Warehouse>().ReverseMap();
            CreateMap<Warehouse, WarehouseReadDto>().ReverseMap();

            CreateMap<StockWriteDto, Stock>().ReverseMap();
            CreateMap<Stock, StockReadDto>().ReverseMap();
            CreateMap<StockAdjustmentWriteDto, StockAdjustment>().ReverseMap();
            CreateMap<StockAdjustment, StockAdjustmentReadDto>().ReverseMap();

            CreateMap<VoucherWriteDto, Voucher>().ReverseMap();
            CreateMap<Voucher, VoucherReadDto>()
                .ForMember(dest => dest.PaymentType,
                    opt => opt.MapFrom(src =>
                        Enum.IsDefined(typeof(PaymentType), src.PaymentType)
                            ? src.PaymentType
                            : PaymentType.Cash));

            CreateMap<InvoiceWriteDto, Invoice>()
                .ForMember(d => d.InvoiceLines, opt => opt.Ignore())
                .ForMember(d => d.Payments, opt => opt.Ignore());
            CreateMap<Invoice, InvoiceWriteDto>();
            CreateMap<Invoice, InvoiceReadDto>()
                .ForMember(dest => dest.DelegateName, opt => opt.MapFrom(src => src.Delegate != null ? src.Delegate.FullName : null))
                .ReverseMap();
            CreateMap<InvoicePaymentWriteDto, InvoicePayment>()
                .ForMember(d => d.Invoice, opt => opt.Ignore())
                .ForMember(d => d.InvoiceId, opt => opt.Ignore());
            CreateMap<InvoicePayment, InvoicePaymentWriteDto>();
            CreateMap<InvoicePayment, InvoicePaymentReadDto>();

            CreateMap<InvoiceLineWriteDto, InvoiceLine>()
                .ForMember(d => d.InvoiceId, opt => opt.Ignore())
                .ForMember(d => d.Invoice, opt => opt.Ignore());
            CreateMap<InvoiceLine, InvoiceLineWriteDto>()
                .ForMember(d => d.Invoice, opt => opt.Ignore())
                .ForMember(d => d.Product, opt => opt.Ignore())
                .ForMember(d => d.ProductUnit, opt => opt.Ignore())
                .ForMember(d => d.SelectedProduct, opt => opt.Ignore());
            CreateMap<InvoiceLine, InvoiceLineReadDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name))
                .ReverseMap();

            CreateMap<BrandWriteDto, Brand>().ReverseMap();
            CreateMap<Brand, BrandReadDto>().ReverseMap();

            CreateMap<StockDocumentWriteDto, StockDocument>().ReverseMap();
            CreateMap<StockDocument, StockDocumentReadDto>().ReverseMap();

            CreateMap<StockItemWriteDto, StockItem>()
                .ForMember(x => x.Product, opt => opt.Ignore())
                .ForMember(x => x.ProductUnit, opt => opt.Ignore())
                .ForMember(x => x.StockDocument, opt => opt.Ignore())
                .ReverseMap();
            CreateMap<StockItem, StockItemReadDto>().ReverseMap();

            CreateMap<CheckWriteDto, Check>().ReverseMap();
            CreateMap<Check, CheckReadDto>().ReverseMap();

            CreateMap<StockTransactionWriteDto, StockTransaction>().ReverseMap();
            CreateMap<StockTransaction, StockTransactionReadDto>().ReverseMap();

            CreateMap<FinancialTransactionWriteDto, FinancialTransaction>().ReverseMap();
            CreateMap<FinancialTransaction, FinancialTransactionReadDto>().ReverseMap();
            CreateMap<FinancialPostDto, FinancialTransaction>().ReverseMap();

            CreateMap<CashierSessionWriteDto, CashierSession>().ReverseMap();
            CreateMap<CashierSession, CashierSessionReadDto>().ReverseMap();

            CreateMap<AccountWriteDto, Account>().ReverseMap();
            CreateMap<Account, AccountReadDto>()
                .ForMember(dest => dest.ParentAccountName, opt => opt.MapFrom(src => src.ParentAccount != null ? src.ParentAccount.Name : null))
                .ReverseMap();

            CreateMap<JournalEntryWriteDto, JournalEntry>()
                .ForMember(dest => dest.Lines, opt => opt.Ignore())
                .ReverseMap();
            CreateMap<JournalEntry, JournalEntryReadDto>()
                .ForMember(dest => dest.TotalDebit, opt => opt.MapFrom(src => src.Lines.Sum(x => x.Debit)))
                .ForMember(dest => dest.TotalCredit, opt => opt.MapFrom(src => src.Lines.Sum(x => x.Credit)))
                .ReverseMap();
            CreateMap<JournalEntryLineWriteDto, JournalEntryLine>().ReverseMap();
            CreateMap<JournalEntryLine, JournalEntryLineReadDto>()
                .ForMember(dest => dest.AccountCode, opt => opt.MapFrom(src => src.Account.Code))
                .ForMember(dest => dest.AccountName, opt => opt.MapFrom(src => src.Account.Name))
                .ReverseMap();

            CreateMap<ReportPermissionWriteDto, ReportPermission>().ReverseMap();
            CreateMap<ReportPermission, ReportPermissionReadDto>().ReverseMap();
            CreateMap<RolePermissionWriteDto, RolePermission>().ReverseMap();
            CreateMap<RolePermission, RolePermissionReadDto>().ReverseMap();
        }
    }
}
