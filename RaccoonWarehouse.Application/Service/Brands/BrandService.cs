using AutoMapper;
using RaccoonWarehouse.Application.Service.Generic;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Brands;
using RaccoonWarehouse.Domain.Brands.DTOs;
using RaccoonWarehouse.Domain.Categories;
using RaccoonWarehouse.Domain.Categories.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Application.Service.Brands
{
    public class BrandService : GenericService<Brand, BrandWriteDto, BrandReadDto>, IBrandService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        public BrandService(ApplicationDbContext context, IUOW uow, IMapper mapper) : base(context, uow, mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }
    }
    public interface IBrandService : IGenericService<Brand, BrandWriteDto, BrandReadDto>
    {

    }
}
