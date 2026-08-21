using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Generic;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Brands;
using RaccoonWarehouse.Domain.Brands.DTOs;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Application.Service.Products
{
    public class ProductService : GenericService<Product, ProductWriteDto, ProductReadDto>, IProductService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        public ProductService(ApplicationDbContext context, IUOW uow, IMapper mapper) : base(context, uow, mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public override async Task<Result<ProductWriteDto>> CreateAsync(ProductWriteDto dto)
        {
            if (dto.ITEMCODE.HasValue)
            {
                var barcodeExists = await _uow.GetRepository<Product>()
                    .GetAllAsQueryable()
                    .AnyAsync(product => !product.IsDeleted && product.ITEMCODE == dto.ITEMCODE);

                if (barcodeExists)
                    return Result<ProductWriteDto>.Fail("الباركود مستخدم بالفعل / This barcode is already used by another product.");
            }

            return await base.CreateAsync(dto);
        }

        public async Task<Result> ApplyTaxToProductUnitsAsync(int productId)
        {
            var productRepo = _uow.GetRepository<Product>();
            var product = await productRepo
                .GetAllAsQueryable()
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                return Result.Fail("Product not found.");

            return await ApplyTaxToProductUnitsAsync(product);
        }

        public async Task<Result<ProductReadDto>> GetByIdWithUnitsAsync(int productId)
        {
            var product = await _uow.GetRepository<Product>()
                .GetAllAsQueryable()
                .AsNoTracking()
                .Include(p => p.ProductUnits!)
                    .ThenInclude(unit => unit.Unit)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                return Result<ProductReadDto>.Fail("Product not found.");

            return Result<ProductReadDto>.Ok(_mapper.Map<ProductReadDto>(product));
        }

        public async Task<Result> ApplyTaxToProductUnitsAsync(Product product)
        {
            var taxRate = product.TaxRate ?? 0m;
            if (taxRate < 0)
                return Result.Fail("TaxRate cannot be negative.");

            var unitRepo = _uow.GetRepository<ProductUnit>();

            var units = await unitRepo
                .GetAllAsQueryable()
                .Where(u => u.ProductId == product.Id)
                .ToListAsync();

            if (!units.Any())
                return Result.Ok("No product units found.");

            foreach (var u in units)
            {
                u.UnTaxedPrice = u.SalePrice;
                u.UpdatedDate = DateTime.Now;

                await unitRepo.UpdateAsync(u);
            }

            await _uow.CommitAsync();
            return Result.Ok("Product tax settings saved without changing unit sale prices.");
        }
        public async Task<Result> UpdateProductWithUnitsAsync(
            ProductWriteDto productDto,
            List<ProductUnitWriteDto> unitsDto)
        {
            var validationMessage = ValidateProductUnits(unitsDto);
            if (validationMessage != null)
                return Result.Fail(validationMessage);

            NormalizeUnitFlags(unitsDto);

            var productRepo = _uow.GetRepository<Product>();
            var unitRepo = _uow.GetRepository<ProductUnit>();

            var product = await productRepo
                .GetAllAsQueryable()
                .Include(p => p.ProductUnits)
                .FirstOrDefaultAsync(p => p.Id == productDto.Id);

            if (product == null)
                return Result.Fail("Product not found.");

            var alternateBarcodeValidation = await ValidateAlternateBarcodesAsync(productDto, unitsDto);
            if (alternateBarcodeValidation != null)
                return Result.Fail(alternateBarcodeValidation);

            // =========================
            // 1️⃣ Update Product scalars
            // =========================
            product.Name = productDto.Name;
            product.ITEMCODE = productDto.ITEMCODE;
            product.Description = productDto.Description;
            product.Status = productDto.Status;
            product.TaxExempt = productDto.TaxExempt;
            product.TaxRate = productDto.TaxRate;
            product.MiniQuantity = productDto.MiniQuantity;
            product.BrandId = productDto.BrandId;
            product.SubCategoryId = productDto.SubCategoryId;
            product.UpdatedDate = DateTime.Now;

            await productRepo.UpdateAsync(product);

            // =========================
            // 2️⃣ Sync Units
            // =========================

            var existingUnits = product.ProductUnits.ToList();

            var incomingIds = unitsDto
                .Where(u => u.Id > 0)
                .Select(u => u.Id)
                .ToHashSet();

            // 2.a Remove deleted units
            var unitsToRemove = existingUnits
                .Where(u => !incomingIds.Contains(u.Id))
                .ToList();

            foreach (var unit in unitsToRemove)
            {
                await unitRepo.DeleteAsync(unit.Id);
            }
            foreach (var unit in unitsDto)
            {
                unit.UnTaxedPrice = unit.SalePrice;
            }

            // 2.b Update existing + Add new
            foreach (var unitDto in unitsDto)
            {
                if (unitDto.Id > 0)
                {
                    // Update existing
                    var unit = existingUnits.First(u => u.Id == unitDto.Id);

                    unit.UnitId = unitDto.UnitId;
                    unit.AlternateBarcode = NormalizeAlternateBarcode(unitDto.AlternateBarcode);
                    unit.SalePrice = unitDto.SalePrice;
                    unit.PurchasePrice = unitDto.PurchasePrice;
                    unit.QuantityPerUnit = unitDto.QuantityPerUnit;
                    unit.UnTaxedPrice = unitDto.UnTaxedPrice;
                    unit.IsBaseUnit = unitDto.IsBaseUnit;
                    unit.IsDefaultSaleUnit = unitDto.IsDefaultSaleUnit;
                    unit.IsDefaultPurchaseUnit = unitDto.IsDefaultPurchaseUnit;
                    unit.UpdatedDate = DateTime.Now;

                    await unitRepo.UpdateAsync(unit);
                }
                else
                {
                    // Add new
                    var newUnit = new ProductUnit
                    {
                        ProductId = product.Id,
                        UnitId = unitDto.UnitId,
                        AlternateBarcode = NormalizeAlternateBarcode(unitDto.AlternateBarcode),
                        SalePrice = unitDto.SalePrice,
                        PurchasePrice = unitDto.PurchasePrice,
                        QuantityPerUnit = unitDto.QuantityPerUnit,
                        UnTaxedPrice = unitDto.UnTaxedPrice,
                        IsBaseUnit = unitDto.IsBaseUnit,
                        IsDefaultSaleUnit = unitDto.IsDefaultSaleUnit,
                        IsDefaultPurchaseUnit = unitDto.IsDefaultPurchaseUnit,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now
                    };

                    await unitRepo.AddAsync(newUnit);
                }
            }

            await _uow.CommitAsync();

            return Result.Ok("Product and units updated successfully.");
        }

        private static string? ValidateProductUnits(List<ProductUnitWriteDto> unitsDto)
        {
            if (unitsDto == null || unitsDto.Count == 0)
                return "يجب إضافة وحدة واحدة على الأقل للصنف.";

            if (unitsDto.Any(u => u.UnitId <= 0))
                return "كل وحدة يجب أن تحتوي على وحدة قياس صحيحة.";

            if (unitsDto.Any(u => u.QuantityPerUnit <= 0))
                return "الكمية لكل وحدة يجب أن تكون أكبر من صفر.";

            if (unitsDto.GroupBy(u => u.UnitId).Any(g => g.Count() > 1))
                return "لا يمكن تكرار نفس الوحدة أكثر من مرة لنفس الصنف.";

            if (unitsDto.Count(u => u.IsBaseUnit) > 1)
                return "يمكن اختيار وحدة أساسية واحدة فقط لكل صنف.";

            if (unitsDto.Count(u => u.IsDefaultSaleUnit) > 1)
                return "يمكن اختيار وحدة بيع افتراضية واحدة فقط لكل صنف.";

            if (unitsDto.Count(u => u.IsDefaultPurchaseUnit) > 1)
                return "يمكن اختيار وحدة شراء افتراضية واحدة فقط لكل صنف.";

            return null;
        }

        private async Task<string?> ValidateAlternateBarcodesAsync(ProductWriteDto productDto, List<ProductUnitWriteDto> unitsDto)
        {
            var alternateBarcodes = unitsDto
                .Select(unit => NormalizeAlternateBarcode(unit.AlternateBarcode))
                .Where(barcode => barcode != null)
                .Select(barcode => barcode!)
                .ToList();

            if (alternateBarcodes.Count != alternateBarcodes.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                return "لا يمكن تكرار الرمز المماثل / Alternate barcodes must be unique.";

            if (productDto.ITEMCODE.HasValue && alternateBarcodes.Any(barcode => barcode == productDto.ITEMCODE.Value.ToString()))
                return "الرمز المماثل يجب أن يختلف عن الباركود الرئيسي / An alternate barcode must differ from the primary barcode.";

            var existingUnitIds = unitsDto
                .Where(dto => dto.Id > 0)
                .Select(dto => dto.Id)
                .ToList();

            var unitRepo = _uow.GetRepository<ProductUnit>();
            var existingAlternate = await unitRepo.GetAllAsQueryable()
                .Where(unit => unit.AlternateBarcode != null)
                .Where(unit => !existingUnitIds.Contains(unit.Id))
                .Select(unit => unit.AlternateBarcode!)
                .ToListAsync();

            if (alternateBarcodes.Any(barcode => existingAlternate.Any(existing =>
                    string.Equals(existing, barcode, StringComparison.OrdinalIgnoreCase))))
                return "الرمز المماثل مستخدم بالفعل / An alternate barcode is already in use.";

            var primaryBarcodeConflict = await _uow.GetRepository<Product>()
                .GetAllAsQueryable()
                .Where(product => product.Id != productDto.Id && product.ITEMCODE.HasValue)
                .Select(product => product.ITEMCODE!.Value)
                .AnyAsync(code => alternateBarcodes.Contains(code.ToString()));

            return primaryBarcodeConflict
                ? "الرمز المماثل مستخدم كرمز رئيسي / An alternate barcode is already used as a primary barcode."
                : null;
        }

        private static string? NormalizeAlternateBarcode(string? barcode)
            => string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim();

        private static void NormalizeUnitFlags(List<ProductUnitWriteDto> unitsDto)
        {
            if (unitsDto.Count == 1)
            {
                unitsDto[0].IsBaseUnit = true;
                unitsDto[0].IsDefaultSaleUnit = true;
                unitsDto[0].IsDefaultPurchaseUnit = true;
                return;
            }

            var baseUnit = unitsDto.FirstOrDefault(u => u.IsBaseUnit) ?? unitsDto[0];
            baseUnit.IsBaseUnit = true;

            var saleUnit = unitsDto.FirstOrDefault(u => u.IsDefaultSaleUnit) ?? baseUnit;
            saleUnit.IsDefaultSaleUnit = true;

            var purchaseUnit = unitsDto.FirstOrDefault(u => u.IsDefaultPurchaseUnit) ?? baseUnit;
            purchaseUnit.IsDefaultPurchaseUnit = true;

            foreach (var unit in unitsDto)
            {
                if (!ReferenceEquals(unit, baseUnit))
                    unit.IsBaseUnit = false;

                if (!ReferenceEquals(unit, saleUnit))
                    unit.IsDefaultSaleUnit = false;

                if (!ReferenceEquals(unit, purchaseUnit))
                    unit.IsDefaultPurchaseUnit = false;
            }
        }



    }
    public interface IProductService : IGenericService<Product, ProductWriteDto, ProductReadDto>
    {
        Task<Result<ProductReadDto>> GetByIdWithUnitsAsync(int productId);
        Task<Result> ApplyTaxToProductUnitsAsync(int productId);
        Task<Result> ApplyTaxToProductUnitsAsync(Product product);
        Task<Result> UpdateProductWithUnitsAsync(
    ProductWriteDto productDto,
    List<ProductUnitWriteDto> unitsDto);


    }
}
