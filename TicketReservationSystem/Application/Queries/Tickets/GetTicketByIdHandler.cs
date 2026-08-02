using TicketReservationSystem.Application.DTOs;
using TicketReservationSystem.Domain.Repositories;

namespace TicketReservationSystem.Application.Queries.Tickets
{
    public class GetTicketByIdHandler : IQueryHandler<GetTicketByIdQuery, GetTicketByIdResult>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetTicketByIdHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<GetTicketByIdResult> Handle(GetTicketByIdQuery request, CancellationToken cancellationToken)
        {
            var ticket = await _unitOfWork.Tickets.GetByIdAsync(request.TicketId, cancellationToken);

            if (ticket is null)
                return new GetTicketByIdResult(null);

            var dto = new TicketDto(
                ticket.Id,
                ticket.EventId,
                ticket.SeatNumber,
                ticket.Status,
                ticket.UserId,
                ticket.Price.Amount,
                ticket.Price.Currency);

            return new GetTicketByIdResult(dto);
        }
    }
}
