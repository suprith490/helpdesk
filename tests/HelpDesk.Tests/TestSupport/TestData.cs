using HelpDesk.Domain.Entities;
using HelpDesk.Domain.Enums;
using HelpDesk.Infrastructure.Persistence;

namespace HelpDesk.Tests.TestSupport;

/// <summary>
/// Small factory methods that make service tests read like a story instead of
/// a wall of object initialisers.
/// </summary>
public static class TestData
{
    public static Department AddDepartment(
        AppDbContext db,
        string name = "IT",
        string? description = "Information Technology")
    {
        // Departments are seeded with HasData, so reuse the seeded row instead
        // of trying to insert a duplicate (the Name column is unique).
        var existing = db.Departments.FirstOrDefault(d => d.Name == name);
        if (existing is not null)
        {
            return existing;
        }

        var department = new Department { Name = name, Description = description };
        db.Departments.Add(department);
        db.SaveChanges();
        return department;
    }

    public static Category AddCategory(
        AppDbContext db,
        string name = "Hardware",
        bool isActive = true)
    {
        // Categories are seeded with HasData as well; reuse when present.
        var existing = db.Categories.FirstOrDefault(c => c.Name == name);
        if (existing is not null)
        {
            return existing;
        }

        var category = new Category
        {
            Name = name,
            Description = $"{name} issues",
            IsActive = isActive
        };
        db.Categories.Add(category);
        db.SaveChanges();
        return category;
    }

    public static User AddUser(
        AppDbContext db,
        string email,
        UserRole role = UserRole.Employee,
        int? departmentId = null,
        bool isActive = true,
        string passwordHash = "hash")
    {
        var user = new User
        {
            FirstName = email.Split('@')[0],
            LastName = "Tester",
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            Role = role,
            DepartmentId = departmentId,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    public static Ticket AddTicket(
        AppDbContext db,
        int categoryId,
        int createdById,
        int? assignedToId = null,
        TicketStatus status = TicketStatus.New,
        TicketPriority priority = TicketPriority.Medium,
        string ticketNumber = "HD-10001",
        string title = "Printer is not working",
        string description = "The office printer shows an offline error.")
    {
        var ticket = new Ticket
        {
            TicketNumber = ticketNumber,
            Title = title,
            Description = description,
            CategoryId = categoryId,
            CreatedById = createdById,
            AssignedToId = assignedToId,
            Status = status,
            Priority = priority,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Tickets.Add(ticket);
        db.SaveChanges();
        return ticket;
    }
}
