namespace TicketReservationSystem.Domain.Repositories
{
    public interface IUnitOfWork
    {
        IEventRepository Events { get; }
        ITicketRepository Tickets { get; }
        IPaymentRepository Payments { get; }
        IUserRepository Users { get; }
        IEmailVerificationCodeRepository EmailVerificationCodes { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
