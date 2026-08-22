using Microsoft.EntityFrameworkCore;
using Quartz;
using TicketReservationSystem.Domain.Events;
using TicketReservationSystem.Domain.Repositories;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.Authentication;
using TicketReservationSystem.Application.DomainEventHandlers;
using TicketReservationSystem.Infrastructure.Services.Email;
using TicketReservationSystem.Infrastructure.Services.InMemory;
using TicketReservationSystem.Infrastructure.Services.Jobs;
using TicketReservationSystem.Infrastructure.Persistence;
using TicketReservationSystem.Infrastructure.Repository;
using TicketReservationSystem.Infrastructure.Services.Payments;

namespace TicketReservationSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("TicketReservationSystem"));

        var stripeSettings = configuration.GetSection(StripeSettings.SectionName).Get<StripeSettings>();
        Stripe.StripeConfiguration.ApiKey = stripeSettings?.SecretKey;

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IVerificationCodeRepository, VerificationCodeRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<
            DomainEventsDispatcher.IDomainEventsDispatcher,
            DomainEventsDispatcher.DomainEventsDispatcher>();

        services.AddQuartz(cfg =>
        {
            var jobKey = new JobKey(nameof(ExpiredReservationsCleanupJob));

            cfg.AddJob<ExpiredReservationsCleanupJob>(jobKey);
            cfg.AddTrigger(trigger => trigger
                .ForJob(jobKey)
                .WithSimpleSchedule(schedule => schedule
                    .WithIntervalInMinutes(10)
                    .RepeatForever()));

            var paymentJobKey = new JobKey(nameof(ExpiredPaymentsCleanupJob));

            cfg.AddJob<ExpiredPaymentsCleanupJob>(paymentJobKey);
            cfg.AddTrigger(trigger => trigger
                .ForJob(paymentJobKey)
                .WithSimpleSchedule(schedule => schedule
                    .WithIntervalInHours(1)
                    .RepeatForever()));
        });

        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        services.AddTransient<InMemorySeeder>();

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddScoped<IJwtService, JwtService>();

        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.AddScoped<IEmailSender, MimeKitEmailSender>();
        services.AddScoped<IDomainEventHandler<AuthenticationCodeGeneratedEvent>, SendAuthenticationCodeEmailHandler>();
        services.AddScoped<IDomainEventHandler<UserRegisteredEvent>, UserRegistrationEventHandler>();
        services.AddScoped<IDomainEventHandler<TicketReservedEvent>, TicketReservedEventHandler>();
        services.AddScoped<IDomainEventHandler<TicketReleasedEvent>, TicketReleasedEventHandler>();
        services.AddScoped<IDomainEventHandler<TicketConfirmedEvent>, TicketConfirmedEventHandler>();
        services.AddScoped<IDomainEventHandler<TicketCanceledEvent>, TicketCanceledEventHandler>();
        services.AddScoped<IDomainEventHandler<PaymentFailedEvent>, PaymentFailedEventHandler>();
        services.AddScoped<IDomainEventHandler<PaymentCompletedEvent>, PaymentCompletedEventHandler>();

        services.Configure<StripeSettings>(configuration.GetSection(StripeSettings.SectionName));
        services.AddScoped<IPaymentsService, StripePaymentsService>();

        return services;
    }
}
