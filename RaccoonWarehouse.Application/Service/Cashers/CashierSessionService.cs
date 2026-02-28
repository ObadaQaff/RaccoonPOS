using AutoMapper;
using RaccoonWarehouse.Application.Service.Generic;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Cashiers;
using RaccoonWarehouse.Domain.Cashiers.DTOs;
using RaccoonWarehouse.Domain.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Application.Service.Cashers
{
    public class CashierSessionService
        : GenericService<CashierSession, CashierSessionWriteDto, CashierSessionReadDto>,
          ICashierSessionService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;

        public CashierSessionService(ApplicationDbContext context, IUOW uow, IMapper mapper)
            : base(context, uow, mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<CashierSessionReadDto> OpenSessionAsync(int cashierId, decimal startBalance)
        {
            if (startBalance < 0)
                throw new Exception("Start balance cannot be negative.");

            // ✅ Prevent multiple open sessions for same cashier
            var hasOpen = _uow.CashierSessions
                .GetAllAsQueryable()
                .Any(s => s.CashierId == cashierId && s.Status == SessionStatus.Open);

            if (hasOpen)
                throw new Exception("There is already an open session for this cashier.");

            var session = new CashierSession
            {
                CashierId = cashierId,
                OpenedAt = DateTime.Now,
                Status = SessionStatus.Open,
                StatrBalance = startBalance,
                EndingBalance = 0,
                ClosedAt = null
            };

            await _uow.CashierSessions.AddAsync(session);
            await _uow.CommitAsync();

            return _mapper.Map<CashierSessionReadDto>(session);
        }

        public async Task CloseSessionAsync(int sessionId, decimal endingBalance)
        {
            if (endingBalance < 0)
                throw new Exception("Ending balance cannot be negative.");

            var session = await _uow.CashierSessions.GetByIdAsync(sessionId);

            if (session == null)
                throw new Exception("Session not found.");

            if (session.Status == SessionStatus.Closed)
                throw new Exception("Session already closed.");

            session.ClosedAt = DateTime.Now;
            session.EndingBalance = endingBalance;
            session.Status = SessionStatus.Closed;

            await _uow.CommitAsync();
        }

        // ✅ Optional: get current open session for cashier
        public Task<CashierSessionReadDto?> GetOpenSessionAsync(int cashierId)
        {
            var session = _uow.CashierSessions
                .GetAllAsQueryable()
                .Where(s => s.CashierId == cashierId && s.Status == SessionStatus.Open)
                .OrderByDescending(s => s.OpenedAt)
                .FirstOrDefault();

            return Task.FromResult(session == null ? null : _mapper.Map<CashierSessionReadDto>(session));
        }



        public async Task<CashierSessionReadDto?> GetOpenSessionByCashierAsync(int cashierId)
        {
            var session = _uow.CashierSessions
                .GetAllAsQueryable()
                .Where(s => s.CashierId == cashierId && s.Status == SessionStatus.Open)
                .OrderByDescending(s => s.OpenedAt)
                .FirstOrDefault();

            return session == null ? null : _mapper.Map<CashierSessionReadDto>(session);
        }

    }

    public interface ICashierSessionService
        : IGenericService<CashierSession, CashierSessionWriteDto, CashierSessionReadDto>
    {
        Task<CashierSessionReadDto> OpenSessionAsync(int cashierId, decimal startBalance);
        Task CloseSessionAsync(int sessionId, decimal endingBalance);

        Task<CashierSessionReadDto?> GetOpenSessionAsync(int cashierId);
        Task<CashierSessionReadDto?> GetOpenSessionByCashierAsync(int cashierId);

    }
}
