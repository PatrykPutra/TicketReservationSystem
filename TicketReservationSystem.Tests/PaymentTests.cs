using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Events;
using TicketReservationSystem.Domain.Exceptions;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.ValueObjects;

namespace TicketReservationSystem.Tests;

public class PaymentTests
{
    private static readonly Money Amount = new(150, "PLN");

    private static Payment CreatePendingPayment()
    {
        return new Payment(
            PaymentId.CreateUnique(),
            TicketId.CreateUnique(),
            UserId.CreateUnique(),
            Amount,
            PaymentProvider.Stripe,
            DateTime.UtcNow);
    }

    [Fact]
    public void Payment_ForValidInputs_IsPendingWithSnapshot()
    {
        var payment = CreatePendingPayment();

        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(Amount, payment.Amount);
        Assert.Equal(PaymentProvider.Stripe, payment.PaymentProvider);
    }

    [Fact]
    public void SetExternalId_ForValidValue_UpdatesExternalReference()
    {
        var payment = CreatePendingPayment();

        payment.SetExternalId("cs_test_123");

        Assert.Equal("cs_test_123", payment.ExternalId);
    }

    [Fact]
    public void MarkCompleted_OnPendingPayment_SetsCompleted()
    {
        var payment = CreatePendingPayment();

        payment.MarkCompleted();

        Assert.Equal(PaymentStatus.Completed, payment.Status);
    }

    [Fact]
    public void MarkCompleted_OnPendingPayment_RaisesCompletedEvent()
    {
        var payment = CreatePendingPayment();
        var userId = payment.UserId;

        payment.MarkCompleted();

        var domainEvent = payment.DomainEvents.OfType<PaymentCompletedEvent>().Single();
        Assert.Equal(userId, domainEvent.UserId);
        Assert.Equal(payment.Id, domainEvent.PaymentId);
    }

    [Fact]
    public void MarkFailed_OnPendingPayment_SetsFailed()
    {
        var payment = CreatePendingPayment();

        payment.MarkFailed();

        Assert.Equal(PaymentStatus.Failed, payment.Status);
    }

    [Fact]
    public void MarkFailed_OnPendingPayment_RaisesFailedEvent()
    {
        var payment = CreatePendingPayment();

        payment.MarkFailed();

        var domainEvent = payment.DomainEvents.OfType<PaymentFailedEvent>().Single();
        Assert.Equal(payment.Id, domainEvent.PaymentId);
    }

    [Fact]
    public void MarkExpired_OnPendingPayment_SetsExpired()
    {
        var payment = CreatePendingPayment();

        payment.MarkExpired();

        Assert.Equal(PaymentStatus.Expired, payment.Status);
    }

    [Fact]
    public void MarkExpired_OnPendingPayment_RaisesExpiredEvent()
    {
        var payment = CreatePendingPayment();

        payment.MarkExpired();

        var domainEvent = payment.DomainEvents.OfType<PaymentExpiredEvent>().Single();
        Assert.Equal(payment.Id, domainEvent.PaymentId);
    }

    [Fact]
    public void MarkCompleted_OnCompletedPayment_Throws()
    {
        var payment = CreatePendingPayment();
        payment.MarkCompleted();

        Assert.Throws<PaymentStatusException>(() => payment.MarkCompleted());
    }

    [Fact]
    public void MarkExpired_OnCompletedPayment_Throws()
    {
        var payment = CreatePendingPayment();
        payment.MarkCompleted();

        Assert.Throws<PaymentStatusException>(() => payment.MarkExpired());
    }
}