using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Domain.Repositories
{
    public interface IEventRepository
    {
        void Add(SocialEvent account);
        void Delete(SocialEvent account);
        Task<SocialEvent?> GetByIdAsync(SocialEventId id, CancellationToken cancellationToken = default);
        Task<List<SocialEvent>> GetAllAsync(CancellationToken cancellationToken = default);
    }
}
