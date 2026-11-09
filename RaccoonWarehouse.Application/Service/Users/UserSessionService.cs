using RaccoonWarehouse.Domain.Cashiers.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users.DTOs;

namespace RaccoonWarehouse.Application.Service.Users
{
    public class UserSession : IUserSession
    {
        private Guid _sessionId = Guid.NewGuid();
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> _permissionCache = new(StringComparer.OrdinalIgnoreCase);
        public static event Action? PermissionsChanged;
        private static long _permissionVersion;
        public static long PermissionVersion => System.Threading.Interlocked.Read(ref _permissionVersion);
        private long _observedPermissionVersion;

        private void ObservePermissionVersion()
        {
            var version = System.Threading.Interlocked.Read(ref _permissionVersion);
            if (version == _observedPermissionVersion) return;
            _permissionCache.Clear();
            _observedPermissionVersion = version;
        }

        public Guid SessionId => _sessionId;
        public UserReadDto? CurrentUser { get; private set; }
        public CashierSessionReadDto? CurrentCashierSession { get; private set; }

        public int? CurrentUserId => CurrentUser?.Id;
        public UserRole? CurrentRole => CurrentUser?.Role;
        public int? CurrentCashierSessionId => CurrentCashierSession?.Id;

        public bool IsLoggedIn => CurrentUser != null;
        public bool HasActiveCashierSession => CurrentCashierSession != null;

        public event EventHandler? Changed;

        public bool TryGetCachedPermission(string permissionKey, out bool isAllowed)
        {
            ObservePermissionVersion();
            return _permissionCache.TryGetValue(permissionKey, out isAllowed);
        }

        public void CachePermissions(IEnumerable<KeyValuePair<string, bool>> permissions)
        {
            ObservePermissionVersion();
            foreach (var permission in permissions)
                _permissionCache[permission.Key] = permission.Value;
        }

        public void ClearPermissionCache()
        {
            System.Threading.Interlocked.Increment(ref _permissionVersion);
            ObservePermissionVersion();
            PermissionsChanged?.Invoke();
        }

        public void SetCurrentUser(UserReadDto user)
        {
            ArgumentNullException.ThrowIfNull(user);

            CurrentUser = user;
            CurrentCashierSession = null;
            ClearPermissionCache();
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
            ClearPermissionCache();
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

        bool TryGetCachedPermission(string permissionKey, out bool isAllowed);
        void CachePermissions(IEnumerable<KeyValuePair<string, bool>> permissions);
        void ClearPermissionCache();

        void SetCurrentUser(UserReadDto user);
        void AttachCashierSession(CashierSessionReadDto session);
        void ClearCashierSession();
        void EndSession();
    }
}
