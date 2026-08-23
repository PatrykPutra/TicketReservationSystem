using MediatR;
using Moq;
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
        // Arrange
        var mediatorMock = new Mock<IMediator>();
        var command = new TicketReservationCommand(TicketId.CreateUnique(), UserId.CreateUnique());
        var expected = TicketReservationResult.Success(command.TicketId, TicketStatus.Reserved, DateTime.UtcNow);
        mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var dispatcher = new CommandDispatcher(mediatorMock.Object);

        // Act
        var result = await dispatcher.DispatchAsync<TicketReservationCommand, TicketReservationResult>(command, CancellationToken.None);

        // Assert
        Assert.Same(expected, result);
        mediatorMock.Verify(m => m.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }
}
