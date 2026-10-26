using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Accounting.TaxRates;
using RaccoonWarehouse.Domain.Accounting.TaxRates.DTOs;
namespace RaccoonWarehouse.Application.Service.Accounting;
public interface ITaxService
{
    Task<TaxRate> CreateAsync(string name, decimal rate, int taxAccountId, TaxType taxType);
    Task UpdateAsync(int id, string name, decimal rate, int taxAccountId, TaxType taxType, bool isActive);
    Task DeactivateAsync(int id);
    Task<List<TaxRate>> GetAllAsync();
    Task<decimal> CalculateTaxAsync(decimal amount, int taxRateId);
    Task<TaxReportDto> GetTaxReportAsync(DateTime fromDate, DateTime toDate);
}
