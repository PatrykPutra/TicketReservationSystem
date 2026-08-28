using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Stripe;
using TicketReservationSystem.API.Controllers;
using TicketReservationSystem.API.Helpers;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.Commands.Payments;
using TicketReservationSystem.Application.Errors;

namespace TicketReservationSystem.Tests;

public class WebhooksControllerTests
{
    private static string Payload =>
        $"{{\"id\":\"evt_test_123\",\"object\":\"event\",\"api_version\":\"{StripeConfiguration.ApiVersion}\"}}";

    private static WebhooksController CreateController(
        string? signatureHeader,
        IStripeHelperService stripeHelperService,
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
            stripeHelperService)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
        return controller;
    }

    [Fact]
    public async Task StripeWebhook_WhenSignatureMissing_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController(signatureHeader: null, Mock.Of<IStripeHelperService>());

        // Act
        var result = await controller.StripeWebhook();

        // Assert
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task StripeWebhook_ForStripeException_ReturnsBadRequest()
    {
        // Arrange
        var stripeHelperServiceMock = new Mock<IStripeHelperService>();
        stripeHelperServiceMock
            .Setup(s => s.ConstructEvent(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new StripeException() );
        var controller = CreateController("NotNullSignatureHeader", stripeHelperServiceMock.Object);

        // Act
        var result = await controller.StripeWebhook();

        // Assert
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task StripeWebhook_ForSuccesResult_ReturnsOk()
    {
        // Arrange
        var stripeHelperServiceMock = new Mock<IStripeHelperService>();
        stripeHelperServiceMock
            .Setup(s => s.ConstructEvent(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new Stripe.Event());

        var controller = CreateController(
            "NotNullSignatureHeader",
            stripeHelperServiceMock.Object,
            commandSetup: mock =>
            {
                mock.Setup(d => d.DispatchAsync<StripeWebhookCommand, Result>(
                        It.IsAny<StripeWebhookCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Success());
            });

        // Act
        var result = await controller.StripeWebhook();

        // Assert
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task StripeWebhook_ForFailedResult_Returns500()
    {
        // Arrange
        var stripeHelperServiceMock = new Mock<IStripeHelperService>();
        stripeHelperServiceMock
            .Setup(s => s.ConstructEvent(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new Stripe.Event());

        var controller = CreateController(
            "NotNullSignatureHeader",
            stripeHelperServiceMock.Object,
            commandSetup: mock =>
            {
                mock.Setup(d => d.DispatchAsync<StripeWebhookCommand, Result>(
                        It.IsAny<StripeWebhookCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Failure(new NotFoundError("Payment not found")));
            });

        // Act
        var result = await controller.StripeWebhook();

        // Assert
        var statusCode = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(500, statusCode.StatusCode);
    }

    [Fact]
    public void StripeWebhook_Documentation_ContainsStatus200OK()
    {
        // Arrange && Act && Assert
        Assert.Contains(StatusCodes.Status200OK, GetDocumentedCodes(nameof(WebhooksController.StripeWebhook)));
    }

    [Fact]
    public void StripeWebhook_Documentation_ContainsStatus400BadRequest()
    {
        // Arrange && Act && Assert
        Assert.Contains(StatusCodes.Status400BadRequest, GetDocumentedCodes(nameof(WebhooksController.StripeWebhook)));
    }

    [Fact]
    public void StripeWebhook_Documentation_ContainsStatus500InternalServerError()
    {
        // Arrange && Act && Assert
        Assert.Contains(StatusCodes.Status500InternalServerError, GetDocumentedCodes(nameof(WebhooksController.StripeWebhook)));
    }

    private static int[] GetDocumentedCodes(string methodName) =>
        typeof(WebhooksController).GetMethod(methodName)!
            .GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Select(a => a.StatusCode)
            .ToArray();
}
