using TicketReservationSystem.Application.DTOs;
using TicketReservationSystem.Domain.Repositories;

namespace TicketReservationSystem.Application.Queries.Tickets
{
    public class GetTicketsByEventHandler : IQueryHandler<GetTicketsByEventQuery, GetTicketsByEventResult>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetTicketsByEventHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<GetTicketsByEventResult> Handle(GetTicketsByEventQuery request, CancellationToken cancellationToken)
        {
            var tickets = await _unitOfWork.Tickets.GetByEventIdAsync(request.EventId, cancellationToken);

            var dtos = tickets.Select(t => new TicketDto(
                t.Id,
                t.EventId,
                t.SeatNumber,
                t.Status,
                t.UserId,
                t.Price.Amount,
                t.Price.Currency))
                .ToList();

            return new GetTicketsByEventResult(dtos);
        }
    }
}
