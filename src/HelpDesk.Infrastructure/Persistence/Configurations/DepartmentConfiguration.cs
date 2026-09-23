using HelpDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDesk.Infrastructure.Persistence.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name).IsRequired().HasMaxLength(100);
        builder.Property(d => d.Description).HasMaxLength(500);

        builder.HasIndex(d => d.Name).IsUnique();

        builder.HasData(
            new Department { Id = 1, Name = "IT", Description = "Information Technology" },
            new Department { Id = 2, Name = "HR", Description = "Human Resources" },
            new Department { Id = 3, Name = "Finance", Description = "Finance and Accounting" },
            new Department { Id = 4, Name = "Operations", Description = "Business Operations" });
    }
}
