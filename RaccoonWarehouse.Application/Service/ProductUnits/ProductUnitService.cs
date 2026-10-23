using AutoMapper;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Application.Service.Stocks;
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
        private readonly ApplicationDbContext _partialSaleContext;
        private readonly IPosProductCatalogService? _partialSaleCatalog;
        private readonly IPosProductCatalogRefreshQueue? _partialSaleCatalogRefreshQueue;
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        public ProductUnitService(ApplicationDbContext context, IUOW uow, IMapper mapper,
            IPosProductCatalogService? partialSaleCatalog = null,
            IPosProductCatalogRefreshQueue? partialSaleCatalogRefreshQueue = null) : base(context, uow, mapper)
        {
            _partialSaleContext = context;
            _partialSaleCatalog = partialSaleCatalog;
            _partialSaleCatalogRefreshQueue = partialSaleCatalogRefreshQueue;
            _uow = uow;
            _mapper = mapper;
        }

        public override async Task<RaccoonWarehouse.Core.Common.Result<ProductUnitWriteDto>> CreateAsync(ProductUnitWriteDto dto)
        {
            var validation = await ValidateAlternateBarcodeAsync(dto);
            if (validation != null)
                return RaccoonWarehouse.Core.Common.Result<ProductUnitWriteDto>.Fail(validation);

            dto.AlternateBarcode = NormalizeAlternateBarcode(dto.AlternateBarcode);
            var result = await base.CreateAsync(dto);
            if (result.Success)
            {
                if (_partialSaleCatalog != null)
                    await _partialSaleCatalog.RefreshProductsAsync(new[] { dto.ProductId });
                await _partialSaleContext.SaveChangesAsync();
                NotifyUnitsChanged(dto.ProductId);
            }
            return result;
        }

        public override async Task<RaccoonWarehouse.Core.Common.Result<ProductUnitWriteDto>> UpdateAsync(ProductUnitWriteDto dto)
        {
            var validation = await ValidateAlternateBarcodeAsync(dto);
            if (validation != null)
                return RaccoonWarehouse.Core.Common.Result<ProductUnitWriteDto>.Fail(validation);

            dto.AlternateBarcode = NormalizeAlternateBarcode(dto.AlternateBarcode);
            var result = await base.UpdateAsync(dto);
            if (result.Success)
            {
                if (_partialSaleCatalog != null)
                    await _partialSaleCatalog.RefreshProductsAsync(new[] { dto.ProductId });
                await _partialSaleContext.SaveChangesAsync();
                NotifyUnitsChanged(dto.ProductId);
            }
            return result;
        }

        public static event Action<int, IReadOnlyList<ProductUnitReadDto>?>? UnitsChanged;

        public static void NotifyUnitsChanged(int productId, IReadOnlyList<ProductUnitReadDto>? units = null)
        {
            if (productId <= 0 || UnitsChanged == null)
                return;
            foreach (Action<int, IReadOnlyList<ProductUnitReadDto>?> listener in UnitsChanged.GetInvocationList())
            {
                try { listener(productId, units); }
                catch (Exception exception) { System.Diagnostics.Debug.WriteLine(exception); }
            }
        }

        public static bool IsTapeUnitName(string? name)
            => string.Equals(name?.Trim(), "Tape", StringComparison.OrdinalIgnoreCase)
               || string.Equals(name?.Trim(), "شريط", StringComparison.OrdinalIgnoreCase);

        public static bool TryConvertPartialQuantity(decimal input, decimal partsPerPackage, out decimal tapes)
        {
            tapes = 0m;
            if (input <= 0m || partsPerPackage < 2m || partsPerPackage != decimal.Truncate(partsPerPackage))
                return false;
            try
            {
                var packages = decimal.Truncate(input);
                var partCount = (input - packages) * 10m;
                if (partCount == 0m || partCount != decimal.Truncate(partCount))
                    return false;
                tapes = checked(packages * partsPerPackage + partCount);
                return tapes > 0m;
            }
            catch (OverflowException) { return false; }
        }

        public async Task<Result<List<ProductUnitReadDto>>> ConfigureFractionalSaleAsync(
            int sourceProductUnitId, decimal partsPerPackage, decimal fractionalSalePrice,
            decimal fractionalSaleCost)
        {
            if (partsPerPackage < 2m || partsPerPackage != decimal.Truncate(partsPerPackage) ||
                partsPerPackage > 999_999_999_999m || fractionalSalePrice <= 0m ||
                fractionalSalePrice > 999_999_999_999m ||
                fractionalSalePrice != Math.Round(fractionalSalePrice, 3, MidpointRounding.AwayFromZero) ||
                fractionalSaleCost < 0m || fractionalSaleCost > 999_999_999_999m ||
                fractionalSaleCost != Math.Round(fractionalSaleCost, 3, MidpointRounding.AwayFromZero))
                return Result<List<ProductUnitReadDto>>.Fail(
                    "إعدادات البيع الجزئي غير صالحة. / Invalid partial-sale settings.");

            try
            {
                await using var transaction = await _partialSaleContext.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);
                var source = await _partialSaleContext.Set<ProductUnit>()
                    .Include(unit => unit.Product).Include(unit => unit.Unit)
                    .SingleOrDefaultAsync(unit => unit.Id == sourceProductUnitId);
                if (source == null || source.Product == null || source.Product.IsDeleted)
                    return Result<List<ProductUnitReadDto>>.Fail("الوحدة غير موجودة. / Product unit not found.");
                await _partialSaleContext.Entry(source).ReloadAsync();
                if (!source.AllowFractionalSale || IsTapeUnitName(source.Unit?.Name))
                    return Result<List<ProductUnitReadDto>>.Fail(
                        "البيع الجزئي غير مسموح لهذه الوحدة. / Partial sale is not allowed for this unit.");
                if (source.QuantityPerUnit <= 0m)
                    return Result<List<ProductUnitReadDto>>.Fail(
                        "وحدة المنتج لا تحتوي على كمية صحيحة. / The product unit has no valid quantity.");

                source.FractionalPartsPerUnit = partsPerPackage;
                source.FractionalPartName = string.IsNullOrWhiteSpace(source.FractionalPartName)
                    ? "Piece"
                    : source.FractionalPartName;
                source.FractionalSalePrice = fractionalSalePrice;
                source.FractionalSaleCost = fractionalSaleCost;
                source.UpdatedDate = DateTime.Now;
                await _partialSaleContext.SaveChangesAsync();
                var savedUnits = await _partialSaleContext.Set<ProductUnit>().AsNoTracking()
                    .Include(unit => unit.Unit).Where(unit => unit.ProductId == source.ProductId).ToListAsync();
                var result = _mapper.Map<List<ProductUnitReadDto>>(savedUnits);
                await transaction.CommitAsync();
                if (_partialSaleCatalogRefreshQueue != null)
                    _partialSaleCatalogRefreshQueue.Enqueue(new[] { source.ProductId });
                else if (_partialSaleCatalog != null)
                    await _partialSaleCatalog.RefreshProductsAsync(new[] { source.ProductId });
                NotifyUnitsChanged(source.ProductId, result);
                return Result<List<ProductUnitReadDto>>.Ok(result);
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception);
                return Result<List<ProductUnitReadDto>>.Fail(
                    "تعذر حفظ إعدادات البيع الجزئي. لم يتم تطبيق البيع الجزئي. / Could not save partial-sale settings.");
            }
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
        Task<Result<List<ProductUnitReadDto>>> ConfigureFractionalSaleAsync(
            int sourceProductUnitId, decimal partsPerPackage, decimal fractionalSalePrice,
            decimal fractionalSaleCost);
    }
}
