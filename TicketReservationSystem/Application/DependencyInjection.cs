using System.Reflection;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.Commands;
using TicketReservationSystem.Application.Queries;
using TicketReservationSystem.Domain.Events;

namespace TicketReservationSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        services.AddScoped<IQueryDispatcher, QueryDispatcher>();
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();

        return services;
    }
}
