using HelpDesk.Application.DTOs.Comments;

namespace HelpDesk.Application.Interfaces;

public interface ICommentService
{
    Task<IReadOnlyList<CommentResponse>> GetForTicketAsync(int ticketId, CancellationToken cancellationToken = default);
    Task<CommentResponse> AddAsync(int ticketId, CreateCommentRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int ticketId, int commentId, CancellationToken cancellationToken = default);
}
