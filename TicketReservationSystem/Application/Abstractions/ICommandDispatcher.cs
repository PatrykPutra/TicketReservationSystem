using TicketReservationSystem.Application.Commands;

namespace TicketReservationSystem.Application.Abstractions;

public interface ICommandDispatcher
{
    Task<TResponse> DispatchAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>
        where TResponse : Result;
}
