using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.Authentication;
using TicketReservationSystem.Domain.Repositories;

namespace TicketReservationSystem.Application.Commands.Authentication
{
    public class GenerateTokenHandler : ICommandHandler<GenerateTokenCommand, GenerateTokenResult>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJwtService _jwtService;

        public GenerateTokenHandler(IUnitOfWork unitOfWork, IJwtService jwtService)
        {
            _unitOfWork = unitOfWork;
            _jwtService = jwtService;
        }

        public async Task<GenerateTokenResult> Handle(GenerateTokenCommand request, CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);

            if (user is null)
                return new GenerateTokenResult(new InvalidCredentialsError("Invalid email or code"));

            var codes = await _unitOfWork.VerificationCodes.FindAsync(
                e => e.UserId == user.Id && e.Code == request.Code && !e.IsUsed && e.ExpiresAt > DateTime.UtcNow,
                cancellationToken);
            var code = codes.FirstOrDefault();

            if (code is null)
                return new GenerateTokenResult(new InvalidCredentialsError("Invalid email or code"));

            code.MarkAsUsed();
            user.Login();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var token = _jwtService.GenerateToken(user.Id, user.Email);
            var expiresAt = DateTime.UtcNow.AddMinutes(60);

            return GenerateTokenResult.Success(token, expiresAt);
        }
    }
}
