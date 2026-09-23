using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Application.DTOs.Tickets;

public class AssignTicketRequest
{
    [Range(1, int.MaxValue)]
    public int? AssignedToId { get; set; }
}
