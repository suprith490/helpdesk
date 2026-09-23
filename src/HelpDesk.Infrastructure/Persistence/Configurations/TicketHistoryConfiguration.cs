using HelpDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDesk.Infrastructure.Persistence.Configurations;

public class TicketHistoryConfiguration : IEntityTypeConfiguration<TicketHistory>
{
    public void Configure(EntityTypeBuilder<TicketHistory> builder)
    {
        builder.ToTable("TicketHistory");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.FieldName).IsRequired().HasMaxLength(50);
        builder.Property(h => h.OldValue).HasMaxLength(200);
        builder.Property(h => h.NewValue).HasMaxLength(200);
        builder.Property(h => h.CreatedAt).IsRequired();

        builder.HasIndex(h => h.TicketId);

        builder.HasOne(h => h.Ticket)
            .WithMany(t => t.History)
            .HasForeignKey(h => h.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.ChangedBy)
            .WithMany(u => u.TicketHistories)
            .HasForeignKey(h => h.ChangedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
