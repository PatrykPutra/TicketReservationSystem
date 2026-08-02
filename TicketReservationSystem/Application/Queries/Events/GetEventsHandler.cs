using TicketReservationSystem.Application.DTOs;
using TicketReservationSystem.Domain.Repositories;

namespace TicketReservationSystem.Application.Queries.Events
{
    public class GetEventsHandler : IQueryHandler<GetEventsQuery, GetEventsResult>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetEventsHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<GetEventsResult> Handle(GetEventsQuery request, CancellationToken cancellationToken)
        {
            var events = await _unitOfWork.Events.GetAllAsync(cancellationToken);

            var dtos = events.Select(e => new EventDto(
                e.Id,
                e.Name,
                e.Description,
                e.TimeRange.StartTime,
                e.TimeRange.EndTime,
                e.TotalTickets,
                e.AvailableTickets,
                e.ReservedTickets,
                e.Status,
                e.TicketPrice.Amount,
                e.TicketPrice.Currency))
                .ToList();

            return new GetEventsResult(dtos);
        }
    }
}
