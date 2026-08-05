using System.Linq.Expressions;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Domain.Repositories
{
    public interface IVerificationCodeRepository
    {
        void Add(VerificationCode code);
        Task<VerificationCode?> GetByIdAsync(VerificationCodeId id, CancellationToken cancellationToken = default);
        Task<List<VerificationCode>> FindAsync(Expression<Func<VerificationCode, bool>> predicate, CancellationToken cancellationToken = default);
    }
}
