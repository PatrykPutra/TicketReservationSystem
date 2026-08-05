using Microsoft.EntityFrameworkCore;
using Quartz;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Repositories;

namespace TicketReservationSystem.Infrastructure.Services.Jobs;

[DisallowConcurrentExecution]
public class ExpiredReservationsCleanupJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredReservationsCleanupJob> _logger;

    public ExpiredReservationsCleanupJob(IServiceScopeFactory scopeFactory, ILogger<ExpiredReservationsCleanupJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var threshold = DateTime.UtcNow.AddMinutes(-10);
        var expiredTickets = await unitOfWork.Tickets.FindAsync(
            t => t.Status == TicketStatus.Reserved && t.ReservedAt <= threshold,
            context.CancellationToken);

        foreach (var ticket in expiredTickets)
        {
            var hasPendingPayment = (await unitOfWork.Payments.FindAsync(
                p => p.TicketId == ticket.Id && p.Status == PaymentStatus.Pending,
                context.CancellationToken)).Count > 0;

            if (hasPendingPayment)
            {
                _logger.LogInformation($"Skipping ticket {ticket.Id} with an active pending payment");
                continue;
            }

            try
            {
                ticket.ReleaseReservation();
                await unitOfWork.SaveChangesAsync(context.CancellationToken);
                _logger.LogInformation($"Released expired reservation for ticket {ticket.Id}");
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning($"Concurrency conflict releasing ticket {ticket.Id}, skipped until next cycle");
            }
        }
    }
}
