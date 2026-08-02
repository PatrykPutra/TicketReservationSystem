using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Quartz;
using TicketReservationSystem.Domain.Events;
using TicketReservationSystem.Domain.Repositories;
using TicketReservationSystem.Infrastructure.Authentication;
using TicketReservationSystem.Infrastructure.DomainEventHandlers;
using TicketReservationSystem.Infrastructure.Email;
using TicketReservationSystem.Infrastructure.InMemory;
using TicketReservationSystem.Infrastructure.Jobs;
using TicketReservationSystem.Infrastructure.Persistence;
using TicketReservationSystem.Infrastructure.Repository;
using TicketReservationSystem.Infrastructure.Payments;

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
        services.AddScoped<IEmailVerificationCodeRepository, EmailVerificationCodeRepository>();
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

        services.Configure<StripeSettings>(configuration.GetSection(StripeSettings.SectionName));
        services.AddScoped<Application.Abstractions.IPaymentsService, StripePaymentsService>();

        return services;
    }
}
