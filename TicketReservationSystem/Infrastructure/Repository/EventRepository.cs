using Microsoft.EntityFrameworkCore;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.Repositories;
using TicketReservationSystem.Infrastructure.Persistence;

namespace TicketReservationSystem.Infrastructure.Repository
{
    public class EventRepository : IEventRepository
    {
        private readonly ApplicationDbContext _context;

        public EventRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public void Add(SocialEvent socialEvent)
        {
            _context.SocialEvents.Add(socialEvent);
        }

        public void Delete(SocialEvent socialEvent)
        {
            _context.SocialEvents.Remove(socialEvent);
        }

        public async Task<SocialEvent?> GetByIdAsync(SocialEventId id, CancellationToken cancellationToken = default)
        {
            return await _context.SocialEvents
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        }

        public async Task<List<SocialEvent>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SocialEvents.ToListAsync(cancellationToken);
        }
    }
}
