using Microsoft.Extensions.Logging;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Core.Audit;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Audit;

namespace RaccoonWarehouse.Application.Service.Audit
{
    public sealed class AuditLogService : IAuditLogService
    {
        private readonly ILogger<AuditLogService> _logger;
        private readonly IUserSession _userSession;
        private readonly ApplicationDbContext _dbContext;

        public AuditLogService(
            ILogger<AuditLogService> logger,
            IUserSession userSession,
            ApplicationDbContext dbContext)
        {
            _logger = logger;
            _userSession = userSession;
            _dbContext = dbContext;
        }

        public async Task WriteAsync(AuditEvent auditEvent)
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

            try
            {
                _dbContext.AuditLogs.Add(new AuditLog
                {
                    UserId = user?.Id,
                    Role = user?.Role,
                    SessionId = _userSession.SessionId,
                    Action = auditEvent.Action,
                    PermissionKey = auditEvent.PermissionKey,
                    EntityType = auditEvent.EntityType,
                    EntityId = auditEvent.EntityId,
                    Succeeded = auditEvent.Succeeded,
                    ErrorMessage = auditEvent.ErrorMessage,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                });

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Audit storage must not block login, checkout, or permission changes
                // when an older installation has not applied the audit migration yet.
                _logger.LogWarning(ex, "Audit persistence failed for action {Action}", auditEvent.Action);
            }

        }
    }
}
