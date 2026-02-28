using RaccoonWarehouse.Domain.Cashiers.DTOs;
using RaccoonWarehouse.Domain.Users.DTOs;

namespace RaccoonWarehouse.Application.Service.Users
{
    public class UserSession : IUserSession
    {
        public UserReadDto? CurrentUser { get; private set; }
        public CashierSessionReadDto? CurrentCashierSession { get; private set; }

        public bool IsLoggedIn => CurrentUser != null;
        public bool HasActiveCashierSession => CurrentCashierSession != null;

        public void SetCurrentUser(UserReadDto user)
        {
            CurrentUser = user;
            CurrentCashierSession = null;
        }

        public void AttachCashierSession(CashierSessionReadDto session)
        {
            if (CurrentUser == null)
                throw new InvalidOperationException("A user session must exist before attaching a cashier session.");

            CurrentCashierSession = session;
        }

        public void ClearCashierSession()
        {
            CurrentCashierSession = null;
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
        bool HasActiveCashierSession { get; }
        UserReadDto? CurrentUser { get; }
        CashierSessionReadDto? CurrentCashierSession { get; }

        void SetCurrentUser(UserReadDto user);
        void AttachCashierSession(CashierSessionReadDto session);
        void ClearCashierSession();
        void EndSession();
    }
}
