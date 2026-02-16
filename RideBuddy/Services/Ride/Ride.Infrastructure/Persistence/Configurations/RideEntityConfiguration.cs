using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ride.Domain.Entities;
using Ride.Domain.Enums;
using Ride.Domain.ValueObjects;

namespace Ride.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for RideEntity.
/// </summary>
public class RideEntityConfiguration : IEntityTypeConfiguration<RideEntity>
{
    public void Configure(EntityTypeBuilder<RideEntity> builder)
    {
        builder.ToTable("Rides");
        builder.HasKey(r => r.Id);

        // DriverId value object
        builder.Property(r => r.DriverId)
            .HasConversion(
                driverId => driverId.Value,
                value => DriverId.Create(value))
            .HasColumnName("DriverId")
            .IsRequired();

        // Driver name fields
        builder.Property(r => r.DriverFirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.DriverLastName)
            .HasMaxLength(100)
            .IsRequired();

        // Origin value object — stored as three columns
        builder.OwnsOne(r => r.Origin, origin =>
        {
            origin.Property(o => o.Name)
                .HasColumnName("OriginName")
                .HasMaxLength(200)
                .IsRequired();

            origin.Property(o => o.Latitude)
                .HasColumnName("OriginLatitude")
                .IsRequired();

            origin.Property(o => o.Longitude)
                .HasColumnName("OriginLongitude")
                .IsRequired();
        });

        // Destination value object
        builder.OwnsOne(r => r.Destination, dest =>
        {
            dest.Property(d => d.Name)
                .HasColumnName("DestinationName")
                .HasMaxLength(200)
                .IsRequired();

            dest.Property(d => d.Latitude)
                .HasColumnName("DestinationLatitude")
                .IsRequired();

            dest.Property(d => d.Longitude)
                .HasColumnName("DestinationLongitude")
                .IsRequired();
        });

        builder.Property(r => r.DepartureTime)
            .IsRequired();

        // TotalSeats value object
        builder.Property(r => r.TotalSeats)
            .HasConversion(
                seats => seats.Value,
                value => SeatsCount.Create(value))
            .HasColumnName("TotalSeats")
            .IsRequired();

        // AvailableSeats value object
        builder.Property(r => r.AvailableSeats)
            .HasConversion(
                seats => seats.Value,
                value => SeatsCount.Create(value))
            .HasColumnName("AvailableSeats")
            .IsRequired();

        // PricePerSeat — Money value object
        builder.OwnsOne(r => r.PricePerSeat, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("PricePerSeat")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(r => r.Status)
            .HasConversion(
                status => status.ToString(),
                value => Enum.Parse<RideStatus>(value))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.AutoConfirmBookings)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.StartedAt);
        builder.Property(r => r.CompletedAt);
        builder.Property(r => r.CancelledAt);

        builder.Property(r => r.CancellationReason)
            .HasMaxLength(500);

        builder.Property(r => r.Version)
            .IsConcurrencyToken();

        // Indexes
        builder.HasIndex(r => r.DriverId)
            .HasDatabaseName("IX_Rides_DriverId");

        builder.HasIndex(r => r.Status)
            .HasDatabaseName("IX_Rides_Status");

        builder.HasIndex(r => r.DepartureTime)
            .HasDatabaseName("IX_Rides_DepartureTime");

        // Ignore domain events collection
        builder.Ignore(r => r.DomainEvents);
    }
}
