using TicketReservationSystem.Application.Queries;

namespace TicketReservationSystem.Application.Abstractions;

public interface IQueryDispatcher
{
    Task<TResult> ExecuteAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>
        where TResult : notnull;
}
