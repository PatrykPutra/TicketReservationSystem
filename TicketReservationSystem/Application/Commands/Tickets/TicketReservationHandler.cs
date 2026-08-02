using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Domain.Repositories;

namespace TicketReservationSystem.Application.Commands.Tickets
{
    public class TicketReservationHandler : ICommandHandler<TicketReservationCommand, TicketReservationResult>
    {
        private readonly IUnitOfWork _unitOfWork;

        public TicketReservationHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<TicketReservationResult> Handle(TicketReservationCommand request, CancellationToken cancellationToken)
        {
            var ticket = await _unitOfWork.Tickets.GetByIdAsync(request.TicketId, cancellationToken);

            if (ticket is null)
                return new TicketReservationResult(new NotFoundError($"Ticket {request.TicketId} not found"));

            if (!ticket.IsAvailable())
                return new TicketReservationResult(new TicketNotAvailableError("Ticket not available"));

            ticket.Reserve(request.UserId);
            ticket.SocialEvent.ReserveTicket();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return TicketReservationResult.Success(ticket.Id, ticket.Status, DateTime.UtcNow);
        }
    }
}
