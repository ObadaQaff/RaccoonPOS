using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Helper;
using RaccoonWarehouse.Application.Service.SubCategories;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Data.Repository;
using RaccoonWarehouse.Domain.SubCategories;
using RaccoonWarehouse.Domain.SubCategories.DTOs;
using Xunit;

namespace RaccoonWarehouse.Tests;

public class SubCategoryServiceCrudTests
{
    private static SubCategoryService CreateService(string databaseName, out ApplicationDbContext context)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        context = new ApplicationDbContext(options);
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        var uow = new UOW(context, mapper);
        return new SubCategoryService(context, uow, mapper);
    }

    [Fact]
    public async Task Create_SubCategory_ShouldPersistSuccessfully()
    {
        var service = CreateService(nameof(Create_SubCategory_ShouldPersistSuccessfully), out var context);

        var dto = new SubCategoryWriteDto
        {
            Name = "QA SubCategory Create",
            ParentCategoryId = 1,
            Description = null,
            ImageUrl = null
        };

        var result = await service.CreateAsync(dto);
        var created = await context.Set<SubCategory>().FirstOrDefaultAsync(s => s.Name == dto.Name);

        Assert.True(result.Success);
        Assert.NotNull(created);
        Assert.Equal("QA SubCategory Create", created!.Name);
        Assert.Null(created.Description);
        Assert.Null(created.ImageUrl);
    }

    [Fact]
    public async Task Update_SubCategory_ShouldUpdateAndKeepNullableFieldsNullable()
    {
        var service = CreateService(nameof(Update_SubCategory_ShouldUpdateAndKeepNullableFieldsNullable), out var context);

        await service.CreateAsync(new SubCategoryWriteDto
        {
            Name = "Old SubCategory",
            ParentCategoryId = 1,
            Description = "Old Description",
            ImageUrl = "http://old.url"
        });

        var created = await context.Set<SubCategory>().FirstAsync(s => s.Name == "Old SubCategory");

        var updateResult = await service.UpdateAsync(new SubCategoryWriteDto
        {
            Id = created.Id,
            Name = "New SubCategory",
            ParentCategoryId = 1,
            Description = null,
            ImageUrl = null
        });

        var updated = await context.Set<SubCategory>().FirstAsync(s => s.Id == created.Id);

        Assert.True(updateResult.Success);
        Assert.Equal("New SubCategory", updated.Name);
        Assert.Null(updated.Description);
        Assert.Null(updated.ImageUrl);
    }

    [Fact]
    public async Task Delete_SubCategory_ShouldRemoveEntity()
    {
        var service = CreateService(nameof(Delete_SubCategory_ShouldRemoveEntity), out var context);

        await service.CreateAsync(new SubCategoryWriteDto
        {
            Name = "Delete SubCategory",
            ParentCategoryId = 1
        });

        var created = await context.Set<SubCategory>().FirstAsync(s => s.Name == "Delete SubCategory");
        var deleteResult = await service.DeleteAsync(created.Id);
        var deleted = await context.Set<SubCategory>().FirstOrDefaultAsync(s => s.Id == created.Id);

        Assert.True(deleteResult.Success);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task Delete_SubCategory_NotFound_ShouldFailGracefully()
    {
        var service = CreateService(nameof(Delete_SubCategory_NotFound_ShouldFailGracefully), out _);
        var result = await service.DeleteAsync(999999);

        Assert.False(result.Success);
        Assert.False(result.Data);
    }
}
