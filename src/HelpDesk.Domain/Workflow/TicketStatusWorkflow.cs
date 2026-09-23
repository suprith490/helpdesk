using HelpDesk.Domain.Enums;

namespace HelpDesk.Domain.Workflow;

public static class TicketStatusWorkflow
{
    private static readonly IReadOnlyDictionary<TicketStatus, TicketStatus[]> AllowedTransitions =
        new Dictionary<TicketStatus, TicketStatus[]>
        {
            [TicketStatus.New] = new[] { TicketStatus.Assigned, TicketStatus.InProgress },
            [TicketStatus.Assigned] = new[] { TicketStatus.InProgress, TicketStatus.New },
            [TicketStatus.InProgress] = new[] { TicketStatus.Resolved, TicketStatus.Assigned },
            [TicketStatus.Resolved] = new[] { TicketStatus.Closed, TicketStatus.InProgress },
            [TicketStatus.Closed] = new[] { TicketStatus.InProgress }
        };

    public static bool IsAllowed(TicketStatus from, TicketStatus to)
    {
        if (from == to)
        {
            return false;
        }

        return AllowedTransitions.TryGetValue(from, out var targets)
               && targets.Contains(to);
    }

    public static IReadOnlyList<TicketStatus> NextStatuses(TicketStatus current) =>
        AllowedTransitions.TryGetValue(current, out var targets)
            ? targets
            : Array.Empty<TicketStatus>();
}
