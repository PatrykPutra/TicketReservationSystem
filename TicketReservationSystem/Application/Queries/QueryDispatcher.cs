using MediatR;
using TicketReservationSystem.Application.Abstractions;

namespace TicketReservationSystem.Application.Queries;

internal sealed class QueryDispatcher(IMediator mediator) : IQueryDispatcher
{
    public async Task<TResult> ExecuteAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>
        where TResult : notnull
    {
        return await mediator.Send(query, cancellationToken);
    }
}
