using HelpDesk.Application.DTOs.Comments;
using HelpDesk.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tickets/{ticketId:int}/comments")]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentsController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int ticketId, CancellationToken cancellationToken)
    {
        var comments = await _commentService.GetForTicketAsync(ticketId, cancellationToken);
        return Ok(comments);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        int ticketId,
        [FromBody] CreateCommentRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _commentService.AddAsync(ticketId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpDelete("{commentId:int}")]
    public async Task<IActionResult> Delete(
        int ticketId,
        int commentId,
        CancellationToken cancellationToken)
    {
        await _commentService.DeleteAsync(ticketId, commentId, cancellationToken);
        return NoContent();
    }
}
