using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Helper;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Data.Repository;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Products.DTOs;
using Xunit;

namespace RaccoonWarehouse.Tests;

public class ProductServiceCrudTests
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
    public async Task Create_Product_ShouldPersistSuccessfully()
    {
        var service = CreateService(nameof(Create_Product_ShouldPersistSuccessfully), out var context);

        var dto = new ProductWriteDto
        {
            Name = "QA Product Create",
            ITEMCODE = 1234567890,
            Description = "Created by test",
            Status = ProductStatus.InStock,
            SubCategoryId = 1,
            BrandId = null,
            MiniQuantity = 2
        };

        var result = await service.CreateAsync(dto);
        var created = await context.Set<Product>().FirstOrDefaultAsync(p => p.Name == dto.Name);

        Assert.True(result.Success);
        Assert.NotNull(created);
        Assert.Equal("QA Product Create", created!.Name);
        Assert.Equal(ProductStatus.InStock, created.Status);
    }

    [Fact]
    public async Task Update_Product_ShouldChangeStoredValues()
    {
        var service = CreateService(nameof(Update_Product_ShouldChangeStoredValues), out var context);

        var createResult = await service.CreateAsync(new ProductWriteDto
        {
            Name = "Old Name",
            ITEMCODE = 1111,
            Description = "Old Description",
            Status = ProductStatus.InStock,
            SubCategoryId = 1
        });

        Assert.True(createResult.Success);
        var created = await context.Set<Product>().FirstAsync(p => p.Name == "Old Name");

        var updateResult = await service.UpdateAsync(new ProductWriteDto
        {
            Id = created.Id,
            Name = "New Name",
            ITEMCODE = 2222,
            Description = "Updated Description",
            Status = ProductStatus.BackOrder,
            SubCategoryId = 1,
            BrandId = null
        });

        var updated = await context.Set<Product>().FirstAsync(p => p.Id == created.Id);

        Assert.True(updateResult.Success);
        Assert.Equal("New Name", updated.Name);
        Assert.Equal(2222, updated.ITEMCODE);
        Assert.Equal(ProductStatus.BackOrder, updated.Status);
    }

    [Fact]
    public async Task Delete_Product_ShouldRemoveEntity()
    {
        var service = CreateService(nameof(Delete_Product_ShouldRemoveEntity), out var context);

        await service.CreateAsync(new ProductWriteDto
        {
            Name = "Delete Me",
            ITEMCODE = 3333,
            Description = "To be deleted",
            Status = ProductStatus.InStock,
            SubCategoryId = 1
        });

        var created = await context.Set<Product>().FirstAsync(p => p.Name == "Delete Me");
        var deleteResult = await service.DeleteAsync(created.Id);
        var deleted = await context.Set<Product>().FirstOrDefaultAsync(p => p.Id == created.Id);

        Assert.True(deleteResult.Success);
        Assert.Null(deleted);
    }
}
