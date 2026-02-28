using AutoMapper;
using RaccoonWarehouse.Application.Service.Generic;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Categories;
using RaccoonWarehouse.Domain.Categories.DTOs;
using RaccoonWarehouse.Domain.SubCategories;
using RaccoonWarehouse.Domain.SubCategories.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Application.Service.SubCategories
{
    public class SubCategoryService : GenericService<SubCategory, SubCategoryWriteDto, SubCategoryReadDto>, ISubCategoryService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        public SubCategoryService(ApplicationDbContext context, IUOW uow, IMapper mapper) : base(context, uow, mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }
    }
    public interface ISubCategoryService : IGenericService<SubCategory, SubCategoryWriteDto, SubCategoryReadDto>
    {

    }
}
