using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Events;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.Primitives;
using TicketReservationSystem.Domain.ValueObjects;
using TicketReservationSystem.Infrastructure.DomainEventsDispatcher;

namespace TicketReservationSystem.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        private readonly IDomainEventsDispatcher _dispatcher;

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IDomainEventsDispatcher dispatcher) : base(options)
        {
            _dispatcher = dispatcher;
        }

        public DbSet<SocialEvent> SocialEvents { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<VerificationCode> VerificationCodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var userIdConverter = new ValueConverter<UserId, Guid>(
                v => v.Value,
                v => UserId.Create(v));

            var socialEventIdConverter = new ValueConverter<SocialEventId, Guid>(
                v => v.Value,
                v => SocialEventId.Create(v));

            var ticketIdConverter = new ValueConverter<TicketId, Guid>(
                v => v.Value,
                v => TicketId.Create(v));

            var verificationCodeIdConverter = new ValueConverter<VerificationCodeId, Guid>(
                v => v.Value,
                v => VerificationCodeId.Create(v));

            var paymentIdConverter = new ValueConverter<PaymentId, Guid>(
                v => v.Value,
                v => PaymentId.Create(v));

            var moneyConverter = new ValueConverter<Money, string>(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<Money>(v, JsonOptions));

            modelBuilder.Ignore<DomainEvent>();

            var dateTimeRangeConverter = new ValueConverter<DateTimeRange, string>(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<DateTimeRange>(v, JsonOptions));

            modelBuilder.Entity<SocialEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasConversion(socialEventIdConverter);
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.TimeRange).HasConversion(dateTimeRangeConverter).IsRequired();
                entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
                entity.Property(e => e.TicketPrice).HasConversion(moneyConverter);
            });

            modelBuilder.Entity<Ticket>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasConversion(ticketIdConverter);
                entity.Property(e => e.EventId).HasConversion(socialEventIdConverter);
                entity.Property(e => e.SeatNumber).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
                entity.Property(e => e.UserId).HasConversion(
                    v => v.HasValue ? v.Value.Value : (Guid?)null,
                    v => v.HasValue ? UserId.Create(v.Value) : (UserId?)null);
                entity.HasOne(e => e.SocialEvent)
                    .WithMany()
                    .HasForeignKey(e => e.EventId);
                entity.Property(e => e.Price).HasConversion(moneyConverter);
                entity.Property<byte[]>("Version").IsRowVersion();
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasConversion(paymentIdConverter);
                entity.Property(e => e.TicketId).HasConversion(ticketIdConverter);
                entity.Property(e => e.UserId).HasConversion(userIdConverter);
                entity.Property(e => e.Amount).HasConversion(moneyConverter);
                entity.Property(e => e.PaymentProvider).HasConversion<string>().HasMaxLength(20);
                entity.Property(e => e.ExternalId).HasMaxLength(200);
                entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasConversion(userIdConverter);
                entity.Property(e => e.Email).HasMaxLength(200).IsRequired();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            });

            modelBuilder.Entity<VerificationCode>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasConversion(verificationCodeIdConverter);
                entity.Property(e => e.UserId).HasConversion(userIdConverter);
                entity.Property(e => e.Code).HasMaxLength(6).IsRequired();
                entity.Property(e => e.ExpiresAt).IsRequired();
                entity.Property(e => e.IsUsed).IsRequired();
                entity.HasIndex(e => new { e.UserId, e.Code });
            });
        }

        public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
        {
            var domainEvents = ChangeTracker
            .Entries<IEntity>()
            .Select(entry => entry.Entity)
            .SelectMany(entity =>
            {
                List<DomainEvent> domainEvents = entity.DomainEvents.ToList();

                entity.ClearDomainEvents();

                return domainEvents;
            })
            .ToList();

            await _dispatcher.DispatchAsync(domainEvents, cancellationToken);

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}

