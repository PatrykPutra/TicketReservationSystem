using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.Repositories;
using TicketReservationSystem.Infrastructure.Persistence;

namespace TicketReservationSystem.Infrastructure.Repository
{
    public class TicketRepository : ITicketRepository
    {
        private readonly ApplicationDbContext _context;

        public TicketRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public void Add(Ticket ticket)
        {
            _context.Tickets.Add(ticket);
        }

        public void Delete(Ticket ticket)
        {
            _context.Tickets.Remove(ticket);
        }

        public async Task<Ticket?> GetByIdAsync(TicketId id, CancellationToken cancellationToken = default)
        {
            return await _context.Tickets
                .Include(t => t.SocialEvent)
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        }

        public async Task<List<Ticket>> GetByEventIdAsync(SocialEventId eventId, CancellationToken cancellationToken = default)
        {
            return await _context.Tickets
                .Include(t => t.SocialEvent)
                .Where(t => t.EventId == eventId)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Ticket>> FindAsync(Expression<Func<Ticket, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _context.Tickets
                .Include(t => t.SocialEvent)
                .Where(predicate)
                .ToListAsync(cancellationToken);
        }
    }
}
