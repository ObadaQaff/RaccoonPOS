using AutoMapper;
using RaccoonWarehouse.Application.Service.Generic;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.InvoiceLines;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Application.Service.InvoiceLines
{
    public class InvoiceLineService : GenericService<InvoiceLine, InvoiceLineWriteDto, InvoiceLineReadDto>, IInvoiceLineService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        public InvoiceLineService(ApplicationDbContext context, IUOW uow, IMapper mapper) : base(context, uow, mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }
    }
    public interface IInvoiceLineService : IGenericService<InvoiceLine, InvoiceLineWriteDto, InvoiceLineReadDto>
    {

    }
}
