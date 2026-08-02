using System.Linq.Expressions;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Domain.Repositories
{
    public interface ITicketRepository
    {
        void Add(Ticket account);
        void Delete(Ticket account);
        Task<Ticket?> GetByIdAsync(TicketId id, CancellationToken cancellationToken = default);
        Task<List<Ticket>> GetByEventIdAsync(SocialEventId eventId, CancellationToken cancellationToken = default);
        Task<List<Ticket>> FindAsync(Expression<Func<Ticket, bool>> predicate, CancellationToken cancellationToken = default);
    }
}
