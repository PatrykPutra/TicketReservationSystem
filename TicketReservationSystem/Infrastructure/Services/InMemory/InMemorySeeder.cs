using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.Repositories;
using TicketReservationSystem.Domain.ValueObjects;

namespace TicketReservationSystem.Infrastructure.Services.InMemory;

public class InMemorySeeder
{
    private readonly IUnitOfWork _unitOfWork;

    public InMemorySeeder(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task SeedAsync()
    {
        var existingEvents = await _unitOfWork.Events.GetAllAsync();
        if (existingEvents.Count > 0)
            return;

        var eventId = SocialEventId.Create(Guid.Parse("ABCDABCD-ABCD-ABCD-ABCD-ABCDABCDABCD"));
        var timeRange = new DateTimeRange(
            new DateTime(2027, 1, 15, 19, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 1, 15, 23, 0, 0, DateTimeKind.Utc));
        var ticketPrice = new Money(150, "PLN");

        var socialEvent = new SocialEvent(
            eventId,
            "Koncert Noworoczny 2027",
            "Noworoczny koncert muzyki klasycznej",
            timeRange,
            100,
            EventStatus.Scheduled,
            ticketPrice);

        _unitOfWork.Events.Add(socialEvent);

        for (int i = 1; i <= 100; i++)
        {
            var ticketId = TicketId.CreateUnique();
            var ticket = new Ticket(ticketId, eventId, socialEvent, $"A{i}", ticketPrice);
            _unitOfWork.Tickets.Add(ticket);
        }

        var userId = UserId.CreateUnique();
        var user = new User(userId);
        user.Register("putryko@gmail.com", "Jan", "Kowalski", "123456789");
        _unitOfWork.Users.Add(user);

        await _unitOfWork.SaveChangesAsync();
    }
}
