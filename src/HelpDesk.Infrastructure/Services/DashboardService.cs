using HelpDesk.Application.DTOs.Dashboard;
using HelpDesk.Application.Exceptions;
using HelpDesk.Application.Interfaces;
using HelpDesk.Domain.Enums;
using HelpDesk.Infrastructure.Persistence;
using HelpDesk.Infrastructure.Services.Mapping;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public DashboardService(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<DashboardResponse> GetAsync(CancellationToken cancellationToken = default)
    {
        var role = CurrentRole;
        var userId = RequireCurrentUserId();

        var scope = _dbContext.Tickets.AsNoTracking().AsQueryable();

        if (role == UserRole.Employee)
        {
            scope = scope.Where(t => t.CreatedById == userId);
        }

        var statusGroups = await scope
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var priorityGroups = await scope
            .GroupBy(t => t.Priority)
            .Select(g => new { Priority = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var categoryGroups = await scope
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var categoryNames = await _dbContext.Categories
            .AsNoTracking()
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        var byStatus = statusGroups
            .Select(g => new StatusCount { Status = g.Status, Count = g.Count })
            .OrderBy(s => s.Status)
            .ToList();

        var byPriority = priorityGroups
            .Select(g => new PriorityCount { Priority = g.Priority, Count = g.Count })
            .OrderBy(p => p.Priority)
            .ToList();

        var byCategory = categoryGroups
            .Select(g => new CategoryCount
            {
                CategoryId = g.CategoryId,
                CategoryName = categoryNames.TryGetValue(g.CategoryId, out var name) ? name : "Unknown",
                Count = g.Count
            })
            .OrderByDescending(c => c.Count)
            .ToList();

        var stats = new TicketStats
        {
            Total = await scope.CountAsync(cancellationToken),
            New = CountFor(byStatus, TicketStatus.New),
            Assigned = CountFor(byStatus, TicketStatus.Assigned),
            InProgress = CountFor(byStatus, TicketStatus.InProgress),
            Resolved = CountFor(byStatus, TicketStatus.Resolved),
            Closed = CountFor(byStatus, TicketStatus.Closed),
            Unassigned = await scope.CountAsync(t => t.AssignedToId == null, cancellationToken),
            AssignedToMe = await _dbContext.Tickets.CountAsync(t => t.AssignedToId == userId, cancellationToken),
            CreatedByMe = await _dbContext.Tickets.CountAsync(t => t.CreatedById == userId, cancellationToken)
        };

        var recentTickets = await scope
            .Include(t => t.Category)
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .OrderByDescending(t => t.UpdatedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        var response = new DashboardResponse
        {
            Role = role.ToString(),
            Tickets = stats,
            ByStatus = byStatus,
            ByPriority = byPriority,
            ByCategory = byCategory,
            RecentTickets = recentTickets.Select(TicketMapper.ToResponse).ToList()
        };

        if (role == UserRole.Admin)
        {
            response.Users = new UserStats
            {
                Total = await _dbContext.Users.CountAsync(cancellationToken),
                Employees = await _dbContext.Users.CountAsync(u => u.Role == UserRole.Employee, cancellationToken),
                SupportAgents = await _dbContext.Users.CountAsync(u => u.Role == UserRole.SupportAgent, cancellationToken),
                Admins = await _dbContext.Users.CountAsync(u => u.Role == UserRole.Admin, cancellationToken),
                Active = await _dbContext.Users.CountAsync(u => u.IsActive, cancellationToken),
                Inactive = await _dbContext.Users.CountAsync(u => !u.IsActive, cancellationToken)
            };
        }

        return response;
    }

    private static int CountFor(IReadOnlyList<StatusCount> statuses, TicketStatus status) =>
        statuses.FirstOrDefault(s => s.Status == status)?.Count ?? 0;

    private int RequireCurrentUserId() =>
        _currentUserService.UserId
        ?? throw new UnauthorizedException("The current request is not authenticated.");

    private UserRole CurrentRole
    {
        get
        {
            var roleText = _currentUserService.Role;

            return !string.IsNullOrWhiteSpace(roleText)
                   && Enum.TryParse<UserRole>(roleText, ignoreCase: true, out var role)
                ? role
                : UserRole.Employee;
        }
    }
}
