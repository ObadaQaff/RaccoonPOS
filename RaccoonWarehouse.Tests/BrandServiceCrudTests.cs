using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Helper;
using RaccoonWarehouse.Application.Service.Brands;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Data.Repository;
using RaccoonWarehouse.Domain.Brands;
using RaccoonWarehouse.Domain.Brands.DTOs;
using Xunit;

namespace RaccoonWarehouse.Tests;

public class BrandServiceCrudTests
{
    private static BrandService CreateService(string databaseName, out ApplicationDbContext context)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        context = new ApplicationDbContext(options);
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        var uow = new UOW(context, mapper);
        return new BrandService(context, uow, mapper);
    }

    [Fact]
    public async Task Create_Brand_ShouldPersistSuccessfully()
    {
        var service = CreateService(nameof(Create_Brand_ShouldPersistSuccessfully), out var context);

        var dto = new BrandWriteDto
        {
            Name = "QA Brand Create",
            ImageUrl = null
        };

        var result = await service.CreateAsync(dto);
        var created = await context.Set<Brand>().FirstOrDefaultAsync(b => b.Name == dto.Name);

        Assert.True(result.Success);
        Assert.NotNull(created);
        Assert.Equal("QA Brand Create", created!.Name);
        Assert.Null(created.ImageUrl);
    }

    [Fact]
    public async Task Update_Brand_ShouldUpdateAndAllowNullableImageUrl()
    {
        var service = CreateService(nameof(Update_Brand_ShouldUpdateAndAllowNullableImageUrl), out var context);

        await service.CreateAsync(new BrandWriteDto
        {
            Name = "Old Brand",
            ImageUrl = "http://old.url"
        });

        var created = await context.Set<Brand>().FirstAsync(b => b.Name == "Old Brand");

        var updateResult = await service.UpdateAsync(new BrandWriteDto
        {
            Id = created.Id,
            Name = "New Brand",
            ImageUrl = null
        });

        var updated = await context.Set<Brand>().FirstAsync(b => b.Id == created.Id);

        Assert.True(updateResult.Success);
        Assert.Equal("New Brand", updated.Name);
        Assert.Null(updated.ImageUrl);
    }

    [Fact]
    public async Task Delete_Brand_ShouldRemoveEntity()
    {
        var service = CreateService(nameof(Delete_Brand_ShouldRemoveEntity), out var context);

        await service.CreateAsync(new BrandWriteDto
        {
            Name = "Delete Brand"
        });

        var created = await context.Set<Brand>().FirstAsync(b => b.Name == "Delete Brand");
        var deleteResult = await service.DeleteAsync(created.Id);
        var deleted = await context.Set<Brand>().FirstOrDefaultAsync(b => b.Id == created.Id);

        Assert.True(deleteResult.Success);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task Delete_Brand_NotFound_ShouldFailGracefully()
    {
        var service = CreateService(nameof(Delete_Brand_NotFound_ShouldFailGracefully), out _);
        var result = await service.DeleteAsync(999999);

        Assert.False(result.Success);
        Assert.False(result.Data);
    }
}
