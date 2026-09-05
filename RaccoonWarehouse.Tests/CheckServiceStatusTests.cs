using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Helper;
using RaccoonWarehouse.Application.Service.Checks;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Data.Repository;
using RaccoonWarehouse.Domain.Checks;
using RaccoonWarehouse.Domain.Enums;
using Xunit;

namespace RaccoonWarehouse.Tests;

public class CheckServiceStatusTests
{
    [Fact]
    public async Task UpdateStatusAsync_PersistsRequestedStatus()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(nameof(UpdateStatusAsync_PersistsRequestedStatus))
            .Options;
        await using var context = new ApplicationDbContext(options);
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        var service = new CheckService(context, new UOW(context, mapper), mapper);
        var check = new Check
        {
            CheckNumber = "CHK-STATUS-1",
            BankName = "Test Bank",
            DueDate = new DateTime(2026, 8, 10),
            Amount = 250m,
            Status = CheckStatus.Pending,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };
        context.Set<Check>().Add(check);
        await context.SaveChangesAsync();

        var result = await service.UpdateStatusAsync(check.Id, CheckStatus.Deposited);
        context.ChangeTracker.Clear();
        var persistedStatus = await context.Set<Check>()
            .Where(x => x.Id == check.Id)
            .Select(x => x.Status)
            .SingleAsync();

        Assert.True(result.Success);
        Assert.Equal(CheckStatus.Deposited, persistedStatus);
    }

    [Fact]
    public async Task UpdateStatusAsync_MissingCheck_ReturnsFailure()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(nameof(UpdateStatusAsync_MissingCheck_ReturnsFailure))
            .Options;
        await using var context = new ApplicationDbContext(options);
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        var service = new CheckService(context, new UOW(context, mapper), mapper);

        var result = await service.UpdateStatusAsync(999, CheckStatus.Cleared);

        Assert.False(result.Success);
    }
}
