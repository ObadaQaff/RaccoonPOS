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

        public async Task<Result> ApplyTaxToProductUnitsAsync(Product product)
        {
            // إذا TaxExempt => ما في ضريبة
            /*if (product.TaxExempt)
                return Result.Ok("Product is tax exempt. No changes applied.");
*/
            var taxRate = product.TaxRate ?? 0m;
            if (taxRate < 0)
                return Result.Fail("TaxRate cannot be negative.");

            // جيب كل الوحدات للمنتج
            var unitRepo = _uow.GetRepository<ProductUnit>();

            var units = await unitRepo
                .GetAllAsQueryable()
                .Where(u => u.ProductId == product.Id)
                .ToListAsync();

            if (!units.Any())
                return Result.Ok("No product units found.");

            foreach (var u in units)
            {
                // ✅ لازم يكون عندك UnTaxedPrice (سعر بدون ضريبة)
                // إذا مش موجود عندك بالـ entity، ضيفه.
                var basePrice = u.UnTaxedPrice;

                // احسب السعر مع الضريبة
                u.SalePrice = basePrice + (basePrice * taxRate / 100m);
                u.UpdatedDate = DateTime.Now;

                await unitRepo.UpdateAsync(u);
            }

            await _uow.CommitAsync();
            return Result.Ok("Tax applied to all product units successfully.");
        }
        public async Task<Result> UpdateProductWithUnitsAsync(
            ProductWriteDto productDto,
            List<ProductUnitWriteDto> unitsDto)
        {
            var productRepo = _uow.GetRepository<Product>();
            var unitRepo = _uow.GetRepository<ProductUnit>();

            var product = await productRepo
                .GetAllAsQueryable()
                .Include(p => p.ProductUnits)
                .FirstOrDefaultAsync(p => p.Id == productDto.Id);

            if (product == null)
                return Result.Fail("Product not found.");

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
            if (product.TaxExempt == false)
            {
                if (product.TaxRate > 0)
                {
                    decimal taxRate = product.TaxRate.Value;
                    foreach (var unit in unitsDto)
                    {

                            unit.UnTaxedPrice = unit.SalePrice; 
                            var basePrice = unit.UnTaxedPrice;
                            unit.SalePrice = basePrice + (basePrice * taxRate / 100m);
                            unit.UpdatedDate = DateTime.Now;
                        
                    }
                }
            }

            // 2.b Update existing + Add new
            foreach (var unitDto in unitsDto)
            {
                if (unitDto.Id > 0)
                {
                    // Update existing
                    var unit = existingUnits.First(u => u.Id == unitDto.Id);

                    unit.UnitId = unitDto.UnitId;
                    unit.SalePrice = unitDto.SalePrice;
                    unit.PurchasePrice = unitDto.PurchasePrice;
                    unit.QuantityPerUnit = unitDto.QuantityPerUnit;
                    unit.UnTaxedPrice = unitDto.UnTaxedPrice;
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
                        SalePrice = unitDto.SalePrice,
                        PurchasePrice = unitDto.PurchasePrice,
                        QuantityPerUnit = unitDto.QuantityPerUnit,
                        UnTaxedPrice = unitDto.UnTaxedPrice,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now
                    };

                    await unitRepo.AddAsync(newUnit);
                }
            }

            await _uow.CommitAsync();

            return Result.Ok("Product and units updated successfully.");
        }



    }
    public interface IProductService : IGenericService<Product, ProductWriteDto, ProductReadDto>
    {
        Task<Result> ApplyTaxToProductUnitsAsync(int productId);
        Task<Result> ApplyTaxToProductUnitsAsync(Product product);
        Task<Result> UpdateProductWithUnitsAsync(
    ProductWriteDto productDto,
    List<ProductUnitWriteDto> unitsDto);


    }
}
