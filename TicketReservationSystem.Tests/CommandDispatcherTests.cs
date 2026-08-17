using MediatR;
using Moq;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.Commands;
using TicketReservationSystem.Application.Commands.Tickets;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Tests;

public class CommandDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_ForCommand_ForwardsToMediatorAndReturnsResult()
    {
        var mediator = new Mock<IMediator>();
        var command = new TicketReservationCommand(TicketId.CreateUnique(), UserId.CreateUnique());
        var expected = TicketReservationResult.Success(command.TicketId, TicketStatus.Reserved, DateTime.UtcNow);
        mediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var dispatcherType = typeof(ICommandDispatcher).Assembly
            .GetType("TicketReservationSystem.Application.Commands.CommandDispatcher")!;
        var dispatcher = (ICommandDispatcher)Activator.CreateInstance(dispatcherType, mediator.Object)!;

        var result = await dispatcher.DispatchAsync<TicketReservationCommand, TicketReservationResult>(command, CancellationToken.None);

        Assert.Same(expected, result);
        mediator.Verify(m => m.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }
}