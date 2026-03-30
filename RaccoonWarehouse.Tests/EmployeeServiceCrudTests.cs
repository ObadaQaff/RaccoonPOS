using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Helper;
using RaccoonWarehouse.Application.Service.Employees;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Data.Repository;
using RaccoonWarehouse.Domain.Employees.DTOs;
using RaccoonWarehouse.Domain.Enums;
using Xunit;
using EmployeeEntity = RaccoonWarehouse.Domain.Employees.Employee;

namespace RaccoonWarehouse.Tests;

public class EmployeeServiceCrudTests
{
    private static EmployeeService CreateService(string databaseName, out ApplicationDbContext context)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        context = new ApplicationDbContext(options);
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        var uow = new UOW(context, mapper);
        var featureService = new EmployeeFeatureService(context);
        return new EmployeeService(context, uow, mapper, featureService);
    }

    [Fact]
    public async Task Create_Employee_ShouldPersistSuccessfully()
    {
        var service = CreateService(nameof(Create_Employee_ShouldPersistSuccessfully), out var context);

        var dto = new EmployeeCreateDto
        {
            Code = "EMP-001",
            FullName = "Employee One",
            PhoneNumber = "0799999999",
            Email = "employee1@test.com",
            Status = EmployeeStatus.Active
        };

        var result = await service.CreateAsync(dto);
        var created = await context.Set<EmployeeEntity>().FirstOrDefaultAsync(x => x.Code == "EMP-001");

        Assert.True(result.Success);
        Assert.NotNull(created);
        Assert.Equal("Employee One", created!.FullName);
    }

    [Fact]
    public async Task Create_Employee_WithDuplicateCode_ShouldFail()
    {
        var service = CreateService(nameof(Create_Employee_WithDuplicateCode_ShouldFail), out _);

        await service.CreateAsync(new EmployeeCreateDto
        {
            Code = "EMP-001",
            FullName = "First Employee"
        });

        var result = await service.CreateAsync(new EmployeeCreateDto
        {
            Code = "EMP-001",
            FullName = "Second Employee"
        });

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Update_Employee_ShouldChangeStatusAndFields()
    {
        var service = CreateService(nameof(Update_Employee_ShouldChangeStatusAndFields), out var context);

        var createResult = await service.CreateAsync(new EmployeeCreateDto
        {
            Code = "EMP-002",
            FullName = "Updatable Employee",
            Status = EmployeeStatus.Active
        });

        var updateResult = await service.UpdateAsync(new EmployeeUpdateDto
        {
            Id = createResult.Data.Id,
            Code = "EMP-002",
            FullName = "Updated Employee",
            Status = EmployeeStatus.Suspended,
            JobTitle = "Cashier",
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        });

        var updated = await context.Set<EmployeeEntity>().FirstAsync(x => x.Id == createResult.Data.Id);

        Assert.True(updateResult.Success);
        Assert.Equal("Updated Employee", updated.FullName);
        Assert.Equal(EmployeeStatus.Suspended, updated.Status);
        Assert.Equal("Cashier", updated.JobTitle);
    }

    [Fact]
    public async Task Analytics_ShouldAggregateEmployees()
    {
        var service = CreateService(nameof(Analytics_ShouldAggregateEmployees), out _);

        await service.CreateAsync(new EmployeeCreateDto
        {
            Code = "EMP-003",
            FullName = "Active Employee",
            Status = EmployeeStatus.Active,
            BranchId = 1,
            DepartmentId = 10,
            JobTitle = "Cashier",
            HireDate = DateTime.Now
        });

        await service.CreateAsync(new EmployeeCreateDto
        {
            Code = "EMP-004",
            FullName = "Suspended Employee",
            Status = EmployeeStatus.Suspended,
            BranchId = 1,
            DepartmentId = 11,
            JobTitle = "Warehouse",
            HireDate = DateTime.Now.AddDays(-2)
        });

        var analytics = await service.GetAnalyticsAsync();

        Assert.True(analytics.Success);
        Assert.Equal(2, analytics.Data.TotalEmployees);
        Assert.Equal(1, analytics.Data.ActiveEmployees);
        Assert.Equal(1, analytics.Data.SuspendedEmployees);
        Assert.Equal(2, analytics.Data.CountByBranch[1]);
    }
}
