using AutoMapper;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Application.Service.Generic;
using RaccoonWarehouse.Domain.Checks;
using RaccoonWarehouse.Domain.Checks.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Application.Service.Checks
{
    public class CheckService : GenericService<Check, CheckWriteDto, CheckReadDto>, ICheckService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        public CheckService(ApplicationDbContext context, IUOW uow, IMapper mapper) : base(context, uow, mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }
    }
    public interface ICheckService : IGenericService<Check, CheckWriteDto, CheckReadDto>
    {
    }
}
