using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using System.Collections.Generic;
using System.Linq;

namespace RaccoonWarehouse.Domain.ProductUnits
{
    public static class ProductUnitSelector
    {
        public static ProductUnit? GetBaseUnit(IEnumerable<ProductUnit>? units)
        {
            var list = units?.ToList();
            if (list == null || list.Count == 0)
                return null;

            return list.FirstOrDefault(u => u.IsBaseUnit) ?? list.First();
        }

        public static ProductUnit? GetDefaultSaleUnit(IEnumerable<ProductUnit>? units)
        {
            var list = units?.ToList();
            if (list == null || list.Count == 0)
                return null;

            return list.FirstOrDefault(u => u.IsDefaultSaleUnit)
                   ?? GetBaseUnit(list)
                   ?? list.First();
        }

        public static ProductUnit? GetDefaultPurchaseUnit(IEnumerable<ProductUnit>? units)
        {
            var list = units?.ToList();
            if (list == null || list.Count == 0)
                return null;

            return list.FirstOrDefault(u => u.IsDefaultPurchaseUnit)
                   ?? GetBaseUnit(list)
                   ?? list.First();
        }

        public static ProductUnitReadDto? GetBaseUnit(IEnumerable<ProductUnitReadDto>? units)
        {
            var list = units?.ToList();
            if (list == null || list.Count == 0)
                return null;

            return list.FirstOrDefault(u => u.IsBaseUnit) ?? list.First();
        }

        public static ProductUnitReadDto? GetDefaultSaleUnit(IEnumerable<ProductUnitReadDto>? units)
        {
            var list = units?.ToList();
            if (list == null || list.Count == 0)
                return null;

            return list.FirstOrDefault(u => u.IsDefaultSaleUnit)
                   ?? GetBaseUnit(list)
                   ?? list.First();
        }

        public static ProductUnitReadDto? GetDefaultPurchaseUnit(IEnumerable<ProductUnitReadDto>? units)
        {
            var list = units?.ToList();
            if (list == null || list.Count == 0)
                return null;

            return list.FirstOrDefault(u => u.IsDefaultPurchaseUnit)
                   ?? GetBaseUnit(list)
                   ?? list.First();
        }

        public static ProductUnitWriteDto? GetBaseUnit(IEnumerable<ProductUnitWriteDto>? units)
        {
            var list = units?.ToList();
            if (list == null || list.Count == 0)
                return null;

            return list.FirstOrDefault(u => u.IsBaseUnit) ?? list.First();
        }

        public static ProductUnitWriteDto? GetDefaultSaleUnit(IEnumerable<ProductUnitWriteDto>? units)
        {
            var list = units?.ToList();
            if (list == null || list.Count == 0)
                return null;

            return list.FirstOrDefault(u => u.IsDefaultSaleUnit)
                   ?? GetBaseUnit(list)
                   ?? list.First();
        }

        public static ProductUnitWriteDto? GetDefaultPurchaseUnit(IEnumerable<ProductUnitWriteDto>? units)
        {
            var list = units?.ToList();
            if (list == null || list.Count == 0)
                return null;

            return list.FirstOrDefault(u => u.IsDefaultPurchaseUnit)
                   ?? GetBaseUnit(list)
                   ?? list.First();
        }
    }
}
