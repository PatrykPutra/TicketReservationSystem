using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Repositories;

namespace TicketReservationSystem.Application.Commands.Authentication
{
    public class SendAuthenticationCodeHandler : ICommandHandler<SendAuthenticationCodeCommand, SendAuthenticationCodeResult>
    {
        private readonly IUnitOfWork _unitOfWork;

        public SendAuthenticationCodeHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SendAuthenticationCodeResult> Handle(SendAuthenticationCodeCommand request, CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);

            if (user is null)
                return new SendAuthenticationCodeResult(new UserNotFoundError($"User with email {request.Email} not found"));

            var codes = await _unitOfWork.EmailVerificationCodes.FindAsync(e => e.UserId == user.Id, cancellationToken);
            var latestCode = codes.OrderByDescending(e => e.CreatedAt).FirstOrDefault();

            if (latestCode is not null && latestCode.CreatedAt > DateTime.UtcNow.AddSeconds(-60))
                return new SendAuthenticationCodeResult(new RateLimitedError("Too many requests"));

            var code = Random.Shared.Next(100000, 999999).ToString();
            var expiresAt = DateTime.UtcNow.AddMinutes(5);

            var verificationCode = EmailVerificationCode.Generate(user.Id, user.Email, code, expiresAt);
            _unitOfWork.EmailVerificationCodes.Add(verificationCode);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return SendAuthenticationCodeResult.Success();
        }
    }
}
