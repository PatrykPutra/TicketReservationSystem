using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.Repositories;
using TicketReservationSystem.Infrastructure.Persistence;

namespace TicketReservationSystem.Infrastructure.Repository
{
    public class VerificationCodeRepository : IVerificationCodeRepository
    {
        private readonly ApplicationDbContext _context;

        public VerificationCodeRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public void Add(VerificationCode code)
        {
            _context.VerificationCodes.Add(code);
        }

        public async Task<VerificationCode?> GetByIdAsync(VerificationCodeId id, CancellationToken cancellationToken = default)
        {
            return await _context.VerificationCodes
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public async Task<List<VerificationCode>> FindAsync(Expression<Func<VerificationCode, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _context.VerificationCodes
                .Where(predicate)
                .ToListAsync(cancellationToken);
        }
    }
}
