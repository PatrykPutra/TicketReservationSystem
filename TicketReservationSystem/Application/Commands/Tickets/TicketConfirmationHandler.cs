using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Domain.Repositories;

namespace TicketReservationSystem.Application.Commands.Tickets
{
    public class TicketConfirmationHandler : ICommandHandler<TicketConfirmationCommand, TicketConfirmationResult>
    {
        private readonly IUnitOfWork _unitOfWork;

        public TicketConfirmationHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<TicketConfirmationResult> Handle(TicketConfirmationCommand request, CancellationToken cancellationToken)
        {
            var ticket = await _unitOfWork.Tickets.GetByIdAsync(request.TicketId, cancellationToken);

            if (ticket is null)
                return new TicketConfirmationResult(new NotFoundError($"Ticket {request.TicketId} not found"));

            if (!ticket.IsReserved())
                return new TicketConfirmationResult(new TicketNotAvailableError("Only reserved ticket can be confirmed"));

            if (ticket.UserId != request.UserId)
                return new TicketConfirmationResult(new UnauthorizedUserError("User does not own this ticket"));

            ticket.Confirm(request.UserId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return TicketConfirmationResult.Success(ticket.Id, ticket.Status, request.ConfirmedAt);
        }
    }
}
