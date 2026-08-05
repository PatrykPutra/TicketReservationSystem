using Microsoft.EntityFrameworkCore;
using Quartz;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Exceptions;
using TicketReservationSystem.Domain.Repositories;

namespace TicketReservationSystem.Infrastructure.Services.Jobs;

[DisallowConcurrentExecution]
public class ExpiredPaymentsCleanupJob : IJob
{
    private static readonly TimeSpan PaymentExpiry = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredPaymentsCleanupJob> _logger;

    public ExpiredPaymentsCleanupJob(IServiceScopeFactory scopeFactory, ILogger<ExpiredPaymentsCleanupJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var threshold = DateTime.UtcNow.Subtract(PaymentExpiry);
        var stalePayments = await unitOfWork.Payments.FindAsync(
            p => p.Status == PaymentStatus.Pending && p.CreatedAt <= threshold,
            context.CancellationToken);

        foreach (var payment in stalePayments)
        {
            var ticket = await unitOfWork.Tickets.GetByIdAsync(payment.TicketId, context.CancellationToken);

            try
            {
                payment.MarkExpired();
                if (ticket is not null && ticket.IsReserved())
                    ticket.ReleaseReservation();

                await unitOfWork.SaveChangesAsync(context.CancellationToken);
                _logger.LogInformation($"Expired stale payment {payment.Id}");
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning($"Concurrency conflict expiring payment {payment.Id}, skipped until next cycle");
            }
            catch (PaymentStatusException)
            {
                _logger.LogWarning($"Payment {payment.Id} is no longer pending, skipped");
            }
        }
    }
}