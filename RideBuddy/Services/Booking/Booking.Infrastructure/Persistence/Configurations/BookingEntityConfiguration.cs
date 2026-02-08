using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for BookingEntity.
/// Defines table mapping, value object conversions, and indexes.
/// </summary>
public class BookingEntityConfiguration : IEntityTypeConfiguration<BookingEntity>
{
    public void Configure(EntityTypeBuilder<BookingEntity> builder)
    {
        builder.ToTable("Bookings");

        builder.HasKey(b => b.Id);

        // Value Object conversions
        builder.Property(b => b.RideId)
            .HasConversion(
                rideId => rideId.Value,
                value => RideId.Create(value))
            .HasColumnName("RideId")
            .IsRequired();

        builder.Property(b => b.PassengerId)
            .HasConversion(
                passengerId => passengerId.Value,
                value => PassengerId.Create(value))
            .HasColumnName("PassengerId")
            .IsRequired();

        builder.Property(b => b.SeatsBooked)
            .HasConversion(
                seats => seats.Value,
                value => SeatsCount.Create(value))
            .HasColumnName("SeatsBooked")
            .IsRequired();

        // Money value object - stored as two columns
        builder.OwnsOne(b => b.TotalPrice, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("TotalPrice")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(b => b.Status)
            .HasConversion(
                status => status.ToString(),
                value => Enum.Parse<BookingStatus>(value))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(b => b.BookedAt)
            .IsRequired();

        builder.Property(b => b.ConfirmedAt);
        builder.Property(b => b.CancelledAt);
        builder.Property(b => b.CompletedAt);
        builder.Property(b => b.RejectedAt);

        builder.Property(b => b.CancellationReason)
            .HasMaxLength(500);

        builder.Property(b => b.DriverId)
            .IsRequired();

        builder.Property(b => b.Version)
            .IsConcurrencyToken();

        // Indexes for common queries
        builder.HasIndex(b => b.PassengerId)
            .HasDatabaseName("IX_Bookings_PassengerId");

        builder.HasIndex(b => b.RideId)
            .HasDatabaseName("IX_Bookings_RideId");

        builder.HasIndex(b => b.Status)
            .HasDatabaseName("IX_Bookings_Status");

        builder.HasIndex(b => new { b.PassengerId, b.RideId, b.Status })
            .HasDatabaseName("IX_Bookings_Passenger_Ride_Status");

        // Ignore domain events collection (not persisted)
        builder.Ignore(b => b.DomainEvents);
    }
}
