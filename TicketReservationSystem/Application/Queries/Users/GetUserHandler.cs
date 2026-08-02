using TicketReservationSystem.Application.DTOs;
using TicketReservationSystem.Domain.Repositories;

namespace TicketReservationSystem.Application.Queries.Users
{
    public class GetUserHandler : IQueryHandler<GetUserQuery, GetUserResult>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetUserHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<GetUserResult> Handle(GetUserQuery request, CancellationToken cancellationToken)
        {
            var user = request.Email is not null
                ? await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken)
                : request.UserId.HasValue
                    ? await _unitOfWork.Users.GetByIdAsync(request.UserId.Value, cancellationToken)
                    : null;

            if (user is null)
                return new GetUserResult(null);

            var dto = new UserDto(
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.IsVerified);

            return new GetUserResult(dto);
        }
    }
}
