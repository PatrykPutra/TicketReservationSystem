using TicketReservationSystem.Application.DTOs;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Tests;

public class TicketDtoTests
{
    [Fact]
    public void TicketDto_ForValidInputs_ExposesAllFields()
    {
        //  Arrange
        var ticketId = TicketId.CreateUnique();
        var eventId = SocialEventId.CreateUnique();
        var userId = UserId.CreateUnique();

        // Act
        var dto = new TicketDto(ticketId, eventId, "A1", TicketStatus.Reserved, userId, 150m, "PLN");

        // Assert
        Assert.Equal(ticketId, dto.Id);
        Assert.Equal(eventId, dto.EventId);
        Assert.Equal("A1", dto.SeatNumber);
        Assert.Equal(TicketStatus.Reserved, dto.Status);
        Assert.Equal(userId, dto.UserId);
        Assert.Equal(150m, dto.PriceAmount);
        Assert.Equal("PLN", dto.PriceCurrency);
    }

    [Fact]
    public void TicketDto_WithNullUserId_ExposesNullUserId()
    {
        // Arrange && Act
        var dto = new TicketDto(TicketId.CreateUnique(), SocialEventId.CreateUnique(), "A1", TicketStatus.Available, null, 150m, "PLN");

        // Assert
        Assert.Null(dto.UserId);
    }
}