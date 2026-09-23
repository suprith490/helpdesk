using HelpDesk.Application.Common;
using HelpDesk.Application.DTOs.Tickets;
using HelpDesk.Application.Exceptions;
using HelpDesk.Application.Interfaces;
using HelpDesk.Domain.Entities;
using HelpDesk.Domain.Enums;
using HelpDesk.Domain.Workflow;
using HelpDesk.Infrastructure.Persistence;
using HelpDesk.Infrastructure.Services.Mapping;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Infrastructure.Services;

public class TicketService : ITicketService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public TicketService(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<TicketResponse> CreateAsync(
        CreateTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = RequireCurrentUserId();
        var category = await GetActiveCategoryAsync(request.CategoryId, cancellationToken);

        var ticket = new Ticket
        {
            TicketNumber = BuildTemporaryNumber(),
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            CategoryId = category.Id,
            CreatedById = userId,
            Priority = request.Priority,
            Status = TicketStatus.New,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        _dbContext.Tickets.Add(ticket);
        await _dbContext.SaveChangesAsync(cancellationToken);

        ticket.TicketNumber = BuildTicketNumber(ticket.Id);
        _dbContext.TicketHistory.Add(
            CreateHistory(ticket.Id, userId, nameof(Ticket.Status), null, TicketStatus.New.ToString()));

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetByIdAsync(ticket.Id, cancellationToken);
    }

    public async Task<PagedResult<TicketResponse>> GetAllAsync(
        TicketQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var userId = RequireCurrentUserId();

        var query = _dbContext.Tickets
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .AsQueryable();

        if (CurrentRole == UserRole.Employee)
        {
            query = query.Where(t => t.CreatedById == userId);
        }

        if (parameters.AssignedToMe == true)
        {
            query = query.Where(t => t.AssignedToId == userId);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim();
            query = query.Where(t =>
                t.Title.Contains(search) ||
                t.TicketNumber.Contains(search) ||
                t.Description.Contains(search));
        }

        if (parameters.Status.HasValue)
        {
            query = query.Where(t => t.Status == parameters.Status.Value);
        }

        if (parameters.Priority.HasValue)
        {
            query = query.Where(t => t.Priority == parameters.Priority.Value);
        }

        if (parameters.CategoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == parameters.CategoryId.Value);
        }

        if (parameters.AssignedToId.HasValue)
        {
            query = query.Where(t => t.AssignedToId == parameters.AssignedToId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var tickets = await ApplySorting(query, parameters.SortBy, parameters.SortDirection)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TicketResponse>(
            tickets.Select(TicketMapper.ToResponse).ToList(),
            parameters.Page,
            parameters.PageSize,
            totalCount);
    }

    public async Task<TicketResponse> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var ticket = await GetTicketForReadAsync(id, includeDetails: false, cancellationToken);

        EnsureCanView(ticket);

        return TicketMapper.ToResponse(ticket);
    }

    public async Task<TicketDetailResponse> GetDetailAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var ticket = await GetTicketForReadAsync(id, includeDetails: true, cancellationToken);

        EnsureCanView(ticket);

        var comments = ticket.Comments
            .Where(c => CurrentRole != UserRole.Employee || !c.IsInternal)
            .OrderBy(c => c.CreatedAt)
            .Select(CommentMapper.ToResponse)
            .ToList();

        var history = ticket.History
            .OrderByDescending(h => h.CreatedAt)
            .Select(TicketMapper.ToHistoryResponse)
            .ToList();

        return new TicketDetailResponse
        {
            Ticket = TicketMapper.ToResponse(ticket),
            Comments = comments,
            History = history
        };
    }

    public async Task<IReadOnlyList<TicketHistoryResponse>> GetHistoryAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var ticket = await GetTicketForReadAsync(id, includeDetails: true, cancellationToken);

        EnsureCanView(ticket);

        return ticket.History
            .OrderByDescending(h => h.CreatedAt)
            .Select(TicketMapper.ToHistoryResponse)
            .ToList();
    }

    public async Task<TicketResponse> UpdateAsync(
        int id,
        UpdateTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = RequireCurrentUserId();

        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (ticket is null)
        {
            throw new NotFoundException($"Ticket with id {id} was not found.");
        }

        EnsureCanModify(ticket);

        var category = await GetActiveCategoryAsync(request.CategoryId, cancellationToken);

        var title = request.Title.Trim();
        var description = request.Description.Trim();
        var historyEntries = new List<TicketHistory>();

        if (ticket.Title != title)
        {
            historyEntries.Add(CreateHistory(ticket.Id, userId, nameof(Ticket.Title), ticket.Title, title));
            ticket.Title = title;
        }

        if (ticket.Description != description)
        {
            historyEntries.Add(CreateHistory(ticket.Id, userId, nameof(Ticket.Description), ticket.Description, description));
            ticket.Description = description;
        }

        if (ticket.CategoryId != category.Id)
        {
            historyEntries.Add(CreateHistory(ticket.Id, userId, nameof(Ticket.CategoryId), ticket.CategoryId.ToString(), category.Id.ToString()));
            ticket.CategoryId = category.Id;
        }

        if (ticket.Priority != request.Priority)
        {
            historyEntries.Add(CreateHistory(ticket.Id, userId, nameof(Ticket.Priority), ticket.Priority.ToString(), request.Priority.ToString()));
            ticket.Priority = request.Priority;
        }

        if (historyEntries.Count > 0)
        {
            ticket.UpdatedAt = DateTime.UtcNow;
            _dbContext.TicketHistory.AddRange(historyEntries);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return await GetByIdAsync(ticket.Id, cancellationToken);
    }

    public async Task<TicketResponse> AssignAsync(
        int id,
        AssignTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        RequireAgentOrAdmin();
        var userId = RequireCurrentUserId();

        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (ticket is null)
        {
            throw new NotFoundException($"Ticket with id {id} was not found.");
        }

        var historyEntries = new List<TicketHistory>();

        if (request.AssignedToId.HasValue)
        {
            var agent = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == request.AssignedToId.Value, cancellationToken);

            if (agent is null)
            {
                throw new BadRequestException("The selected support agent does not exist.");
            }

            if (!agent.IsActive)
            {
                throw new BadRequestException("The selected support agent is deactivated.");
            }

            if (agent.Role is not (UserRole.SupportAgent or UserRole.Admin))
            {
                throw new BadRequestException("Tickets can only be assigned to support agents or administrators.");
            }

            if (ticket.AssignedToId != agent.Id)
            {
                historyEntries.Add(CreateHistory(
                    ticket.Id,
                    userId,
                    nameof(Ticket.AssignedToId),
                    ticket.AssignedToId?.ToString(),
                    agent.Id.ToString()));

                ticket.AssignedToId = agent.Id;
            }

            if (ticket.Status == TicketStatus.New)
            {
                historyEntries.Add(CreateHistory(
                    ticket.Id,
                    userId,
                    nameof(Ticket.Status),
                    TicketStatus.New.ToString(),
                    TicketStatus.Assigned.ToString()));

                ticket.Status = TicketStatus.Assigned;
            }
        }
        else if (ticket.AssignedToId.HasValue)
        {
            historyEntries.Add(CreateHistory(
                ticket.Id,
                userId,
                nameof(Ticket.AssignedToId),
                ticket.AssignedToId.Value.ToString(),
                null));

            ticket.AssignedToId = null;

            if (ticket.Status == TicketStatus.Assigned)
            {
                historyEntries.Add(CreateHistory(
                    ticket.Id,
                    userId,
                    nameof(Ticket.Status),
                    TicketStatus.Assigned.ToString(),
                    TicketStatus.New.ToString()));

                ticket.Status = TicketStatus.New;
            }
        }

        if (historyEntries.Count > 0)
        {
            ticket.UpdatedAt = DateTime.UtcNow;
            _dbContext.TicketHistory.AddRange(historyEntries);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return await GetByIdAsync(ticket.Id, cancellationToken);
    }

    public async Task<TicketResponse> ChangeStatusAsync(
        int id,
        UpdateTicketStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        RequireAgentOrAdmin();
        var userId = RequireCurrentUserId();

        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (ticket is null)
        {
            throw new NotFoundException($"Ticket with id {id} was not found.");
        }

        var target = request.Status;

        if (ticket.Status == target)
        {
            throw new ConflictException($"Ticket is already in '{target}' status.");
        }

        if (!TicketStatusWorkflow.IsAllowed(ticket.Status, target))
        {
            throw new BadRequestException(
                $"Invalid status transition from '{ticket.Status}' to '{target}'.");
        }

        var oldStatus = ticket.Status;

        var historyEntries = new List<TicketHistory>
        {
            CreateHistory(ticket.Id, userId, nameof(Ticket.Status), oldStatus.ToString(), target.ToString())
        };

        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            historyEntries.Add(CreateHistory(ticket.Id, userId, "Note", null, request.Note.Trim()));
        }

        ticket.Status = target;
        ticket.UpdatedAt = DateTime.UtcNow;
        ticket.ResolvedAt = target switch
        {
            TicketStatus.Resolved => DateTime.UtcNow,
            TicketStatus.Closed => ticket.ResolvedAt ?? DateTime.UtcNow,
            _ => null
        };
        ticket.ClosedAt = target == TicketStatus.Closed ? DateTime.UtcNow : null;

        _dbContext.TicketHistory.AddRange(historyEntries);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(ticket.Id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (CurrentRole != UserRole.Admin)
        {
            throw new ForbiddenException("Only administrators can delete tickets.");
        }

        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (ticket is null)
        {
            throw new NotFoundException($"Ticket with id {id} was not found.");
        }

        _dbContext.Tickets.Remove(ticket);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    internal async Task<Ticket> GetTicketForReadAsync(
        int id,
        bool includeDetails,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Tickets
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .AsQueryable();

        if (includeDetails)
        {
            query = query
                .Include(t => t.Comments).ThenInclude(c => c.User)
                .Include(t => t.History).ThenInclude(h => h.ChangedBy);
        }

        var ticket = await query.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (ticket is null)
        {
            throw new NotFoundException($"Ticket with id {id} was not found.");
        }

        return ticket;
    }

    private async Task<Category> GetActiveCategoryAsync(int categoryId, CancellationToken cancellationToken)
    {
        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);

        if (category is null || !category.IsActive)
        {
            throw new BadRequestException("The selected category does not exist or is inactive.");
        }

        return category;
    }

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

    private void RequireAgentOrAdmin()
    {
        if (CurrentRole is not (UserRole.SupportAgent or UserRole.Admin))
        {
            throw new ForbiddenException("Only support agents and administrators can perform this action.");
        }
    }

    private void EnsureCanView(Ticket ticket)
    {
        if (CurrentRole is UserRole.SupportAgent or UserRole.Admin)
        {
            return;
        }

        if (ticket.CreatedById != RequireCurrentUserId())
        {
            throw new ForbiddenException("You are not allowed to view this ticket.");
        }
    }

    private void EnsureCanModify(Ticket ticket)
    {
        if (CurrentRole is UserRole.SupportAgent or UserRole.Admin)
        {
            return;
        }

        if (ticket.CreatedById != RequireCurrentUserId())
        {
            throw new ForbiddenException("You are not allowed to modify this ticket.");
        }

        if (ticket.Status != TicketStatus.New)
        {
            throw new ConflictException("Only tickets in 'New' status can be modified by the requester.");
        }
    }

    private static IQueryable<Ticket> ApplySorting(
        IQueryable<Ticket> query,
        string? sortBy,
        string? sortDirection)
    {
        var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        return sortBy?.ToLowerInvariant() switch
        {
            "ticketnumber" => descending
                ? query.OrderByDescending(t => t.TicketNumber)
                : query.OrderBy(t => t.TicketNumber),
            "title" => descending
                ? query.OrderByDescending(t => t.Title)
                : query.OrderBy(t => t.Title),
            "priority" => descending
                ? query.OrderByDescending(t => t.Priority)
                : query.OrderBy(t => t.Priority),
            "status" => descending
                ? query.OrderByDescending(t => t.Status)
                : query.OrderBy(t => t.Status),
            "updatedat" => descending
                ? query.OrderByDescending(t => t.UpdatedAt)
                : query.OrderBy(t => t.UpdatedAt),
            _ => descending
                ? query.OrderByDescending(t => t.CreatedAt)
                : query.OrderBy(t => t.CreatedAt)
        };
    }

    private static string BuildTicketNumber(int ticketId) => $"HD-{10000 + ticketId}";

    private static string BuildTemporaryNumber() => $"T{Guid.NewGuid():N}"[..17];

    private static TicketHistory CreateHistory(
        int ticketId,
        int changedById,
        string fieldName,
        string? oldValue,
        string? newValue) => new()
    {
        TicketId = ticketId,
        ChangedById = changedById,
        FieldName = fieldName,
        OldValue = oldValue,
        NewValue = newValue,
        CreatedAt = DateTime.UtcNow
    };
}
