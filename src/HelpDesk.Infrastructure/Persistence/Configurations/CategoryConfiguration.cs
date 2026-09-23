using HelpDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDesk.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.IsActive).HasDefaultValue(true);

        builder.HasIndex(c => c.Name).IsUnique();

        builder.HasData(
            new Category { Id = 1, Name = "Hardware", Description = "Laptops, desktops and printers" },
            new Category { Id = 2, Name = "Software", Description = "Applications and licenses" },
            new Category { Id = 3, Name = "Network", Description = "Connectivity and VPN" },
            new Category { Id = 4, Name = "Access & Accounts", Description = "Passwords and permissions" },
            new Category { Id = 5, Name = "Other", Description = "Anything that does not fit elsewhere" });
    }
}
