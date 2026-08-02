using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.Repositories;
using TicketReservationSystem.Infrastructure.Persistence;

namespace TicketReservationSystem.Infrastructure.Repository
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly ApplicationDbContext _context;

        public PaymentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public void Add(Payment payment)
        {
            _context.Payments.Add(payment);
        }

        public void Delete(Payment payment)
        {
            _context.Payments.Remove(payment);
        }

        public async Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken cancellationToken = default)
        {
            return await _context.Payments
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<List<Payment>> FindAsync(Expression<Func<Payment, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _context.Payments
                .Where(predicate)
                .ToListAsync(cancellationToken);
        }
    }
}