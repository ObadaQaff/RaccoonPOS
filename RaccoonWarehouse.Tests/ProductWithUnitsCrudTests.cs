using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Helper;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Data.Repository;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using Xunit;

namespace RaccoonWarehouse.Tests;

public class ProductWithUnitsCrudTests
{
    private static ProductService CreateService(string databaseName, out ApplicationDbContext context)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        context = new ApplicationDbContext(options);
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        var uow = new UOW(context, mapper);
        return new ProductService(context, uow, mapper);
    }

    [Fact]
    public async Task UpdateProductWithUnits_ShouldUpdateProduct_AddUpdateAndRemoveUnits()
    {
        var service = CreateService(nameof(UpdateProductWithUnits_ShouldUpdateProduct_AddUpdateAndRemoveUnits), out var context);

        var product = new Product
        {
            Name = "Old Product",
            ITEMCODE = 111,
            Description = "Old",
            Status = ProductStatus.InStock,
            TaxExempt = false,
            TaxRate = 10,
            SubCategoryId = 1,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };
        context.Set<Product>().Add(product);
        await context.SaveChangesAsync();

        var existingUnitToUpdate = new ProductUnit
        {
            ProductId = product.Id,
            UnitId = 1,
            SalePrice = 10,
            PurchasePrice = 5,
            QuantityPerUnit = 1,
            UnTaxedPrice = 10,
            IsBaseUnit = true,
            IsDefaultSaleUnit = true,
            IsDefaultPurchaseUnit = true,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };
        var existingUnitToRemove = new ProductUnit
        {
            ProductId = product.Id,
            UnitId = 2,
            SalePrice = 20,
            PurchasePrice = 10,
            QuantityPerUnit = 2,
            UnTaxedPrice = 20,
            IsBaseUnit = false,
            IsDefaultSaleUnit = false,
            IsDefaultPurchaseUnit = false,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };
        context.Set<ProductUnit>().AddRange(existingUnitToUpdate, existingUnitToRemove);
        await context.SaveChangesAsync();

        var updateDto = new ProductWriteDto
        {
            Id = product.Id,
            Name = "New Product",
            ITEMCODE = 222,
            Description = "New",
            Status = ProductStatus.BackOrder,
            TaxExempt = false,
            TaxRate = 10,
            SubCategoryId = 1,
            BrandId = null
        };

        var incomingUnits = new List<ProductUnitWriteDto>
        {
            new()
            {
                Id = existingUnitToUpdate.Id,
                UnitId = 1,
                SalePrice = 100,
                PurchasePrice = 50,
                QuantityPerUnit = 1,
                UnTaxedPrice = 100,
                IsBaseUnit = true,
                IsDefaultSaleUnit = true,
                IsDefaultPurchaseUnit = true
            },
            new()
            {
                Id = 0,
                UnitId = 3,
                SalePrice = 30,
                PurchasePrice = 15,
                QuantityPerUnit = 3,
                UnTaxedPrice = 30,
                IsBaseUnit = false,
                IsDefaultSaleUnit = false,
                IsDefaultPurchaseUnit = false
            }
        };

        var result = await service.UpdateProductWithUnitsAsync(updateDto, incomingUnits);

        var updatedProduct = await context.Set<Product>().FirstAsync(p => p.Id == product.Id);
        var unitsAfter = await context.Set<ProductUnit>().Where(u => u.ProductId == product.Id).ToListAsync();

        Assert.True(result.Success);
        Assert.Equal("New Product", updatedProduct.Name);
        Assert.Equal(ProductStatus.BackOrder, updatedProduct.Status);
        Assert.Equal(2, unitsAfter.Count);
        Assert.DoesNotContain(unitsAfter, u => u.Id == existingUnitToRemove.Id);
        Assert.Contains(unitsAfter, u => u.UnitId == 3);
    }

    [Fact]
    public async Task UpdateProductWithUnits_DuplicateUnitIds_ShouldFailValidation()
    {
        var service = CreateService(nameof(UpdateProductWithUnits_DuplicateUnitIds_ShouldFailValidation), out var context);

        var product = new Product
        {
            Name = "Any Product",
            Status = ProductStatus.InStock,
            SubCategoryId = 1,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };
        context.Set<Product>().Add(product);
        await context.SaveChangesAsync();

        var dto = new ProductWriteDto
        {
            Id = product.Id,
            Name = "Any Product",
            Status = ProductStatus.InStock,
            SubCategoryId = 1
        };

        var units = new List<ProductUnitWriteDto>
        {
            new() { UnitId = 1, SalePrice = 10, PurchasePrice = 5, QuantityPerUnit = 1, IsBaseUnit = true, IsDefaultSaleUnit = true, IsDefaultPurchaseUnit = true },
            new() { UnitId = 1, SalePrice = 20, PurchasePrice = 10, QuantityPerUnit = 2, IsBaseUnit = false, IsDefaultSaleUnit = false, IsDefaultPurchaseUnit = false }
        };

        var result = await service.UpdateProductWithUnitsAsync(dto, units);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task UpdateProductWithUnits_ProductNotFound_ShouldFail()
    {
        var service = CreateService(nameof(UpdateProductWithUnits_ProductNotFound_ShouldFail), out _);

        var dto = new ProductWriteDto
        {
            Id = 999999,
            Name = "Missing Product",
            Status = ProductStatus.InStock,
            SubCategoryId = 1
        };

        var units = new List<ProductUnitWriteDto>
        {
            new() { UnitId = 1, SalePrice = 10, PurchasePrice = 5, QuantityPerUnit = 1, IsBaseUnit = true, IsDefaultSaleUnit = true, IsDefaultPurchaseUnit = true }
        };

        var result = await service.UpdateProductWithUnitsAsync(dto, units);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ApplyTaxToProductUnits_ShouldRecalculateSalePrice()
    {
        var service = CreateService(nameof(ApplyTaxToProductUnits_ShouldRecalculateSalePrice), out var context);

        var product = new Product
        {
            Name = "Tax Product",
            Status = ProductStatus.InStock,
            TaxExempt = false,
            TaxRate = 16,
            SubCategoryId = 1,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };
        context.Set<Product>().Add(product);
        await context.SaveChangesAsync();

        var unit = new ProductUnit
        {
            ProductId = product.Id,
            UnitId = 1,
            SalePrice = 100,
            UnTaxedPrice = 100,
            PurchasePrice = 80,
            QuantityPerUnit = 1,
            IsBaseUnit = true,
            IsDefaultSaleUnit = true,
            IsDefaultPurchaseUnit = true,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };
        context.Set<ProductUnit>().Add(unit);
        await context.SaveChangesAsync();

        var result = await service.ApplyTaxToProductUnitsAsync(product.Id);
        var updatedUnit = await context.Set<ProductUnit>().FirstAsync(u => u.Id == unit.Id);

        Assert.True(result.Success);
        Assert.Equal(116m, updatedUnit.SalePrice);
    }
}
