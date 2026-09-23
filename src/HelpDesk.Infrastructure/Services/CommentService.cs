using HelpDesk.Application.DTOs.Comments;
using HelpDesk.Application.Exceptions;
using HelpDesk.Application.Interfaces;
using HelpDesk.Domain.Entities;
using HelpDesk.Domain.Enums;
using HelpDesk.Infrastructure.Persistence;
using HelpDesk.Infrastructure.Services.Mapping;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Infrastructure.Services;

public class CommentService : ICommentService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public CommentService(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<CommentResponse>> GetForTicketAsync(
        int ticketId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanAccessTicketAsync(ticketId, cancellationToken);

        var query = _dbContext.Comments
            .AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.TicketId == ticketId);

        if (CurrentRole == UserRole.Employee)
        {
            query = query.Where(c => !c.IsInternal);
        }

        var comments = await query
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return comments.Select(CommentMapper.ToResponse).ToList();
    }

    public async Task<CommentResponse> AddAsync(
        int ticketId,
        CreateCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = RequireCurrentUserId();
        await EnsureCanAccessTicketAsync(ticketId, cancellationToken);

        if (request.IsInternal && CurrentRole == UserRole.Employee)
        {
            throw new ForbiddenException("Only support agents and administrators can post internal notes.");
        }

        var comment = new Comment
        {
            TicketId = ticketId,
            UserId = userId,
            Body = request.Body.Trim(),
            IsInternal = request.IsInternal,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Comments.Add(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var created = await _dbContext.Comments
            .AsNoTracking()
            .Include(c => c.User)
            .FirstAsync(c => c.Id == comment.Id, cancellationToken);

        return CommentMapper.ToResponse(created);
    }

    public async Task DeleteAsync(
        int ticketId,
        int commentId,
        CancellationToken cancellationToken = default)
    {
        var userId = RequireCurrentUserId();

        var comment = await _dbContext.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId && c.TicketId == ticketId, cancellationToken);

        if (comment is null)
        {
            throw new NotFoundException($"Comment with id {commentId} was not found on ticket {ticketId}.");
        }

        if (comment.UserId != userId && CurrentRole != UserRole.Admin)
        {
            throw new ForbiddenException("You can only delete your own comments.");
        }

        _dbContext.Comments.Remove(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCanAccessTicketAsync(int ticketId, CancellationToken cancellationToken)
    {
        var ticket = await _dbContext.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);

        if (ticket is null)
        {
            throw new NotFoundException($"Ticket with id {ticketId} was not found.");
        }

        if (CurrentRole is UserRole.SupportAgent or UserRole.Admin)
        {
            return;
        }

        if (ticket.CreatedById != RequireCurrentUserId())
        {
            throw new ForbiddenException("You are not allowed to access this ticket.");
        }
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
}
