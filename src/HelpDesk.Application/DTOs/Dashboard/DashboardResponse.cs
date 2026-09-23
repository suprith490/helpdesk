using HelpDesk.Application.DTOs.Tickets;
using HelpDesk.Domain.Enums;

namespace HelpDesk.Application.DTOs.Dashboard;

public class DashboardResponse
{
    public string Role { get; set; } = string.Empty;
    public TicketStats Tickets { get; set; } = new();
    public IReadOnlyList<StatusCount> ByStatus { get; set; } = Array.Empty<StatusCount>();
    public IReadOnlyList<PriorityCount> ByPriority { get; set; } = Array.Empty<PriorityCount>();
    public IReadOnlyList<CategoryCount> ByCategory { get; set; } = Array.Empty<CategoryCount>();
    public UserStats? Users { get; set; }
    public IReadOnlyList<TicketResponse> RecentTickets { get; set; } = Array.Empty<TicketResponse>();
}

public class TicketStats
{
    public int Total { get; set; }
    public int New { get; set; }
    public int Assigned { get; set; }
    public int InProgress { get; set; }
    public int Resolved { get; set; }
    public int Closed { get; set; }
    public int Unassigned { get; set; }
    public int AssignedToMe { get; set; }
    public int CreatedByMe { get; set; }
}

public class StatusCount
{
    public TicketStatus Status { get; set; }
    public int Count { get; set; }
}

public class PriorityCount
{
    public TicketPriority Priority { get; set; }
    public int Count { get; set; }
}

public class CategoryCount
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class UserStats
{
    public int Total { get; set; }
    public int Employees { get; set; }
    public int SupportAgents { get; set; }
    public int Admins { get; set; }
    public int Active { get; set; }
    public int Inactive { get; set; }
}
