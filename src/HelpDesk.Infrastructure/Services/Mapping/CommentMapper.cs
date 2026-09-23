using HelpDesk.Application.DTOs.Comments;
using HelpDesk.Domain.Entities;
using HelpDesk.Domain.Enums;

namespace HelpDesk.Infrastructure.Services.Mapping;

public static class CommentMapper
{
    public static CommentResponse ToResponse(Comment comment) => new()
    {
        Id = comment.Id,
        TicketId = comment.TicketId,
        UserId = comment.UserId,
        UserName = comment.User is null
            ? string.Empty
            : $"{comment.User.FirstName} {comment.User.LastName}".Trim(),
        UserRole = comment.User?.Role ?? UserRole.Employee,
        Body = comment.Body,
        IsInternal = comment.IsInternal,
        CreatedAt = comment.CreatedAt
    };
}
