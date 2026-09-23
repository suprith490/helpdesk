using HelpDesk.Domain.Enums;

namespace HelpDesk.Application.DTOs.Comments;

public class CommentResponse
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public UserRole UserRole { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public DateTime CreatedAt { get; set; }
}
