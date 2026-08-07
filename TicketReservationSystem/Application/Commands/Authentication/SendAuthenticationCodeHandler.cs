using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.Errors;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Repositories;

namespace TicketReservationSystem.Application.Commands.Authentication
{
    public class SendAuthenticationCodeHandler : ICommandHandler<SendAuthenticationCodeCommand, SendAuthenticationCodeResult>
    {
        private const int RateLimitWindowSeconds = 60;
        private const int MinCodeValue = 100000;
        private const int MaxCodeValue = 999999;
        internal const int CodeLifetimeMinutes = 5;

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

            var codes = await _unitOfWork.VerificationCodes.FindAsync(e => e.UserId == user.Id, cancellationToken);
            var latestCode = codes.OrderByDescending(e => e.CreatedAt).FirstOrDefault();

            if (latestCode is not null && latestCode.CreatedAt > DateTime.UtcNow.AddSeconds(-RateLimitWindowSeconds))
                return new SendAuthenticationCodeResult(new RateLimitedError("Too many requests"));

            var code = Random.Shared.Next(MinCodeValue, MaxCodeValue).ToString();
            var expiresAt = DateTime.UtcNow.AddMinutes(CodeLifetimeMinutes);

            var verificationCode = VerificationCode.Generate(user.Id, user.Email, code, expiresAt);
            _unitOfWork.VerificationCodes.Add(verificationCode);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return SendAuthenticationCodeResult.Success();
        }
    }
}
