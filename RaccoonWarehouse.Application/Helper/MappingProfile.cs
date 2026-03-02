using AutoMapper;
using RaccoonWarehouse.Domain.Brands;
using RaccoonWarehouse.Domain.Brands.DTOs;
using RaccoonWarehouse.Domain.Cashiers;
using RaccoonWarehouse.Domain.Cashiers.DTOs;
using RaccoonWarehouse.Domain.Categories;
using RaccoonWarehouse.Domain.Categories.DTOs;
using RaccoonWarehouse.Domain.Checks;
using RaccoonWarehouse.Domain.Checks.DTOs;
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
using RaccoonWarehouse.Domain.StockItems;
using RaccoonWarehouse.Domain.StockItems.DTOs;
using RaccoonWarehouse.Domain.StockTransactions;
using RaccoonWarehouse.Domain.StockTransactions.DTOs;



/*using RaccoonWarehouse.Domain.StockVouchers;
using RaccoonWarehouse.Domain.StockVouchers.DTOs;*/
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace RaccoonWarehouse.Application.Helper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<UserWriteDto, User>().ReverseMap();
            CreateMap<User, UserReadDto>().ReverseMap();

            CreateMap<CategoryWriteDto, Category>().ReverseMap();
            CreateMap<Category, CategoryReadDto>().ReverseMap();

            CreateMap<SubCategoryWriteDto, SubCategory>().ReverseMap();
            CreateMap<SubCategory, SubCategoryReadDto>().ReverseMap();
            CreateMap<SubCategoryReadDto, SubCategoryWriteDto>().ReverseMap();

            CreateMap<ProductWriteDto, Product>().ReverseMap();
            CreateMap<Product, ProductReadDto>().ReverseMap();

            // If ProductUnit has nested Product or Unit, map them
            CreateMap<ProductUnitWriteDto, ProductUnit>()
                   .ReverseMap();
            CreateMap<ProductUnit, ProductUnitReadDto>().ReverseMap();

            CreateMap<ProductUnit, ProductUnitWriteDto>()
                   .ReverseMap();

            CreateMap<UnitWriteDto, Unit>().ReverseMap();
            CreateMap<Unit, UnitReadDto>().ReverseMap();

            CreateMap<WarehouseWriteDto, Warehouse>().ReverseMap();
            CreateMap<Warehouse, WarehouseReadDto>().ReverseMap();

            CreateMap<StockWriteDto, Stock>().ReverseMap();
            CreateMap<Stock, StockReadDto>().ReverseMap();
        
          
            CreateMap<VoucherWriteDto, Voucher>().ReverseMap();
            CreateMap<Voucher, VoucherReadDto>()
                .ForMember(dest => dest.PaymentType,
                           opt => opt.MapFrom(src =>
                               Enum.IsDefined(typeof(PaymentType), src.PaymentType)
                               ? src.PaymentType
                   : PaymentType.Cash));


            CreateMap<InvoiceWriteDto, Invoice>().ForMember(d => d.InvoiceLines, opt => opt.Ignore()); // ✅ نخليها يدوي.ReverseMap();
            CreateMap<Invoice, InvoiceReadDto>()
                .ReverseMap();
           
            
            CreateMap<InvoiceLineWriteDto, InvoiceLine>()
                .ForMember(d => d.InvoiceId, opt => opt.Ignore()) // ✅ مهم
                .ForMember(d => d.Invoice, opt => opt.Ignore()); // ✅ مهم
            CreateMap<InvoiceLine, InvoiceLineReadDto>().ForMember(dest => dest.ProductName,
               opt => opt.MapFrom(src => src.Product.Name)).ReverseMap();

            CreateMap<BrandWriteDto, Brand>().ReverseMap();
            CreateMap<Brand, BrandReadDto>().ReverseMap();

            CreateMap<UnitWriteDto, Unit>().ReverseMap();
            CreateMap<Unit, UnitReadDto>().ReverseMap();

            CreateMap<StockDocumentWriteDto, StockDocument>().ReverseMap();
            CreateMap<StockDocument, StockDocumentReadDto>().ReverseMap();

            CreateMap<StockItemWriteDto, StockItem>()
                    .ForMember(x => x.Product, opt => opt.Ignore())
                    .ForMember(x => x.ProductUnit, opt => opt.Ignore()).ReverseMap();
            CreateMap<StockItem, StockItemReadDto>().ReverseMap();

            CreateMap<CheckWriteDto, Check>().ReverseMap();
            CreateMap<Check, CheckReadDto>().ReverseMap();


            CreateMap <StockTransactionWriteDto, StockTransaction>().ReverseMap();
            CreateMap<StockTransaction, StockTransactionReadDto>().ReverseMap();
            
            CreateMap <FinancialTransactionWriteDto, FinancialTransaction>().ReverseMap();
            CreateMap<FinancialTransaction, FinancialTransactionReadDto>().ReverseMap();
            CreateMap<FinancialPostDto, FinancialTransaction>().ReverseMap();




            CreateMap<CashierSessionWriteDto, CashierSession>().ReverseMap();
            CreateMap<CashierSession, CashierSessionReadDto>().ReverseMap();

            CreateMap<ReportPermissionWriteDto, ReportPermission>().ReverseMap();
            CreateMap<ReportPermission, ReportPermissionReadDto>().ReverseMap();


        }
    }
}
