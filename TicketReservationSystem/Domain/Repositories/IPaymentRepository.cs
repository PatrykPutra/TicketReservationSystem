using System.Linq.Expressions;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Domain.Repositories
{
    public interface IPaymentRepository
    {
        void Add(Payment payment);
        void Delete(Payment payment);
        Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken cancellationToken = default);
        Task<List<Payment>> FindAsync(Expression<Func<Payment, bool>> predicate, CancellationToken cancellationToken = default);
    }
}