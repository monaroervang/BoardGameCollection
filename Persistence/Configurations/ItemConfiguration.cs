using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("Items");

        // Primary key
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .ValueGeneratedNever();

        // Archive status
        builder.Property(i => i.IsArchived)
            .IsRequired()
            .HasDefaultValue(false);

        // BoardGame relationship
        builder.HasOne<BoardGame>() // One Item has one BoardGame, one BoardGame can have many Items
            .WithMany()
            .HasForeignKey(i => i.BoardGameId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete to avoid deleting BoardGame when Item is deleted

        // Owner relationship
        builder.HasOne<User>() // One Item has one Owner, one User can have many Items
            .WithMany()
            .HasForeignKey(i => i.OwnerId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete to avoid deleting User when Item is deleted
    }
}