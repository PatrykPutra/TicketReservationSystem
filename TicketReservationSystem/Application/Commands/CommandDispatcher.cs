using MediatR;
using TicketReservationSystem.Application.Abstractions;

namespace TicketReservationSystem.Application.Commands;

internal sealed class CommandDispatcher(IMediator mediator) : ICommandDispatcher
{
    public async Task<TResponse> DispatchAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>
        where TResponse : Result
    {
        return await mediator.Send(command, cancellationToken);
    }
}
