
using MediatR;
using TicketReservationSystem.Application.Abstractions;

namespace TicketReservationSystem.Application.Commands
{
    public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
        where TResponse : Result
    {
    }
}
