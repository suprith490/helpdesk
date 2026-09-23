namespace HelpDesk.Application.DTOs.Tickets;

public class TicketHistoryResponse
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int ChangedById { get; set; }
    public string ChangedByName { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime CreatedAt { get; set; }
}
