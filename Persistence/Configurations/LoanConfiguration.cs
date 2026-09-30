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

        // Use SQL Server RowVersion for optimistic concurrency.
        builder.Property(l => l.RowVersion)
            .IsRowVersion();

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

        builder.HasIndex(loan => loan.ItemId) // Index for efficient lookups by ItemId
        .IsUnique() // Ensure that an item can only be loaned out once at a time
        .HasFilter("[ReturnDate] IS NULL"); // Only enforce uniqueness for active loans (where ReturnDate is null)
    }
}