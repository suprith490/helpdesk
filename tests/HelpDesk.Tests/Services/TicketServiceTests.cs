using HelpDesk.Application.DTOs.Tickets;
using HelpDesk.Application.Exceptions;
using HelpDesk.Domain.Entities;
using HelpDesk.Domain.Enums;
using HelpDesk.Infrastructure.Services;
using HelpDesk.Tests.TestSupport;

namespace HelpDesk.Tests.Services;

public class TicketServiceTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();
    private readonly FakeCurrentUserService _currentUser = new();

    private TicketService CreateSut() => new(_database.Context, _currentUser);

    private void ActAs(User user)
    {
        _currentUser.UserId = user.Id;
        _currentUser.Email = user.Email;
        _currentUser.Role = user.Role.ToString();
    }

    [Fact]
    public async Task CreateAsync_GeneratesSequentialTicketNumbers()
    {
        var department = TestData.AddDepartment(_database.Context);
        var category = TestData.AddCategory(_database.Context);
        var employee = TestData.AddUser(_database.Context, "jane@example.com", departmentId: department.Id);
        ActAs(employee);

        var sut = CreateSut();

        var first = await sut.CreateAsync(new CreateTicketRequest
        {
            Title = "Laptop will not boot",
            Description = "Black screen after entering my password.",
            CategoryId = category.Id,
            Priority = TicketPriority.High
        });

        var second = await sut.CreateAsync(new CreateTicketRequest
        {
            Title = "VPN is slow",
            Description = "The VPN drops every few minutes.",
            CategoryId = category.Id
        });

        Assert.Equal("HD-10001", first.TicketNumber);
        Assert.Equal("HD-10002", second.TicketNumber);
        Assert.Equal(TicketStatus.New, first.Status);
    }

    [Fact]
    public async Task CreateAsync_WritesStatusHistoryEntry()
    {
        var category = TestData.AddCategory(_database.Context);
        var employee = TestData.AddUser(_database.Context, "jane@example.com");
        ActAs(employee);

        var sut = CreateSut();

        var ticket = await sut.CreateAsync(new CreateTicketRequest
        {
            Title = "Keyboard is broken",
            Description = "Some keys do not register any input.",
            CategoryId = category.Id
        });

        var history = await sut.GetHistoryAsync(ticket.Id);

        Assert.Contains(history, h => h.FieldName == nameof(Ticket.Status) && h.NewValue == "New");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyOwnTickets_ForEmployee()
    {
        var category = TestData.AddCategory(_database.Context);
        var owner = TestData.AddUser(_database.Context, "owner@example.com");
        var other = TestData.AddUser(_database.Context, "other@example.com");
        TestData.AddTicket(_database.Context, category.Id, owner.Id, ticketNumber: "HD-10001");
        TestData.AddTicket(_database.Context, category.Id, other.Id, ticketNumber: "HD-10002");

        ActAs(owner);
        var sut = CreateSut();

        var page = await sut.GetAllAsync(new TicketQueryParameters());

        Assert.Single(page.Items);
        Assert.Equal(owner.Id, page.Items[0].CreatedById);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllTickets_ForAgent()
    {
        var category = TestData.AddCategory(_database.Context);
        var owner = TestData.AddUser(_database.Context, "owner@example.com");
        var other = TestData.AddUser(_database.Context, "other@example.com");
        var agent = TestData.AddUser(_database.Context, "agent@example.com", UserRole.SupportAgent);
        TestData.AddTicket(_database.Context, category.Id, owner.Id, ticketNumber: "HD-10001");
        TestData.AddTicket(_database.Context, category.Id, other.Id, ticketNumber: "HD-10002");

        ActAs(agent);
        var sut = CreateSut();

        var page = await sut.GetAllAsync(new TicketQueryParameters());

        Assert.Equal(2, page.TotalCount);
    }

    [Fact]
    public async Task AssignAsync_SetsStatusToAssigned_AndRecordsHistory()
    {
        var category = TestData.AddCategory(_database.Context);
        var employee = TestData.AddUser(_database.Context, "jane@example.com");
        var agent = TestData.AddUser(_database.Context, "agent@example.com", UserRole.SupportAgent);
        var ticket = TestData.AddTicket(_database.Context, category.Id, employee.Id);

        ActAs(agent);
        var sut = CreateSut();

        var result = await sut.AssignAsync(ticket.Id, new AssignTicketRequest { AssignedToId = agent.Id });

        Assert.Equal(TicketStatus.Assigned, result.Status);
        Assert.Equal(agent.Id, result.AssignedToId);

        var history = await sut.GetHistoryAsync(ticket.Id);
        Assert.Contains(history, h => h.FieldName == nameof(Ticket.AssignedToId));
    }

    [Fact]
    public async Task AssignAsync_ThrowsForbidden_ForEmployee()
    {
        var category = TestData.AddCategory(_database.Context);
        var employee = TestData.AddUser(_database.Context, "jane@example.com");
        var ticket = TestData.AddTicket(_database.Context, category.Id, employee.Id);

        ActAs(employee);
        var sut = CreateSut();

        await Assert.ThrowsAsync<ForbiddenException>(
            () => sut.AssignAsync(ticket.Id, new AssignTicketRequest { AssignedToId = employee.Id }));
    }

    [Fact]
    public async Task ChangeStatusAsync_ThrowsBadRequest_OnInvalidTransition()
    {
        var category = TestData.AddCategory(_database.Context);
        var employee = TestData.AddUser(_database.Context, "jane@example.com");
        var agent = TestData.AddUser(_database.Context, "agent@example.com", UserRole.SupportAgent);
        var ticket = TestData.AddTicket(_database.Context, category.Id, employee.Id);

        ActAs(agent);
        var sut = CreateSut();

        await Assert.ThrowsAsync<BadRequestException>(
            () => sut.ChangeStatusAsync(ticket.Id, new UpdateTicketStatusRequest { Status = TicketStatus.Resolved }));
    }

    [Fact]
    public async Task ChangeStatusAsync_SetsResolvedAt_WhenResolved()
    {
        var category = TestData.AddCategory(_database.Context);
        var employee = TestData.AddUser(_database.Context, "jane@example.com");
        var agent = TestData.AddUser(_database.Context, "agent@example.com", UserRole.SupportAgent);
        var ticket = TestData.AddTicket(
            _database.Context,
            category.Id,
            employee.Id,
            assignedToId: agent.Id,
            status: TicketStatus.InProgress);

        ActAs(agent);
        var sut = CreateSut();

        var result = await sut.ChangeStatusAsync(
            ticket.Id,
            new UpdateTicketStatusRequest { Status = TicketStatus.Resolved, Note = "Fixed by replacing the PSU." });

        Assert.Equal(TicketStatus.Resolved, result.Status);
        Assert.NotNull(result.ResolvedAt);
    }

    [Fact]
    public async Task GetDetailAsync_HidesInternalComments_FromEmployee()
    {
        var category = TestData.AddCategory(_database.Context);
        var employee = TestData.AddUser(_database.Context, "jane@example.com");
        var agent = TestData.AddUser(_database.Context, "agent@example.com", UserRole.SupportAgent);
        var ticket = TestData.AddTicket(_database.Context, category.Id, employee.Id);

        _database.Context.Comments.AddRange(
            new Comment { TicketId = ticket.Id, UserId = employee.Id, Body = "Any update?", IsInternal = false },
            new Comment { TicketId = ticket.Id, UserId = agent.Id, Body = "Check the PSU.", IsInternal = true });
        _database.Context.SaveChanges();

        ActAs(employee);
        var employeeView = await CreateSut().GetDetailAsync(ticket.Id);

        ActAs(agent);
        var agentView = await CreateSut().GetDetailAsync(ticket.Id);

        Assert.Single(employeeView.Comments);
        Assert.False(employeeView.Comments[0].IsInternal);
        Assert.Equal(2, agentView.Comments.Count);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsForbidden_ForNonAdmin()
    {
        var category = TestData.AddCategory(_database.Context);
        var agent = TestData.AddUser(_database.Context, "agent@example.com", UserRole.SupportAgent);
        var ticket = TestData.AddTicket(_database.Context, category.Id, agent.Id);

        ActAs(agent);
        var sut = CreateSut();

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.DeleteAsync(ticket.Id));
    }

    [Fact]
    public async Task DeleteAsync_RemovesTicket_ForAdmin()
    {
        var category = TestData.AddCategory(_database.Context);
        var employee = TestData.AddUser(_database.Context, "jane@example.com");
        var admin = TestData.AddUser(_database.Context, "admin@example.com", UserRole.Admin);
        var ticket = TestData.AddTicket(_database.Context, category.Id, employee.Id);

        ActAs(admin);
        var sut = CreateSut();

        await sut.DeleteAsync(ticket.Id);

        Assert.Null(await _database.Context.Tickets.FindAsync(ticket.Id));
    }

    public void Dispose() => _database.Dispose();
}
