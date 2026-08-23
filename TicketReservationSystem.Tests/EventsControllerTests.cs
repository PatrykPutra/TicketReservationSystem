using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TicketReservationSystem.API.Controllers;
using TicketReservationSystem.Application.Abstractions;
using TicketReservationSystem.Application.DTOs;
using TicketReservationSystem.Application.Queries.Events;
using TicketReservationSystem.Domain.Entities;
using TicketReservationSystem.Domain.Ids;

namespace TicketReservationSystem.Tests;

public class EventsControllerTests
{
    private static EventDto CreateEventDto()
    {
        DateTime start = DateTime.UtcNow.AddDays(30);
        DateTime end = start.AddHours(3);

        return new EventDto(
            SocialEventId.CreateUnique(),
            "Test Event",
            "Description",
            start,
            end,
            100,
            80,
            20,
            EventStatus.Scheduled,
            150m,
            "PLN");
    }

    private static EventsController CreateController(Action<Mock<IQueryDispatcher>>? querySetup = null)
    {
        var queryDispatcherMock = new Mock<IQueryDispatcher>();
        querySetup?.Invoke(queryDispatcherMock);

        var controller = new EventsController(queryDispatcherMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        return controller;
    }

    [Fact]
    public async Task GetEventById_WhenEventFound_ReturnsOk()
    {
        // Arrange
        var eventDto = CreateEventDto();
        var controller = CreateController(querySetup: mock =>
        {
            mock.Setup(d => d.ExecuteAsync<GetEventByIdQuery, GetEventByIdResult>(
                    It.IsAny<GetEventByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetEventByIdResult(eventDto));
        });

        // Act
        var result = await controller.GetEventById(Guid.NewGuid());

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(eventDto, ok.Value);
    }

    [Fact]
    public async Task GetEventById_WhenEventMissing_ReturnsNotFound()
    {
        // Arrange
        var controller = CreateController(querySetup: mock =>
        {
            mock.Setup(d => d.ExecuteAsync<GetEventByIdQuery, GetEventByIdResult>(
                    It.IsAny<GetEventByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetEventByIdResult(null));
        });

        // Act
        var result = await controller.GetEventById(Guid.NewGuid());

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetEvents_ForExistingEvents_ReturnsOkWithEvents()
    {
        // Arrange
        var events = new List<EventDto> { CreateEventDto(), CreateEventDto() };
        var controller = CreateController(querySetup: mock =>
        {
            mock.Setup(d => d.ExecuteAsync<GetEventsQuery, GetEventsResult>(
                    It.IsAny<GetEventsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetEventsResult(events));
        });

        // Act
        var result = await controller.GetEvents();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(events, ok.Value);
    }

    [Fact]
    public void GetEventById_Documentation_ContainsStatus200OK()
    {
        Assert.Contains(StatusCodes.Status200OK, GetDocumentedCodes(nameof(EventsController.GetEventById)));
    }

    [Fact]
    public void GetEventById_Documentation_ContainsStatus404NotFound()
    {
        Assert.Contains(StatusCodes.Status404NotFound, GetDocumentedCodes(nameof(EventsController.GetEventById)));
    }

    [Fact]
    public void GetEventById_Documentation_ContainsStatus500InternalServerError()
    {
        Assert.Contains(StatusCodes.Status500InternalServerError, GetDocumentedCodes(nameof(EventsController.GetEventById)));
    }

    [Fact]
    public void GetEvents_Documentation_ContainsStatus200OK()
    {
        Assert.Contains(StatusCodes.Status200OK, GetDocumentedCodes(nameof(EventsController.GetEvents)));
    }

    [Fact]
    public void GetEvents_Documentation_ContainsStatus500InternalServerError()
    {
        Assert.Contains(StatusCodes.Status500InternalServerError, GetDocumentedCodes(nameof(EventsController.GetEvents)));
    }

    private static int[] GetDocumentedCodes(string methodName) =>
        typeof(EventsController).GetMethod(methodName)!
            .GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Select(a => a.StatusCode)
            .ToArray();
}
