using AutoMapper;
using RaccoonWarehouse.Application.Service.Generic;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Units;
using RaccoonWarehouse.Domain.Units.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Application.Service.Units
{
    public class UnitService : GenericService<Unit, UnitWriteDto, UnitReadDto>, IUnitService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        public UnitService(ApplicationDbContext context, IUOW uow, IMapper mapper) : base(context, uow, mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }
    }
    public interface IUnitService : IGenericService<Unit, UnitWriteDto, UnitReadDto>
    {

    }
}
