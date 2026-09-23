using HelpDesk.Application.DTOs.Users;
using HelpDesk.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Controllers;

[Authorize]
public class UsersController : ApiControllerBase
{
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUserService;

    public UsersController(IUserService userService, ICurrentUserService currentUserService)
    {
        _userService = userService;
        _currentUserService = currentUserService;
    }

    [HttpGet("agents")]
    [Authorize(Roles = "SupportAgent,Admin")]
    public async Task<IActionResult> GetAgents(CancellationToken cancellationToken)
    {
        var agents = await _userService.GetAgentsAsync(cancellationToken);
        return Ok(agents);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] UserQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var users = await _userService.GetAllAsync(parameters, cancellationToken);
        return Ok(users);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(CurrentUserId(), cancellationToken);
        return Ok(user);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _userService.UpdateProfileAsync(request, cancellationToken);
        return Ok(updated);
    }

    [HttpPost("me/change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _userService.ChangePasswordAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(id, cancellationToken);
        return Ok(user);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _userService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    private int CurrentUserId() =>
        _currentUserService.UserId
        ?? throw new Application.Exceptions.UnauthorizedException("The current request is not authenticated.");
}
