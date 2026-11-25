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
                await SyncBarcodesAsync(dto, result.Data?.Id ?? dto.Id);
                if (_partialSaleCatalogRefreshQueue != null)
                    _partialSaleCatalogRefreshQueue.Enqueue(new[] { dto.ProductId });
                else if (_partialSaleCatalog != null)
                {
                    await _partialSaleCatalog.RefreshProductsAsync(new[] { dto.ProductId });
                    await _partialSaleContext.SaveChangesAsync();
                }
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
                await SyncBarcodesAsync(dto, dto.Id);
                if (_partialSaleCatalogRefreshQueue != null)
                    _partialSaleCatalogRefreshQueue.Enqueue(new[] { dto.ProductId });
                else if (_partialSaleCatalog != null)
                {
                    await _partialSaleCatalog.RefreshProductsAsync(new[] { dto.ProductId });
                    await _partialSaleContext.SaveChangesAsync();
                }
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
                // Partial quantities use three decimal places as the piece
                // count: 0.001 = 1 piece, 0.010 = 10 pieces.
                var partCount = (input - packages) * 1000m;
                if (partCount == 0m || partCount != decimal.Truncate(partCount))
                    return false;
                tapes = checked(packages * partsPerPackage + partCount);
                return tapes > 0m;
            }
            catch (OverflowException) { return false; }
        }

        public static bool TryConvertPartialQuantityToBaseQuantity(
            decimal input,
            decimal partsPerPackage,
            decimal quantityPerUnit,
            out decimal baseQuantity)
        {
            baseQuantity = 0m;
            if (quantityPerUnit <= 0m ||
                !TryConvertPartialQuantity(input, partsPerPackage, out var pieces))
                return false;

            baseQuantity = Math.Round(
                pieces / partsPerPackage * quantityPerUnit,
                6,
                MidpointRounding.AwayFromZero);
            return baseQuantity > 0m;
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
                if (IsTapeUnitName(source.Unit?.Name))
                    return Result<List<ProductUnitReadDto>>.Fail(
                        "Partial sale cannot be configured for the Tape unit.");
                if (source.QuantityPerUnit <= 0m)
                    return Result<List<ProductUnitReadDto>>.Fail(
                        "وحدة المنتج لا تحتوي على كمية صحيحة. / The product unit has no valid quantity.");

                // POS may configure partial sale the first time it is needed.
                // Persist the catalog permission together with its pricing data.
                source.AllowFractionalSale = true;
                source.FractionalPartsPerUnit = partsPerPackage;
                source.FractionalPartName = string.IsNullOrWhiteSpace(source.FractionalPartName)
                    ? "Piece"
                    : source.FractionalPartName;
                source.FractionalSalePrice = fractionalSalePrice;
                source.FractionalSaleCost = fractionalSaleCost;
                source.UpdatedDate = DateTime.Now;
                await _partialSaleContext.SaveChangesAsync();
                var savedUnits = await _partialSaleContext.Set<ProductUnit>().AsNoTracking()
                    .Include(unit => unit.Unit)
                    .Include(unit => unit.Barcodes)
                    .Where(unit => unit.ProductId == source.ProductId).ToListAsync();
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
            var barcodes = NormalizeAlternateBarcodes(dto);
            if (barcodes.Count == 0)
                return null;

            if (barcodes.Count > 10)
                return "يمكن إضافة 10 رموز مماثلة كحد أقصى لكل وحدة / A maximum of 10 alternate barcodes is allowed per unit.";

            if (barcodes.Count != barcodes.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                return "لا يمكن تكرار الرموز المماثلة / Alternate barcodes must be unique.";

            var duplicateUnit = await _uow.GetRepository<ProductUnitBarcode>().GetAllAsQueryable()
                .Where(barcode => barcode.ProductUnitId != dto.Id)
                .AnyAsync(barcode => barcodes.Contains(barcode.NormalizedBarcode));
            if (duplicateUnit)
                return "الرمز المماثل مستخدم بالفعل / An alternate barcode is already in use.";

            var productBarcode = await _uow.GetRepository<Product>()
                .GetAllAsQueryable()
                .AnyAsync(product => product.ITEMCODE.HasValue && barcodes.Contains(product.ITEMCODE.Value.ToString()));
            return productBarcode
                ? "الرمز المماثل مستخدم كرمز رئيسي / An alternate barcode is already used as a primary barcode."
                : null;
        }

        private async Task SyncBarcodesAsync(ProductUnitWriteDto dto, int productUnitId)
        {
            if (productUnitId <= 0)
                return;

            var repository = _uow.GetRepository<ProductUnitBarcode>();
            var existing = await repository.GetAllAsQueryable()
                .Where(barcode => barcode.ProductUnitId == productUnitId)
                .ToListAsync();
            foreach (var barcode in existing)
                await repository.DeleteAsync(barcode.Id);

            foreach (var barcode in NormalizeAlternateBarcodes(dto))
                await repository.AddAsync(new ProductUnitBarcode
                {
                    ProductUnitId = productUnitId,
                    Barcode = barcode,
                    NormalizedBarcode = barcode
                });

            await _uow.CommitAsync();
        }

        private static List<string> NormalizeAlternateBarcodes(ProductUnitWriteDto dto)
        {
            var values = dto.AlternateBarcodes ?? new List<string>();
            if (values.Count == 0 && !string.IsNullOrWhiteSpace(dto.AlternateBarcode))
                values.Add(dto.AlternateBarcode);
            return values
                .Select(NormalizeAlternateBarcode)
                .Where(value => value != null)
                .Select(value => value!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(10)
                .ToList();
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
