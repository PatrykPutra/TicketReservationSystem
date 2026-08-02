using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Domain.Repositories
{
    public interface IUserRepository
    {
        void Add(User account);
        void Delete(User account);
        Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    }
}
