using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Domain.Repositories;

namespace TicketReservationSystem.Application.Commands.Authentication
{
    public class AuthenticationHandler : ICommandHandler<AuthenticationCommand, AuthenticationResult>
    {
        private readonly IUnitOfWork _unitOfWork;

        public AuthenticationHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<AuthenticationResult> Handle(AuthenticationCommand request, CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);

            if (user is null)
                return new AuthenticationResult(new UserNotFoundError($"User with email {request.Email} not found"));

            user.Login();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return AuthenticationResult.Success(user.Id);
        }
    }
}
