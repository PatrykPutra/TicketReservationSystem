using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Events;
using TicketReservationSystem.Domain.Exceptions;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.ValueObjects;

namespace TicketReservationSystem.Tests;

public class TicketReleaseReservationTests
{
    private static readonly Money DefaultPrice = new(150, "PLN");

    private static SocialEvent CreateSocialEvent()
    {
        var eventId = SocialEventId.CreateUnique();
        var timeRange = new DateTimeRange(
            DateTime.UtcNow.AddDays(30),
            DateTime.UtcNow.AddDays(30).AddHours(4));
        return new SocialEvent(eventId, "Test Event", "Description", timeRange, 100, EventStatus.Scheduled, DefaultPrice);
    }

    private static Ticket CreateReservedTicket()
    {
        var ticketId = TicketId.CreateUnique();
        var socialEvent = CreateSocialEvent();
        var ticket = new Ticket(ticketId, socialEvent.Id, socialEvent, "A1", DefaultPrice);
        ticket.Reserve(UserId.CreateUnique());
        return ticket;
    }

    [Fact]
    public void ReleaseReservation_sets_status_to_Available()
    {
        var ticket = CreateReservedTicket();

        ticket.ReleaseReservation();

        Assert.Equal(TicketStatus.Available, ticket.Status);
    }

    [Fact]
    public void ReleaseReservation_clears_UserId()
    {
        var ticket = CreateReservedTicket();

        ticket.ReleaseReservation();

        Assert.Null(ticket.UserId);
    }

    [Fact]
    public void ReleaseReservation_clears_ReservedAt()
    {
        var ticket = CreateReservedTicket();

        ticket.ReleaseReservation();

        Assert.Null(ticket.ReservedAt);
    }

    [Fact]
    public void ReleaseReservation_clears_ConfirmedAt()
    {
        var ticket = CreateReservedTicket();

        ticket.ReleaseReservation();

        Assert.Null(ticket.ConfirmedAt);
    }

    [Fact]
    public void ReleaseReservation_fires_TicketReleasedEvent()
    {
        var ticket = CreateReservedTicket();

        ticket.ReleaseReservation();

        var domainEvent = ticket.DomainEvents.OfType<TicketReleasedEvent>().SingleOrDefault();
        Assert.NotNull(domainEvent);
        Assert.Equal(ticket.Id, domainEvent.TicketId);
        Assert.Equal(ticket.EventId, domainEvent.EventId);
    }

    [Fact]
    public void ReleaseReservation_on_Available_ticket_throws()
    {
        var ticketId = TicketId.CreateUnique();
        var socialEvent = CreateSocialEvent();
        var ticket = new Ticket(ticketId, socialEvent.Id, socialEvent, "A1", DefaultPrice);

        Assert.Throws<TicketStatusException>(() => ticket.ReleaseReservation());
    }

    [Fact]
    public void ReleaseReservation_on_Confirmed_ticket_throws()
    {
        var ticket = CreateReservedTicket();
        var userId = ticket.UserId!.Value;
        ticket.Confirm(userId);

        Assert.Throws<TicketStatusException>(() => ticket.ReleaseReservation());
    }
}
