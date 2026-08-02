using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Domain.Repositories;

namespace TicketReservationSystem.Application.Commands.Tickets
{
    public class TicketCancelationHandler : ICommandHandler<TicketCancelationCommand, TicketCancelationResult>
    {
        private readonly IUnitOfWork _unitOfWork;

        public TicketCancelationHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<TicketCancelationResult> Handle(TicketCancelationCommand request, CancellationToken cancellationToken)
        {
            var ticket = await _unitOfWork.Tickets.GetByIdAsync(request.TicketId, cancellationToken);

            if (ticket is null)
                return new TicketCancelationResult(new NotFoundError($"Ticket {request.TicketId} not found"));

            if (ticket.UserId != request.UserId)
                return new TicketCancelationResult(new UnauthorizedUserError("User does not own this ticket"));

            ticket.Cancel(request.UserId);
            ticket.SocialEvent.CancelReservation();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return TicketCancelationResult.Success(ticket.Id, ticket.Status);
        }
    }
}
