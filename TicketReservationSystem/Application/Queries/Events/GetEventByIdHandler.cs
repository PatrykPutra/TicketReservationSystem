using TicketReservationSystem.Application.DTOs;
using TicketReservationSystem.Domain.Repositories;

namespace TicketReservationSystem.Application.Queries.Events
{
    public class GetEventByIdHandler : IQueryHandler<GetEventByIdQuery, GetEventByIdResult>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetEventByIdHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<GetEventByIdResult> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
        {
            var socialEvent = await _unitOfWork.Events.GetByIdAsync(request.Id, cancellationToken);

            if (socialEvent is null)
                return new GetEventByIdResult(null);

            var dto = new EventDto(
                socialEvent.Id,
                socialEvent.Name,
                socialEvent.Description,
                socialEvent.TimeRange.StartTime,
                socialEvent.TimeRange.EndTime,
                socialEvent.TotalTickets,
                socialEvent.AvailableTickets,
                socialEvent.ReservedTickets,
                socialEvent.Status,
                socialEvent.TicketPrice.Amount,
                socialEvent.TicketPrice.Currency);

            return new GetEventByIdResult(dto);
        }
    }
}
