using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.Repositories;
using TicketReservationSystem.Infrastructure.Persistence;

namespace TicketReservationSystem.Infrastructure.Repository
{
    public class EmailVerificationCodeRepository : IEmailVerificationCodeRepository
    {
        private readonly ApplicationDbContext _context;

        public EmailVerificationCodeRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public void Add(EmailVerificationCode code)
        {
            _context.EmailVerificationCodes.Add(code);
        }

        public async Task<EmailVerificationCode?> GetByIdAsync(EmailVerificationCodeId id, CancellationToken cancellationToken = default)
        {
            return await _context.EmailVerificationCodes
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public async Task<List<EmailVerificationCode>> FindAsync(Expression<Func<EmailVerificationCode, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _context.EmailVerificationCodes
                .Where(predicate)
                .ToListAsync(cancellationToken);
        }
    }
}
