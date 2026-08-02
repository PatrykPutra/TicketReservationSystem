using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.Repositories;

namespace TicketReservationSystem.Application.Commands.Users
{
    public class AddUserHandler : ICommandHandler<AddUserCommand, AddUserResult>
    {
        private readonly IUnitOfWork _unitOfWork;

        public AddUserHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<AddUserResult> Handle(AddUserCommand request, CancellationToken cancellationToken)
        {
            var existing = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);

            if (existing is not null)
                return new AddUserResult(new UserAlreadyExistsError($"User with email {request.Email} already exists"));

            var userId = UserId.CreateUnique();
            var user = new User(userId);

            user.Register(request.Email, request.FirstName, request.LastName, request.PhoneNumber);

            _unitOfWork.Users.Add(user);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return AddUserResult.Success(userId);
        }
    }
}
