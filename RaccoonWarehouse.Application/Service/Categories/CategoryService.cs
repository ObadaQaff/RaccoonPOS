using AutoMapper;
using RaccoonWarehouse.Application.Service.Generic;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Categories;
using RaccoonWarehouse.Domain.Categories.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Application.Service.Categories
{
    public class CategoryService:  GenericService<Category, CategoryWriteDto, CategoryReadDto> , ICategoryService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        public CategoryService(ApplicationDbContext context, IUOW uow, IMapper mapper) : base(context, uow, mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }
    }
    public interface ICategoryService : IGenericService<Category, CategoryWriteDto, CategoryReadDto>
    {

    }
}
