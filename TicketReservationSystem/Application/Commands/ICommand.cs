

using MediatR;
using TicketReservationSystem.Application.Abstractions;

namespace TicketReservationSystem.Application.Commands
{
    public interface ICommand<out TResponse> : IRequest<TResponse>
        where TResponse : Result
    {
    }
}
