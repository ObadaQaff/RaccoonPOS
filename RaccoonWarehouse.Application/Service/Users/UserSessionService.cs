using RaccoonWarehouse.Domain.Cashiers.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users.DTOs;

namespace RaccoonWarehouse.Application.Service.Users
{
    public class UserSession : IUserSession
    {
        private Guid _sessionId = Guid.NewGuid();

        public Guid SessionId => _sessionId;
        public UserReadDto? CurrentUser { get; private set; }
        public CashierSessionReadDto? CurrentCashierSession { get; private set; }

        public int? CurrentUserId => CurrentUser?.Id;
        public UserRole? CurrentRole => CurrentUser?.Role;
        public int? CurrentCashierSessionId => CurrentCashierSession?.Id;

        public bool IsLoggedIn => CurrentUser != null;
        public bool HasActiveCashierSession => CurrentCashierSession != null;

        public event EventHandler? Changed;

        public void SetCurrentUser(UserReadDto user)
        {
            ArgumentNullException.ThrowIfNull(user);

            CurrentUser = user;
            CurrentCashierSession = null;
            StartNewApplicationSession();
            NotifyChanged();
        }

        public void AttachCashierSession(CashierSessionReadDto session)
        {
            if (CurrentUser == null)
                throw new InvalidOperationException("A user session must exist before attaching a cashier session.");

            CurrentCashierSession = session;
            NotifyChanged();
        }

        public void ClearCashierSession()
        {
            CurrentCashierSession = null;
            NotifyChanged();
        }

        public void EndSession()
        {
            CurrentUser = null;
            CurrentCashierSession = null;
            StartNewApplicationSession();
            NotifyChanged();
        }

        private void StartNewApplicationSession()
        {
            _sessionId = Guid.NewGuid();
        }

        private void NotifyChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public interface IUserSession
    {
        Guid SessionId { get; }
        bool IsLoggedIn { get; }
        bool HasActiveCashierSession { get; }
        UserReadDto? CurrentUser { get; }
        CashierSessionReadDto? CurrentCashierSession { get; }
        int? CurrentUserId { get; }
        UserRole? CurrentRole { get; }
        int? CurrentCashierSessionId { get; }

        event EventHandler? Changed;

        void SetCurrentUser(UserReadDto user);
        void AttachCashierSession(CashierSessionReadDto session);
        void ClearCashierSession();
        void EndSession();
    }
}
