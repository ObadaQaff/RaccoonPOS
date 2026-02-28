using RaccoonWarehouse.Domain.Cashiers.DTOs;
using RaccoonWarehouse.Domain.Users.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Application.Service.Users
{
    public class UserSession : IUserSession
    {
        public UserReadDto? CurrentUser { get; private set; }
        public CashierSessionReadDto? CurrentCashierSession { get; private set; }

        public bool IsLoggedIn => CurrentUser != null;

        public void StartSession(UserReadDto user/*, CashierSessionReadDto cashierSession*/)
        {
            CurrentUser = user;
            //CurrentCashierSession = cashierSession;
        }

         public void StartUserSession(UserReadDto user)
        {
            CurrentUser = user;
            CurrentCashierSession = null;
        }

        public void AttachCashierSession(CashierSessionReadDto session)
        {
            CurrentCashierSession = session;

        }

        public void EndSession()
        {
            CurrentUser = null;
            CurrentCashierSession = null;
        }
    }

    public interface IUserSession
    {
        bool IsLoggedIn { get; }

        UserReadDto? CurrentUser { get; }
        CashierSessionReadDto? CurrentCashierSession { get; }
    
        void StartUserSession(UserReadDto user);
        void AttachCashierSession(CashierSessionReadDto session);
        void StartSession(UserReadDto user);
        void EndSession();
    }

}
