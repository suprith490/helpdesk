using System.ComponentModel.DataAnnotations;
using HelpDesk.Domain.Enums;

namespace HelpDesk.Application.DTOs.Tickets;

public class UpdateTicketStatusRequest
{
    [Required]
    [EnumDataType(typeof(TicketStatus))]
    public TicketStatus Status { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}
