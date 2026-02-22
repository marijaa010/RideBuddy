using Notification.Domain.Entities;
using Notification.Domain.Enums;

namespace Notification.Domain.Tests.Entities;

public class NotificationEntityTests
{
    [Fact]
    public void Create_SetsAllPropertiesCorrectly()
    {
        var userId    = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var rideId    = Guid.NewGuid();

        var notification = NotificationEntity.Create(
            userId, "Title", "Message", NotificationType.BookingCreated, bookingId, rideId);

        notification.UserId.Should().Be(userId);
        notification.Title.Should().Be("Title");
        notification.Message.Should().Be("Message");
        notification.Type.Should().Be(NotificationType.BookingCreated);
        notification.BookingId.Should().Be(bookingId);
        notification.RideId.Should().Be(rideId);
    }

    [Fact]
    public void Create_IsReadIsFalse()
    {
        var notification = NotificationEntity.Create(
            Guid.NewGuid(), "T", "M", NotificationType.BookingCreated);

        notification.IsRead.Should().BeFalse();
    }

    [Fact]
    public void Create_ReadAtIsNull()
    {
        var notification = NotificationEntity.Create(
            Guid.NewGuid(), "T", "M", NotificationType.BookingCreated);

        notification.ReadAt.Should().BeNull();
    }

    [Fact]
    public void Create_GeneratesUniqueId()
    {
        var n1 = NotificationEntity.Create(Guid.NewGuid(), "T", "M", NotificationType.BookingCreated);
        var n2 = NotificationEntity.Create(Guid.NewGuid(), "T", "M", NotificationType.BookingCreated);

        n1.Id.Should().NotBe(n2.Id);
    }

    [Fact]
    public void Create_SetsCreatedAtToUtcNow()
    {
        var before       = DateTime.UtcNow;
        var notification = NotificationEntity.Create(Guid.NewGuid(), "T", "M", NotificationType.BookingCreated);
        var after        = DateTime.UtcNow;

        notification.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void MarkAsRead_SetsIsReadToTrue()
    {
        var notification = NotificationEntity.Create(
            Guid.NewGuid(), "T", "M", NotificationType.BookingCreated);

        notification.MarkAsRead();

        notification.IsRead.Should().BeTrue();
    }

    [Fact]
    public void MarkAsRead_SetsReadAt()
    {
        var notification = NotificationEntity.Create(
            Guid.NewGuid(), "T", "M", NotificationType.BookingCreated);
        var before = DateTime.UtcNow;

        notification.MarkAsRead();

        notification.ReadAt.Should().NotBeNull();
        notification.ReadAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void MarkAsRead_CalledTwice_DoesNotChangeReadAt()
    {
        var notification = NotificationEntity.Create(
            Guid.NewGuid(), "T", "M", NotificationType.BookingCreated);

        notification.MarkAsRead();
        var firstReadAt = notification.ReadAt;

        notification.MarkAsRead();

        notification.ReadAt.Should().Be(firstReadAt);
    }
}
