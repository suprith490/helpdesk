using HelpDesk.Domain.Enums;
using HelpDesk.Domain.Workflow;

namespace HelpDesk.Tests.Domain;

public class TicketStatusWorkflowTests
{
    [Theory]
    [InlineData(TicketStatus.New, TicketStatus.Assigned)]
    [InlineData(TicketStatus.New, TicketStatus.InProgress)]
    [InlineData(TicketStatus.Assigned, TicketStatus.InProgress)]
    [InlineData(TicketStatus.Assigned, TicketStatus.New)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Resolved)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Assigned)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Closed)]
    [InlineData(TicketStatus.Resolved, TicketStatus.InProgress)]
    [InlineData(TicketStatus.Closed, TicketStatus.InProgress)]
    public void IsAllowed_ReturnsTrue_ForSupportedTransitions(TicketStatus from, TicketStatus to)
    {
        Assert.True(TicketStatusWorkflow.IsAllowed(from, to));
    }

    [Theory]
    [InlineData(TicketStatus.New, TicketStatus.Resolved)]
    [InlineData(TicketStatus.New, TicketStatus.Closed)]
    [InlineData(TicketStatus.Assigned, TicketStatus.Resolved)]
    [InlineData(TicketStatus.Closed, TicketStatus.Resolved)]
    public void IsAllowed_ReturnsFalse_ForUnsupportedTransitions(TicketStatus from, TicketStatus to)
    {
        Assert.False(TicketStatusWorkflow.IsAllowed(from, to));
    }

    [Fact]
    public void IsAllowed_ReturnsFalse_WhenTransitioningToSameStatus()
    {
        Assert.False(TicketStatusWorkflow.IsAllowed(TicketStatus.New, TicketStatus.New));
    }

    [Fact]
    public void NextStatuses_ReturnsExpectedTargets_ForNewStatus()
    {
        var next = TicketStatusWorkflow.NextStatuses(TicketStatus.New);

        Assert.Equal(new[] { TicketStatus.Assigned, TicketStatus.InProgress }, next);
    }
}
