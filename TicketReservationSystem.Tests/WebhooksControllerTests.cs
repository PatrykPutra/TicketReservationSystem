using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Stripe;
using TicketReservationSystem.API.Controllers;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.Commands.Payments;
using TicketReservationSystem.Application.Errors;
using TicketReservationSystem.Infrastructure.Services.Payments;

namespace TicketReservationSystem.Tests;

public class WebhooksControllerTests
{
    private const string WebhookSecret = "whsec_test_secret";

    private static string Payload =>
        $"{{\"id\":\"evt_test_123\",\"object\":\"event\",\"api_version\":\"{StripeConfiguration.ApiVersion}\"}}";

    private static string ComputeSignature(long timestamp)
    {
        var signedPayload = $"{timestamp}.{Payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(WebhookSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static WebhooksController CreateController(
        string? signatureHeader,
        Action<Mock<ICommandDispatcher>>? commandSetup = null)
    {
        var commandDispatcherMock = new Mock<ICommandDispatcher>();
        commandSetup?.Invoke(commandDispatcherMock);

        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(Payload));
        if (!string.IsNullOrEmpty(signatureHeader))
            context.Request.Headers["Stripe-Signature"] = signatureHeader;

        var controller = new WebhooksController(
            commandDispatcherMock.Object,
            Options.Create(new StripeSettings { WebhookSecret = WebhookSecret }))
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
        return controller;
    }

    [Fact]
    public async Task StripeWebhook_WhenSignatureMissing_ReturnsBadRequest()
    {
        var controller = CreateController(signatureHeader: null);

        var result = await controller.StripeWebhook();

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task StripeWebhook_WhenSignatureInvalid_ReturnsBadRequest()
    {
        var controller = CreateController("t=123,v1=deadbeef");

        var result = await controller.StripeWebhook();

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task StripeWebhook_WhenSignatureValidAndCommandSucceeds_ReturnsOk()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var controller = CreateController(
            $"t={timestamp},v1={ComputeSignature(timestamp)}",
            commandSetup: mock =>
            {
                mock.Setup(d => d.DispatchAsync<StripeWebhookCommand, Result>(
                        It.IsAny<StripeWebhookCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Success());
            });

        var result = await controller.StripeWebhook();

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task StripeWebhook_WhenCommandFails_Returns500()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var controller = CreateController(
            $"t={timestamp},v1={ComputeSignature(timestamp)}",
            commandSetup: mock =>
            {
                mock.Setup(d => d.DispatchAsync<StripeWebhookCommand, Result>(
                        It.IsAny<StripeWebhookCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Failure(new NotFoundError("Payment not found")));
            });

        var result = await controller.StripeWebhook();

        var statusCode = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(500, statusCode.StatusCode);
    }

    [Fact]
    public void StripeWebhook_Documentation_ContainsStatus200OK()
    {
        Assert.Contains(StatusCodes.Status200OK, GetDocumentedCodes(nameof(WebhooksController.StripeWebhook)));
    }

    [Fact]
    public void StripeWebhook_Documentation_ContainsStatus400BadRequest()
    {
        Assert.Contains(StatusCodes.Status400BadRequest, GetDocumentedCodes(nameof(WebhooksController.StripeWebhook)));
    }

    [Fact]
    public void StripeWebhook_Documentation_ContainsStatus500InternalServerError()
    {
        Assert.Contains(StatusCodes.Status500InternalServerError, GetDocumentedCodes(nameof(WebhooksController.StripeWebhook)));
    }

    private static int[] GetDocumentedCodes(string methodName) =>
        typeof(WebhooksController).GetMethod(methodName)!
            .GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Select(a => a.StatusCode)
            .ToArray();
}
