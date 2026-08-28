using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Events;
using TicketReservationSystem.Domain.Exceptions;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.ValueObjects;

namespace TicketReservationSystem.Tests;

public class TicketTests
{
    private static readonly Money DefaultPrice = new(150, "PLN");

    private static SocialEvent CreateSocialEvent(DateTime? startTime = null, DateTime? endTime = null)
    {
        var eventId = SocialEventId.CreateUnique();

        var timeRange = new DateTimeRange(
            startTime ?? DateTime.UtcNow.AddDays(30),
            endTime ?? DateTime.UtcNow.AddDays(30).AddHours(4));

        return new SocialEvent(
            eventId,
            "Test Event",
            "Description",
            timeRange,
            100,
            EventStatus.Scheduled,
            DefaultPrice);
    }

    private static Ticket CreateAvailableTicket()
    {
        var socialEvent = CreateSocialEvent();

        return new Ticket(
            TicketId.CreateUnique(),
            socialEvent.Id,
            socialEvent,
            "A1",
            DefaultPrice);
    }

    private static Ticket CreateReservedTicket()
    {
        var ticket = CreateAvailableTicket();
        ticket.Reserve(UserId.CreateUnique());

        return ticket;
    }

    private static Ticket CreateConfirmedTicket()
    {
        var ticket = CreateReservedTicket();
        ticket.Confirm(ticket.UserId!.Value);

        return ticket;
    }


    [Fact]
    public void Reserve_ForAvailableTicket_SetsReservedProperties()
    {
        // Arrange
        var ticket = CreateAvailableTicket();
        var userId = UserId.CreateUnique();

        // Act
        ticket.Reserve(userId);

        // Assert
        Assert.Equal(TicketStatus.Reserved, ticket.Status);
        Assert.Equal(userId, ticket.UserId);
        Assert.NotNull(ticket.ReservedAt);
    }

    [Fact]
    public void Reserve_ForAvailableTicket_RaisesTicketReservedEvent()
    {
        // Arrange
        var ticket = CreateAvailableTicket();
        var userId = UserId.CreateUnique();

        // Act
        ticket.Reserve(userId);

        // Assert
        var domainEvent = ticket.DomainEvents
            .OfType<TicketReservedEvent>()
            .SingleOrDefault();

        Assert.NotNull(domainEvent);
        Assert.Equal(ticket.Id, domainEvent.TicketId);
        Assert.Equal(ticket.EventId, domainEvent.EventId);
        Assert.Equal(userId, domainEvent.UserId);
    }

    [Fact]
    public void Reserve_ForReservedTicket_ThrowsTicketStatusException()
    {
        // Arrange
        var ticket = CreateReservedTicket();

        // Act & Assert
        Assert.Throws<TicketStatusException>(
            () => ticket.Reserve(UserId.CreateUnique()));
    }

    [Fact]
    public void Reserve_ForConfirmedTicket_ThrowsTicketStatusException()
    {
        // Arrange
        var ticket = CreateConfirmedTicket();

        // Act & Assert
        Assert.Throws<TicketStatusException>(
            () => ticket.Reserve(UserId.CreateUnique()));
    }

    [Fact]
    public void Confirm_ForReservedTicket_SetsConfirmedProperties()
    {
        // Arrange
        var ticket = CreateReservedTicket();
        var userId = ticket.UserId!.Value;

        // Act
        ticket.Confirm(userId);

        // Assert
        Assert.Equal(TicketStatus.Confirmed, ticket.Status);
        Assert.NotNull(ticket.ConfirmedAt);
    }

    [Fact]
    public void Confirm_ForReservedTicket_RaisesTicketConfirmedEvent()
    {
        // Arrange
        var ticket = CreateReservedTicket();
        var userId = ticket.UserId!.Value;

        // Act
        ticket.Confirm(userId);

        // Assert
        var domainEvent = ticket.DomainEvents
            .OfType<TicketConfirmedEvent>()
            .SingleOrDefault();

        Assert.NotNull(domainEvent);
        Assert.Equal(ticket.Id, domainEvent.TicketId);
        Assert.Equal(ticket.EventId, domainEvent.EventId);
        Assert.Equal(userId, domainEvent.UserId);
    }

    [Fact]
    public void Confirm_ForAvailableTicket_ThrowsTicketStatusException()
    {
        // Arrange
        var ticket = CreateAvailableTicket();

        // Act & Assert
        Assert.Throws<TicketStatusException>(
            () => ticket.Confirm(UserId.CreateUnique()));
    }

    [Fact]
    public void Confirm_ForAnotherUser_ThrowsUnauthorizedUserException()
    {
        // Arrange
        var ticket = CreateReservedTicket();

        // Act & Assert
        Assert.Throws<UnauthorizedUserException>(
            () => ticket.Confirm(UserId.CreateUnique()));
    }

    [Fact]
    public void Cancel_ForTicketOwnedByUser_ResetsTicketProperties()
    {
        // Arrange
        var ticket = CreateConfirmedTicket();
        var userId = ticket.UserId!.Value;

        // Act
        ticket.Cancel(userId);

        // Assert
        Assert.Equal(TicketStatus.Available, ticket.Status);
        Assert.Null(ticket.UserId);
        Assert.Null(ticket.ReservedAt);
        Assert.Null(ticket.ConfirmedAt);
    }

    [Fact]
    public void Cancel_ForTicketOwnedByUser_RaisesTicketCanceledEvent()
    {
        // Arrange
        var ticket = CreateConfirmedTicket();
        var userId = ticket.UserId!.Value;

        // Act
        ticket.Cancel(userId);

        // Assert
        var domainEvent = ticket.DomainEvents
            .OfType<TicketCanceledEvent>()
            .SingleOrDefault();

        Assert.NotNull(domainEvent);
        Assert.Equal(ticket.Id, domainEvent.TicketId);
        Assert.Equal(ticket.EventId, domainEvent.EventId);
        Assert.Equal(userId, domainEvent.UserId);
    }

    [Fact]
    public void Cancel_ForAnotherUser_ThrowsUnauthorizedUserException()
    {
        // Arrange
        var ticket = CreateConfirmedTicket();

        // Act & Assert
        Assert.Throws<UnauthorizedUserException>(
            () => ticket.Cancel(UserId.CreateUnique()));
    }

    [Fact]
    public void IsAvailable_ForAvailableTicket_ReturnsTrue()
    {
        // Arrange && Act && Assert
        Assert.True(CreateAvailableTicket().IsAvailable());
    }

    [Fact]
    public void IsAvailable_ForReservedTicket_ReturnsFalse()
    {
        // Arrange && Act && Assert
        Assert.False(CreateReservedTicket().IsAvailable());
    }

    [Fact]
    public void IsReserved_ForReservedTicket_ReturnsTrue()
    {
        // Arrange && Act && Assert
        Assert.True(CreateReservedTicket().IsReserved());
    }

    [Fact]
    public void IsReserved_ForAvailableTicket_ReturnsFalse()
    {
        // Arrange && Act && Assert
        Assert.False(CreateAvailableTicket().IsReserved());
    }

    [Fact]
    public void IsConfirmed_ForConfirmedTicket_ReturnsTrue()
    {
        // Arrange && Act && Assert
        Assert.True(CreateConfirmedTicket().IsConfirmed());
    }

    [Fact]
    public void IsConfirmed_ForReservedTicket_ReturnsFalse()
    {
        // Arrange && Act && Assert
        Assert.False(CreateReservedTicket().IsConfirmed());
    }


    [Fact]
    public void IsExpired_ForPastEvent_ReturnsTrue()
    {
        // Arrange
        var socialEvent = CreateSocialEvent(
            DateTime.UtcNow.AddDays(-2),
            DateTime.UtcNow.AddDays(-1));

        var ticket = new Ticket(
            TicketId.CreateUnique(),
            socialEvent.Id,
            socialEvent,
            "A1",
            DefaultPrice);

        // Act & Assert
        Assert.True(ticket.IsExpired());
    }

    [Fact]
    public void IsExpired_ForFutureEvent_ReturnsFalse()
    {
        // Arrange
        var socialEvent = CreateSocialEvent(
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2));

        var ticket = new Ticket(
            TicketId.CreateUnique(),
            socialEvent.Id,
            socialEvent,
            "A1",
            DefaultPrice);

        // Act & Assert
        Assert.False(ticket.IsExpired());
    }

    [Fact]
    public void ReleaseReservation_ForReservedTicket_ResetsTicketProperties()
    {
        // Arrange
        var ticket = CreateReservedTicket();

        // Act
        ticket.ReleaseReservation();

        // Assert
        Assert.Equal(TicketStatus.Available, ticket.Status);
        Assert.Null(ticket.UserId);
        Assert.Null(ticket.ReservedAt);
        Assert.Null(ticket.ConfirmedAt);
    }

    [Fact]
    public void ReleaseReservation_ForReservedTicket_RaisesTicketReleasedEvent()
    {
        // Arrange
        var ticket = CreateReservedTicket();

        // Act
        ticket.ReleaseReservation();

        // Assert
        var domainEvent = ticket.DomainEvents.OfType<TicketReleasedEvent>().SingleOrDefault();
        Assert.NotNull(domainEvent);
        Assert.Equal(ticket.Id, domainEvent.TicketId);
        Assert.Equal(ticket.EventId, domainEvent.EventId);
    }

    [Fact]
    public void ReleaseReservation_ForAvailableTicket_ThrowsTicketStatusException()
    {
        // Arrange
        var ticketId = TicketId.CreateUnique();
        var socialEvent = CreateSocialEvent();
        var ticket = new Ticket(ticketId, socialEvent.Id, socialEvent, "A1", DefaultPrice);

        // Act & Assert
        Assert.Throws<TicketStatusException>(() => ticket.ReleaseReservation());
    }

    [Fact]
    public void ReleaseReservation_ForConfirmedTicket_ThrowsTicketStatusException()
    {
        // Arrange
        var ticket = CreateReservedTicket();
        var userId = ticket.UserId!.Value;
        ticket.Confirm(userId);

        // Act & Assert
        Assert.Throws<TicketStatusException>(() => ticket.ReleaseReservation());
    }
}
