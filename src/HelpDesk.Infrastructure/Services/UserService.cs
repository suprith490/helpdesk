using HelpDesk.Application.Common;
using HelpDesk.Application.DTOs.Users;
using HelpDesk.Application.Exceptions;
using HelpDesk.Application.Interfaces;
using HelpDesk.Domain.Entities;
using HelpDesk.Domain.Enums;
using HelpDesk.Infrastructure.Persistence;
using HelpDesk.Infrastructure.Services.Mapping;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(
        AppDbContext dbContext,
        ICurrentUserService currentUserService,
        IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _passwordHasher = passwordHasher;
    }

    public async Task<IReadOnlyList<UserResponse>> GetAgentsAsync(
        CancellationToken cancellationToken = default)
    {
        var agents = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.Department)
            .Where(u => u.IsActive && (u.Role == UserRole.SupportAgent || u.Role == UserRole.Admin))
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync(cancellationToken);

        return agents.Select(UserMapper.ToResponse).ToList();
    }

    public async Task<PagedResult<UserResponse>> GetAllAsync(
        UserQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Users
            .AsNoTracking()
            .Include(u => u.Department)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim();
            query = query.Where(u =>
                u.FirstName.Contains(search) ||
                u.LastName.Contains(search) ||
                u.Email.Contains(search));
        }

        if (parameters.Role.HasValue)
        {
            query = query.Where(u => u.Role == parameters.Role.Value);
        }

        if (parameters.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == parameters.IsActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<UserResponse>(
            users.Select(UserMapper.ToResponse).ToList(),
            parameters.Page,
            parameters.PageSize,
            totalCount);
    }

    public async Task<UserResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException($"User with id {id} was not found.");
        }

        return UserMapper.ToResponse(user);
    }

    public async Task<UserResponse> UpdateAsync(
        int id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException($"User with id {id} was not found.");
        }

        if (request.DepartmentId.HasValue &&
            !await _dbContext.Departments.AnyAsync(d => d.Id == request.DepartmentId.Value, cancellationToken))
        {
            throw new BadRequestException("The selected department does not exist.");
        }

        if (id == RequireCurrentUserId())
        {
            if (request.Role != user.Role)
            {
                throw new BadRequestException("You cannot change your own role.");
            }

            if (!request.IsActive)
            {
                throw new BadRequestException("You cannot deactivate your own account.");
            }
        }

        user.Role = request.Role;
        user.DepartmentId = request.DepartmentId;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(user.Id, cancellationToken);
    }

    public async Task<UserResponse> UpdateProfileAsync(
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = RequireCurrentUserId();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("The current user no longer exists.");
        }

        var email = request.Email.Trim().ToLowerInvariant();

        var emailInUse = await _dbContext.Users
            .AnyAsync(u => u.Email == email && u.Id != userId, cancellationToken);

        if (emailInUse)
        {
            throw new ConflictException($"A user with email '{request.Email}' already exists.");
        }

        if (request.DepartmentId.HasValue &&
            !await _dbContext.Departments.AnyAsync(d => d.Id == request.DepartmentId.Value, cancellationToken))
        {
            throw new BadRequestException("The selected department does not exist.");
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = email;
        user.DepartmentId = request.DepartmentId;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(user.Id, cancellationToken);
    }

    public async Task ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = RequireCurrentUserId();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("The current user no longer exists.");
        }

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new BadRequestException("The current password is incorrect.");
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private int RequireCurrentUserId() =>
        _currentUserService.UserId
        ?? throw new UnauthorizedException("The current request is not authenticated.");
}
