
using MediatR;

namespace TicketReservationSystem.Application.Queries
{
    public interface IQuery<out T> : IRequest<T> where T : notnull
    {
    }
}
