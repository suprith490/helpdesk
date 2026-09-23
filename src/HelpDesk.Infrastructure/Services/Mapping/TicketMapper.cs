using HelpDesk.Application.DTOs.Tickets;
using HelpDesk.Domain.Entities;

namespace HelpDesk.Infrastructure.Services.Mapping;

public static class TicketMapper
{
    public static TicketResponse ToResponse(Ticket ticket) => new()
    {
        Id = ticket.Id,
        TicketNumber = ticket.TicketNumber,
        Title = ticket.Title,
        Description = ticket.Description,
        CategoryId = ticket.CategoryId,
        CategoryName = ticket.Category?.Name ?? string.Empty,
        CreatedById = ticket.CreatedById,
        CreatedByName = FullName(ticket.CreatedBy),
        AssignedToId = ticket.AssignedToId,
        AssignedToName = ticket.AssignedTo is null ? null : FullName(ticket.AssignedTo),
        Priority = ticket.Priority,
        Status = ticket.Status,
        CreatedAt = ticket.CreatedAt,
        UpdatedAt = ticket.UpdatedAt,
        ResolvedAt = ticket.ResolvedAt,
        ClosedAt = ticket.ClosedAt
    };

    public static TicketHistoryResponse ToHistoryResponse(TicketHistory history) => new()
    {
        Id = history.Id,
        TicketId = history.TicketId,
        ChangedById = history.ChangedById,
        ChangedByName = FullName(history.ChangedBy),
        FieldName = history.FieldName,
        OldValue = history.OldValue,
        NewValue = history.NewValue,
        CreatedAt = history.CreatedAt
    };

    private static string FullName(User? user) =>
        user is null ? string.Empty : $"{user.FirstName} {user.LastName}".Trim();
}
