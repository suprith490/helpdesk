using HelpDesk.Application.DTOs.Tickets;
using HelpDesk.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Controllers;

[Authorize]
public class TicketsController : ApiControllerBase
{
    private readonly ITicketService _ticketService;

    public TicketsController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTicketRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _ticketService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] TicketQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var tickets = await _ticketService.GetAllAsync(parameters, cancellationToken);
        return Ok(tickets);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var ticket = await _ticketService.GetByIdAsync(id, cancellationToken);
        return Ok(ticket);
    }

    [HttpGet("{id:int}/details")]
    public async Task<IActionResult> GetDetail(int id, CancellationToken cancellationToken)
    {
        var detail = await _ticketService.GetDetailAsync(id, cancellationToken);
        return Ok(detail);
    }

    [HttpGet("{id:int}/history")]
    public async Task<IActionResult> GetHistory(int id, CancellationToken cancellationToken)
    {
        var history = await _ticketService.GetHistoryAsync(id, cancellationToken);
        return Ok(history);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateTicketRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _ticketService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpPost("{id:int}/assign")]
    [Authorize(Roles = "SupportAgent,Admin")]
    public async Task<IActionResult> Assign(
        int id,
        [FromBody] AssignTicketRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _ticketService.AssignAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpPost("{id:int}/status")]
    [Authorize(Roles = "SupportAgent,Admin")]
    public async Task<IActionResult> ChangeStatus(
        int id,
        [FromBody] UpdateTicketStatusRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _ticketService.ChangeStatusAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _ticketService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
