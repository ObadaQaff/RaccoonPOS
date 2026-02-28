using AutoMapper;
using RaccoonWarehouse.Application.Service.Generic;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Products.DTOs;

using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
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
    }
    public interface IProductUnitService : IGenericService<ProductUnit, ProductUnitWriteDto, ProductUnitReadDto>
    {

    }
}
