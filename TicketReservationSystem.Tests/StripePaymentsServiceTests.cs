using Microsoft.Extensions.Options;
using Moq;
using Stripe;
using Stripe.Checkout;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Domain.Ids;
using TicketReservationSystem.Domain.ValueObjects;
using TicketReservationSystem.Infrastructure.Services.Payments;

namespace TicketReservationSystem.Tests;

public class StripePaymentsServiceTests
{
    private static readonly PaymentId PaymentId = PaymentId.CreateUnique();

    private static StripePaymentsService CreateService(string currency, Mock<SessionService>? sessionService = null)
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
    public async Task CreateCheckoutSessionAsync_currency_mismatch_returns_failed_result()
    {
        var service = CreateService("PLN");

        var result = await service.CreateCheckoutSessionAsync(new Money(100, "USD"), PaymentId);

        Assert.True(result.IsFailure);
        Assert.IsType<CurrencyMismatchError>(result.Error);
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_unknown_currency_returns_unsupported_currency()
    {
        var service = CreateService("XYZ");

        var result = await service.CreateCheckoutSessionAsync(new Money(100, "XYZ"), PaymentId);

        Assert.True(result.IsFailure);
        Assert.IsType<UnsupportedCurrencyError>(result.Error);
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_zero_decimal_currency_uses_divisor_1()
    {
        var sessionService = CreateSessionServiceMock();
        var service = CreateService("JPY", sessionService);

        var result = await service.CreateCheckoutSessionAsync(new Money(100, "JPY"), PaymentId);

        Assert.True(result.IsSuccess);
        var options = (SessionCreateOptions)sessionService.Invocations[0].Arguments[0];
        Assert.Equal(100, options.LineItems[0].PriceData.UnitAmount);
        Assert.Equal("JPY", options.LineItems[0].PriceData.Currency);
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_two_decimal_currency_uses_divisor_100()
    {
        var sessionService = CreateSessionServiceMock();
        var service = CreateService("PLN", sessionService);

        var result = await service.CreateCheckoutSessionAsync(new Money(150, "PLN"), PaymentId);

        Assert.True(result.IsSuccess);
        var options = (SessionCreateOptions)sessionService.Invocations[0].Arguments[0];
        Assert.Equal(15000, options.LineItems[0].PriceData.UnitAmount);
        Assert.Equal("PLN", options.LineItems[0].PriceData.Currency);
    }
}
