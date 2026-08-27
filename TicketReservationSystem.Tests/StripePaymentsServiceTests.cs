using Microsoft.Extensions.Options;
using Moq;
using Stripe.Checkout;
using Stripe;
using TicketReservationSystem.Application.Errors;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.ValueObjects;
using TicketReservationSystem.Infrastructure.Services.Payments;

namespace TicketReservationSystem.Tests;

public class StripePaymentsServiceTests
{
    private static readonly PaymentId PaymentId = PaymentId.CreateUnique();

    private static StripePaymentsService CreateStripePaymentService(string currency, Mock<SessionService>? sessionService = null)
    {
        var settings = Options.Create(new StripeSettings { Currency = currency });
        return new StripePaymentsService(settings, sessionService?.Object);
    }

    private static Mock<SessionService> CreateSessionServiceMock()
    {
        var sessionService = new Mock<SessionService>();
        sessionService
            .Setup(s => s.CreateAsync(It.IsAny<SessionCreateOptions>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Session { Url = "https://checkout.url", Id = "cs_test_123" });
        return sessionService;
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_ForCurrencyMismatchError_ReturnsFailedResult()
    {
        // Arrange
        var service = CreateStripePaymentService("PLN");

        // Act
        var result = await service.CreateCheckoutSessionAsync(new Money(100, "USD"), PaymentId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<CurrencyMismatchError>(result.Error);
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_ForUnknownCurrency_ReturnsUnsupportedCurrencyErrorResult()
    {
        // Arrange
        var service = CreateStripePaymentService("XYZ");

        // Act
        var result = await service.CreateCheckoutSessionAsync(new Money(100, "XYZ"), PaymentId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<UnsupportedCurrencyError>(result.Error);
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_ForZeroDecimalCurrency_UsesDivisor1()
    {
        // Arrange
        var sessionService = new Mock<SessionService>();
        sessionService
            .Setup(s => s.CreateAsync(It.IsAny<SessionCreateOptions>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Session { Url = "https://checkout.url", Id = "cs_test_123" });

        var service = CreateStripePaymentService("JPY", sessionService);

        // Act
        var result = await service.CreateCheckoutSessionAsync(new Money(100, "JPY"), PaymentId);

        // Assert
        Assert.True(result.IsSuccess);
        var options = (SessionCreateOptions)sessionService.Invocations[0].Arguments[0];
        Assert.Equal(100, options.LineItems[0].PriceData.UnitAmount);
        Assert.Equal("JPY", options.LineItems[0].PriceData.Currency);
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_ForTwoDecimalCurrency_UsesDivisor100()
    {
        // Arrange
        var sessionService = CreateSessionServiceMock();
        var service = CreateStripePaymentService("PLN", sessionService);

        // Act
        var result = await service.CreateCheckoutSessionAsync(new Money(150, "PLN"), PaymentId);

        // Assert
        Assert.True(result.IsSuccess);
        var options = (SessionCreateOptions)sessionService.Invocations[0].Arguments[0];
        Assert.Equal(15000, options.LineItems[0].PriceData.UnitAmount);
        Assert.Equal("PLN", options.LineItems[0].PriceData.Currency);
    }
}
