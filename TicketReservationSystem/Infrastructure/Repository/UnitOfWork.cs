using TicketReservationSystem.Domain.Repositories;
using TicketReservationSystem.Infrastructure.Persistence;

namespace TicketReservationSystem.Infrastructure.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        public IUserRepository Users { get; }
        public ITicketRepository Tickets { get; }
        public IPaymentRepository Payments { get; }
        public IEventRepository Events { get; }
        public IEmailVerificationCodeRepository EmailVerificationCodes { get; }

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            Events = new EventRepository(context);
            Tickets = new TicketRepository(context);
            Payments = new PaymentRepository(context);
            Users = new UserRepository(context);
            EmailVerificationCodes = new EmailVerificationCodeRepository(context);
        }

        public void Dispose() => _context.Dispose();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => _context.SaveChangesAsync(cancellationToken);
    }
}
