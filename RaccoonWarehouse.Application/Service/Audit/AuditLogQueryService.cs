using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Audit;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users;

namespace RaccoonWarehouse.Application.Service.Audit;

public sealed record AuditLogFilter(
    int PageNumber = 1,
    int PageSize = 50,
    int? UserId = null,
    string? Search = null,
    string? Action = null,
    DateTime? From = null,
    DateTime? To = null,
    bool? Succeeded = null);

public sealed class AuditLogReadModel
{
    public int Id { get; init; }
    public DateTime CreatedDate { get; init; }
    public int? UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public UserRole? Role { get; init; }
    public Guid SessionId { get; init; }
    public string Action { get; init; } = string.Empty;
    public string? PermissionKey { get; init; }
    public string? EntityType { get; init; }
    public int? EntityId { get; init; }
    public bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }
}

public interface IAuditLogQueryService
{
    Task<IPagedResult<AuditLogReadModel>> GetPagedAsync(AuditLogFilter filter, CancellationToken cancellationToken = default);
}

public sealed class AuditLogQueryService : IAuditLogQueryService
{
    private readonly ApplicationDbContext _dbContext;

    public AuditLogQueryService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IPagedResult<AuditLogReadModel>> GetPagedAsync(
        AuditLogFilter filter,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, filter.PageNumber);
        var size = Math.Clamp(filter.PageSize, 10, 200);
        IQueryable<AuditLog> query = _dbContext.Set<AuditLog>().AsNoTracking();

        if (filter.UserId.HasValue)
            query = query.Where(x => x.UserId == filter.UserId.Value);
        if (filter.From.HasValue)
            query = query.Where(x => x.CreatedDate >= filter.From.Value);
        if (filter.To.HasValue)
            query = query.Where(x => x.CreatedDate < filter.To.Value.Date.AddDays(1));
        if (filter.Succeeded.HasValue)
            query = query.Where(x => x.Succeeded == filter.Succeeded.Value);
        if (!string.IsNullOrWhiteSpace(filter.Action))
            query = query.Where(x => x.Action == filter.Action);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(x => x.Action.Contains(search)
                || (x.PermissionKey != null && x.PermissionKey.Contains(search))
                || (x.EntityType != null && x.EntityType.Contains(search))
                || (x.ErrorMessage != null && x.ErrorMessage.Contains(search)));
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(x => x.CreatedDate)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(x => new AuditLogReadModel
            {
                Id = x.Id,
                CreatedDate = x.CreatedDate,
                UserId = x.UserId,
                UserName = _dbContext.Set<User>()
                    .Where(user => x.UserId.HasValue && user.Id == x.UserId.Value)
                    .Select(user => user.Name)
                    .FirstOrDefault() ?? string.Empty,
                Role = x.Role,
                SessionId = x.SessionId,
                Action = x.Action,
                PermissionKey = x.PermissionKey,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                Succeeded = x.Succeeded,
                ErrorMessage = x.ErrorMessage
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogReadModel>(rows, total, page, size);
    }
}
