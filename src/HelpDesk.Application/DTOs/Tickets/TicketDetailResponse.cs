using HelpDesk.Application.DTOs.Comments;

namespace HelpDesk.Application.DTOs.Tickets;

public class TicketDetailResponse
{
    public TicketResponse Ticket { get; set; } = new();
    public IReadOnlyList<CommentResponse> Comments { get; set; } = Array.Empty<CommentResponse>();
    public IReadOnlyList<TicketHistoryResponse> History { get; set; } = Array.Empty<TicketHistoryResponse>();
}
