using AutoMapper;
using RaccoonWarehouse.Application.Service.Generic;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Products.DTOs;

using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using RaccoonWarehouse.Domain.Products;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Application.Service.ProductUnits
{
    public class ProductUnitService : GenericService<ProductUnit, ProductUnitWriteDto, ProductUnitReadDto>, IProductUnitService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        public ProductUnitService(ApplicationDbContext context, IUOW uow, IMapper mapper) : base(context, uow, mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public override async Task<RaccoonWarehouse.Core.Common.Result<ProductUnitWriteDto>> CreateAsync(ProductUnitWriteDto dto)
        {
            var validation = await ValidateAlternateBarcodeAsync(dto);
            if (validation != null)
                return RaccoonWarehouse.Core.Common.Result<ProductUnitWriteDto>.Fail(validation);

            dto.AlternateBarcode = NormalizeAlternateBarcode(dto.AlternateBarcode);
            return await base.CreateAsync(dto);
        }

        public override async Task<RaccoonWarehouse.Core.Common.Result<ProductUnitWriteDto>> UpdateAsync(ProductUnitWriteDto dto)
        {
            var validation = await ValidateAlternateBarcodeAsync(dto);
            if (validation != null)
                return RaccoonWarehouse.Core.Common.Result<ProductUnitWriteDto>.Fail(validation);

            dto.AlternateBarcode = NormalizeAlternateBarcode(dto.AlternateBarcode);
            return await base.UpdateAsync(dto);
        }

        private async Task<string?> ValidateAlternateBarcodeAsync(ProductUnitWriteDto dto)
        {
            var barcode = NormalizeAlternateBarcode(dto.AlternateBarcode);
            if (barcode == null)
                return null;

            var duplicateUnit = await _uow.GetRepository<ProductUnit>().GetAllAsQueryable()
                .Where(unit => unit.Id != dto.Id && unit.AlternateBarcode != null)
                .AnyAsync(unit => unit.AlternateBarcode == barcode);
            if (duplicateUnit)
                return "الرمز المماثل مستخدم بالفعل / An alternate barcode is already in use.";

            var productBarcode = await _uow.GetRepository<Product>()
                .GetAllAsQueryable()
                .AnyAsync(product => product.ITEMCODE.HasValue && product.ITEMCODE.Value.ToString() == barcode);
            return productBarcode
                ? "الرمز المماثل مستخدم كرمز رئيسي / An alternate barcode is already used as a primary barcode."
                : null;
        }

        private static string? NormalizeAlternateBarcode(string? barcode)
            => string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim();
    }
    public interface IProductUnitService : IGenericService<ProductUnit, ProductUnitWriteDto, ProductUnitReadDto>
    {

    }
}
