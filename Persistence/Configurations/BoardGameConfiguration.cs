using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class BoardGameConfiguration : IEntityTypeConfiguration<BoardGame> //Represents the configuration for the BoardGame entity. It implements the IEntityTypeConfiguration interface to define the configuration for the BoardGame entity in the database context.
{
    public void Configure(EntityTypeBuilder<BoardGame> builder) //Explains the Configure method, which is used to configure the BoardGame entity. It takes an EntityTypeBuilder<BoardGame> parameter that allows you to specify various configurations for the entity.
    {
        builder.ToTable("BoardGames");

        // Primary key
        builder.HasKey(bg => bg.Id);

        builder.Property(bg => bg.Id)
            .ValueGeneratedNever();

        // Required properties
        // Domain validation ensures the object is valid (protects object)
        // while IsRequired ensures the database column cannot be NULL (protects database)
        builder.Property(bg => bg.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(bg => bg.Description)
            .IsRequired();

        builder.Property(bg => bg.Genre)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(bg => bg.Publisher)
            .IsRequired()
            .HasMaxLength(100);

        // Edition is optional
        builder.Property(bg => bg.Edition)
            .HasMaxLength(50);

        builder.Property(boardGame => boardGame.RowVersion) // Configure the RowVersion property for concurrency control
            .IsRowVersion();
    }
}
