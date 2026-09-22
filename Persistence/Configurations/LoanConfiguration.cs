using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("Loans");

        // Primary key
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .ValueGeneratedNever();

        // Item relationship
        builder.HasOne<Item>()
            .WithMany()
            .HasForeignKey(l => l.ItemId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict); // Preserve loan history by preventing cascade deletes

        // Borrower relationship
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(l => l.BorrowerId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict); // Preserve loan history by preventing cascade deletes

        // Calculated domain properties are not persisted
        builder.Ignore(l => l.IsReturned);
        builder.Ignore(l => l.IsOverdue);
    }
}