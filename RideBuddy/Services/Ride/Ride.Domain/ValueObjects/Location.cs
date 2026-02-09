using SharedKernel;
using Ride.Domain.Exceptions;

namespace Ride.Domain.ValueObjects;

/// <summary>
/// Value Object representing a geographic location with name and coordinates.
/// </summary>
public class Location : ValueObject
{
    /// <summary>
    /// Human-readable name of the location (e.g., "Novi Sad", "Beograd").
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Latitude coordinate.
    /// </summary>
    public double Latitude { get; }

    /// <summary>
    /// Longitude coordinate.
    /// </summary>
    public double Longitude { get; }

    private Location(string name, double latitude, double longitude)
    {
        Name = name;
        Latitude = latitude;
        Longitude = longitude;
    }

    public static Location Create(string name, double latitude, double longitude)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new RideDomainException("Location name cannot be empty.");

        if (latitude < -90 || latitude > 90)
            throw new RideDomainException("Latitude must be between -90 and 90.");

        if (longitude < -180 || longitude > 180)
            throw new RideDomainException("Longitude must be between -180 and 180.");

        return new Location(name.Trim(), latitude, longitude);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name;
        yield return Latitude;
        yield return Longitude;
    }

    public override string ToString() => $"{Name} ({Latitude:F6}, {Longitude:F6})";
}
