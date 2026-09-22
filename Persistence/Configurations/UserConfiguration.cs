using Domain.Models;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        // Primary key
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .ValueGeneratedNever();

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(u => u.Username) //Means that the Username property must be unique across all User entities in the database
            .IsUnique();

        builder.Property(u => u.DateOfBirth)
            .IsRequired();

        // Name value object
        builder.ComplexProperty(u => u.Name, name => //ComplexProperty is used to configure value objects in EF Core
        {
            name.Property(n => n.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            name.Property(n => n.LastName)
                .IsRequired()
                .HasMaxLength(100);
        });

        // Address value object
        builder.ComplexProperty(u => u.Address, address =>
        {
            address.Property(a => a.Street)
                .IsRequired()
                .HasMaxLength(150);

            address.Property(a => a.HouseNumber)
                .IsRequired()
                .HasMaxLength(20);

            address.Property(a => a.PostalCode)
                .IsRequired()
                .HasMaxLength(20);

            address.Property(a => a.City)
                .IsRequired()
                .HasMaxLength(100);

            address.Property(a => a.Country)
                .IsRequired()
                .HasMaxLength(100);
        });

        // Email value object
        builder.Property(u => u.Email) 
            .HasConversion( 
                email => email.Value,
                value => new Email(value)) // Store Email value object as a single database column
            .IsRequired()
            .HasMaxLength(254);

        builder.HasIndex(u => u.Email)
           .IsUnique();

    }
}