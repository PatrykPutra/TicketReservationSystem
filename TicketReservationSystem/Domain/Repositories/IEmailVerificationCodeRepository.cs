using System.Linq.Expressions;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Domain.Repositories
{
    public interface IEmailVerificationCodeRepository
    {
        void Add(EmailVerificationCode code);
        Task<EmailVerificationCode?> GetByIdAsync(EmailVerificationCodeId id, CancellationToken cancellationToken = default);
        Task<List<EmailVerificationCode>> FindAsync(Expression<Func<EmailVerificationCode, bool>> predicate, CancellationToken cancellationToken = default);
    }
}
