using Microsoft.Extensions.FileSystemGlobbing.Internal;
using System.Text.RegularExpressions;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.Errors;
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
            if(!Regex.IsMatch(request.Email, @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$"))
                return new AddUserResult(new ValidationError($"{request.Email} is invalid email format"));

            var existing = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);

            if (existing is not null)
                return new AddUserResult(new UserAlreadyExistsError($"User with email {request.Email} already exists"));

            User user = User.Register(request.Email, request.FirstName, request.LastName, request.PhoneNumber);

            _unitOfWork.Users.Add(user);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return AddUserResult.Success(user.Id);
        }
    }
}
