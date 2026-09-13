using Microsoft.Extensions.Logging;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Core.Audit;

namespace RaccoonWarehouse.Application.Service.Audit
{
    public sealed class AuditLogService : IAuditLogService
    {
        private readonly ILogger<AuditLogService> _logger;
        private readonly IUserSession _userSession;

        public AuditLogService(
            ILogger<AuditLogService> logger,
            IUserSession userSession)
        {
            _logger = logger;
            _userSession = userSession;
        }

        public Task WriteAsync(AuditEvent auditEvent)
        {
            ArgumentNullException.ThrowIfNull(auditEvent);

            var user = _userSession.CurrentUser;
            var level = auditEvent.Succeeded ? LogLevel.Information : LogLevel.Warning;
            _logger.Log(
                level,
                "Audit action {Action}; Permission={PermissionKey}; Entity={EntityType}:{EntityId}; " +
                "Succeeded={Succeeded}; UserId={UserId}; Role={Role}; SessionId={SessionId}; Error={ErrorMessage}",
                auditEvent.Action,
                auditEvent.PermissionKey,
                auditEvent.EntityType,
                auditEvent.EntityId,
                auditEvent.Succeeded,
                user?.Id,
                user?.Role,
                _userSession.SessionId,
                auditEvent.ErrorMessage);

            return Task.CompletedTask;
        }
    }
}
