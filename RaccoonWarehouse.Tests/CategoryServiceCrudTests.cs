using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Helper;
using RaccoonWarehouse.Application.Service.Categories;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Data.Repository;
using RaccoonWarehouse.Domain.Categories;
using RaccoonWarehouse.Domain.Categories.DTOs;
using Xunit;

namespace RaccoonWarehouse.Tests;

public class CategoryServiceCrudTests
{
    private static CategoryService CreateService(string databaseName, out ApplicationDbContext context)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        context = new ApplicationDbContext(options);
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        var uow = new UOW(context, mapper);
        return new CategoryService(context, uow, mapper);
    }

    [Fact]
    public async Task Create_Category_ShouldPersistSuccessfully()
    {
        var service = CreateService(nameof(Create_Category_ShouldPersistSuccessfully), out var context);

        var dto = new CategoryWriteDto
        {
            Name = "QA Category Create",
            Description = "Created by test"
        };

        var result = await service.CreateAsync(dto);
        var created = await context.Set<Category>().FirstOrDefaultAsync(c => c.Name == dto.Name);

        Assert.True(result.Success);
        Assert.NotNull(created);
        Assert.Equal("QA Category Create", created!.Name);
    }

    [Fact]
    public async Task Update_Category_ShouldChangeStoredValues()
    {
        var service = CreateService(nameof(Update_Category_ShouldChangeStoredValues), out var context);

        var createResult = await service.CreateAsync(new CategoryWriteDto
        {
            Name = "Old Category",
            Description = "Old Description"
        });

        Assert.True(createResult.Success);
        var created = await context.Set<Category>().FirstAsync(c => c.Name == "Old Category");

        var updateResult = await service.UpdateAsync(new CategoryWriteDto
        {
            Id = created.Id,
            Name = "New Category",
            Description = "Updated Description"
        });

        var updated = await context.Set<Category>().FirstAsync(c => c.Id == created.Id);

        Assert.True(updateResult.Success);
        Assert.Equal("New Category", updated.Name);
        Assert.Equal("Updated Description", updated.Description);
    }

    [Fact]
    public async Task Delete_Category_ShouldRemoveEntity()
    {
        var service = CreateService(nameof(Delete_Category_ShouldRemoveEntity), out var context);

        await service.CreateAsync(new CategoryWriteDto
        {
            Name = "Delete Category",
            Description = "To be deleted"
        });

        var created = await context.Set<Category>().FirstAsync(c => c.Name == "Delete Category");
        var deleteResult = await service.DeleteAsync(created.Id);
        var deleted = await context.Set<Category>().FirstOrDefaultAsync(c => c.Id == created.Id);

        Assert.True(deleteResult.Success);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task Create_Category_WithNullName_ShouldThrow_AndNotCrashTestRun()
    {
        var service = CreateService(nameof(Create_Category_WithNullName_ShouldThrow_AndNotCrashTestRun), out _);

        // Negative input case: invalid null name should surface as an exception with current implementation.
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await service.CreateAsync(new CategoryWriteDto
            {
                Name = null!,
                Description = "Invalid input"
            });
        });
    }
}
