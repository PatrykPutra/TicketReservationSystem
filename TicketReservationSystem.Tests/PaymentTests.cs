using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Events;
using TicketReservationSystem.Domain.Exceptions;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.ValueObjects;

namespace TicketReservationSystem.Tests;

public class PaymentTests
{
    private static readonly Money DefaultAmount = new(150, "PLN");

    [Fact]
    public void Payment_ForNewlyCreatedPayment_PaymentStatusIsPending()
    {
        // Arrange & Act
        var payment = new Payment(
            PaymentId.CreateUnique(),
            TicketId.CreateUnique(),
            UserId.CreateUnique(),
            DefaultAmount,
            PaymentProvider.Stripe,
            DateTime.UtcNow);

        // Assert
        Assert.Equal(PaymentStatus.Pending, payment.Status);
    }

    [Fact]
    public void Payment_ForNewlyCreatedPayment_PropertiesAreSetCorrectly()
    {
        // Arrange
        var paymentId = PaymentId.CreateUnique();
        var ticketId = TicketId.CreateUnique();
        var userId = UserId.CreateUnique();
        var paymentProvider = PaymentProvider.Stripe;
        var createdAt = DateTime.UtcNow;

        // Act
        var payment = new Payment(
            paymentId,
            ticketId,
            userId,
            DefaultAmount,
            paymentProvider,
            createdAt);

        // Assert
        Assert.Equal(paymentId, payment.Id);
        Assert.Equal(ticketId, payment.TicketId);
        Assert.Equal(userId, payment.UserId);
        Assert.Equal(DefaultAmount, payment.Amount);
        Assert.Equal(paymentProvider, payment.PaymentProvider);
        Assert.Equal(createdAt, payment.CreatedAt);
    }

    [Fact]
    public void SetExternalId_ForValidValue_ExternalIdIsSetCorrectly()
    {
        // Arrange
        var payment = new Payment(
            PaymentId.CreateUnique(),
            TicketId.CreateUnique(),
            UserId.CreateUnique(),
            DefaultAmount,
            PaymentProvider.Stripe,
            DateTime.UtcNow);
        
        // Act
        payment.SetExternalId("cs_test_123");

        // Assert
        Assert.Equal("cs_test_123", payment.ExternalId);
    }

    [Fact]
    public void MarkCompleted_ForPendingPayment_SetsStatusToCompleted()
    {
        // Arrange
        var payment = new Payment(
            PaymentId.CreateUnique(),
            TicketId.CreateUnique(),
            UserId.CreateUnique(),
            DefaultAmount,
            PaymentProvider.Stripe,
            DateTime.UtcNow);

        // Act
        payment.MarkCompleted();

        // Assert
        Assert.Equal(PaymentStatus.Completed, payment.Status);
    }

    [Fact]
    public void MarkCompleted_ForPendingPayment_RaisesPaymentCompletedEvent()
    {
        // Arrange
        var payment = new Payment(
            PaymentId.CreateUnique(),
            TicketId.CreateUnique(),
            UserId.CreateUnique(),
            DefaultAmount,
            PaymentProvider.Stripe,
            DateTime.UtcNow);

        var userId = payment.UserId;

        // Act
        payment.MarkCompleted();

        // Assert
        var domainEvent = payment.DomainEvents.OfType<PaymentCompletedEvent>().Single();
        Assert.NotNull(domainEvent);
        Assert.Equal(userId, domainEvent.UserId);
        Assert.Equal(payment.Id, domainEvent.PaymentId);
    }

    [Fact]
    public void MarkCompleted_WhenPaymentCompletedEventIsRaised_EventPropertiesAseSerCorrectly()
    {
        // Arrange
        var payment = new Payment(
            PaymentId.CreateUnique(),
            TicketId.CreateUnique(),
            UserId.CreateUnique(),
            DefaultAmount,
            PaymentProvider.Stripe,
            DateTime.UtcNow);

        var userId = payment.UserId;

        // Act
        payment.MarkCompleted();

        // Assert
        var domainEvent = payment.DomainEvents.OfType<PaymentCompletedEvent>().Single();
        Assert.Equal(userId, domainEvent.UserId);
        Assert.Equal(payment.Id, domainEvent.PaymentId);
    }



    [Fact]
    public void MarkFailed_ForPendingPayment_SetsStatusToFailed()
    {
        // Arrange
        var payment = new Payment(
            PaymentId.CreateUnique(),
            TicketId.CreateUnique(),
            UserId.CreateUnique(),
            DefaultAmount,
            PaymentProvider.Stripe,
            DateTime.UtcNow);

        // Act
        payment.MarkFailed();

        // Assert
        Assert.Equal(PaymentStatus.Failed, payment.Status);
    }

    [Fact]
    public void MarkFailed_ForPendingPayment_RaisesFailedEvent()
    {
        // Arrange
        var payment = new Payment(
            PaymentId.CreateUnique(),
            TicketId.CreateUnique(),
            UserId.CreateUnique(),
            DefaultAmount,
            PaymentProvider.Stripe,
            DateTime.UtcNow);

        // Act
        payment.MarkFailed();

        // Assert
        var domainEvent = payment.DomainEvents.OfType<PaymentFailedEvent>().Single();
        Assert.NotNull(domainEvent);
    }

    [Fact]
    public void MarkFailed_WhenPaymentFailedEventIsRaised_EventPropertiesAreSetCorrectly()
    {
        // Arrange
        var payment = new Payment(
            PaymentId.CreateUnique(),
            TicketId.CreateUnique(),
            UserId.CreateUnique(),
            DefaultAmount,
            PaymentProvider.Stripe,
            DateTime.UtcNow);

        // Act
        payment.MarkFailed();

        // Assert
        var domainEvent = payment.DomainEvents.OfType<PaymentFailedEvent>().Single();
        Assert.Equal(payment.Id, domainEvent.PaymentId);
    }

    [Fact]
    public void MarkExpired_ForPendingPayment_SetsStatusToExpired()
    {
        // Arrange
        var payment = new Payment(
            PaymentId.CreateUnique(),
            TicketId.CreateUnique(),
            UserId.CreateUnique(),
            DefaultAmount,
            PaymentProvider.Stripe,
            DateTime.UtcNow);

        // Act
        payment.MarkExpired();

        // Assert
        Assert.Equal(PaymentStatus.Expired, payment.Status);
    }

    [Fact]
    public void MarkExpired_ForPendingPayment_RaisesPaymentExpiredEvent()
    {
        // Arrange
        var payment = new Payment(
            PaymentId.CreateUnique(),
            TicketId.CreateUnique(),
            UserId.CreateUnique(),
            DefaultAmount,
            PaymentProvider.Stripe,
            DateTime.UtcNow);

        // Act
        payment.MarkExpired();

        // Assert
        var domainEvent = payment.DomainEvents.OfType<PaymentExpiredEvent>().Single();
        Assert.NotNull(domainEvent);
    }

    [Fact]
    public void MarkExpired_WhenPaymentExpiredEventIsRaised_EventPropertiesAreSetCorrectly()
    {
        // Arrange
        var payment = new Payment(
            PaymentId.CreateUnique(),
            TicketId.CreateUnique(),
            UserId.CreateUnique(),
            DefaultAmount,
            PaymentProvider.Stripe,
            DateTime.UtcNow);

        // Act
        payment.MarkExpired();

        // Assert
        var domainEvent = payment.DomainEvents.OfType<PaymentExpiredEvent>().Single();
        Assert.Equal(payment.Id, domainEvent.PaymentId);
    }

    [Fact]
    public void MarkCompleted_ForCompletedPayment_ThrowsPaymentStatusException()
    {
        //  Arrange
        var payment = new Payment(
            PaymentId.CreateUnique(),
            TicketId.CreateUnique(),
            UserId.CreateUnique(),
            DefaultAmount,
            PaymentProvider.Stripe,
            DateTime.UtcNow);
        
        // Act
        payment.MarkCompleted();

        // Assert
        Assert.Throws<PaymentStatusException>(() => payment.MarkCompleted());
    }

    [Fact]
    public void MarkExpired_ForCompletedPayment_ThrowsPaymentStatusException()
    {
        // Arrange
        var payment = new Payment(
            PaymentId.CreateUnique(),
            TicketId.CreateUnique(),
            UserId.CreateUnique(),
            DefaultAmount,
            PaymentProvider.Stripe,
            DateTime.UtcNow);
        
        // Act
        payment.MarkCompleted();

        // Assert
        Assert.Throws<PaymentStatusException>(() => payment.MarkExpired());
    }
}