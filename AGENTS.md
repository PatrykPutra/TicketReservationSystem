# AGENTS.md

Application for selling a limited pool of tickets for popular events. The platform must be resilient to scenarios where multiple users attempt to purchase the same ticket within the exact same fraction of a second.

## Requirements

- Race condition resistance — handles concurrent purchase attempts safely
- Temporary reservation — if a ticket is not paid within 5 minutes, it returns to the pool of available tickets
- Real-time — emits the current number of available tickets to all connected clients

## Stack

- .NET 10 ASP.NET Core Web API, target `net10.0`
- DDD-style domain layer (`Domain/Ids/`, `Domain/Entities/`, `Domain/Events/`, `Domain/ValueObjects/`, `Domain/Exceptions/`, `Domain/Primitives/`, `Domain/Repositories/`)
- MediatR 14.x for CQRS (`ICommand<T>` / `IQuery<T>` via MediatR `IRequest<T>`)
- Custom `IDomainEventsDispatcher` + `IDomainEventHandler<T>` for in-process domain event dispatch (not MediatR)
- EntityFrameworkCore 10.x (`ApplicationDbContext` with domain event dispatch on `SaveChangesAsync`)
- Repository pattern — `IEventRepository` / `ITicketRepository` / `IUserRepository` / `IUnitOfWork` interfaces + stubs in `Infrastructure/Repository/`
- Single project in `.slnx` solution format

## Commands

```powershell
dotnet build              # build
dotnet run --project TicketReservationSystem  # start API (http://localhost:5218)
dotnet watch run --project TicketReservationSystem  # hot-reload dev
```

## Project Structure

```
TicketReservationSystem/
├── API/
│   └── Controllers/
│       ├── AuthenticationController.cs       # POST /api/authentication/login
│       ├── EventsController.cs               # GET /api/events, GET /api/events/{id}
│       ├── TicketsController.cs              # GET/POST reservation/confirmation/cancel
│       └── UserController.cs                 # User CRUD
├── Application/
│   ├── Abstractions/
│   │   └── ITicketAvailabilityNotifier.cs    # Interface for future SignalR broadcast
│   ├── Commands/
│   │   ├── ICommand.cs                       # ICommand<TResponse> : IRequest<TResponse>
│   │   ├── ICommandHandler.cs                # ICommandHandler<T,TResponse> : IRequestHandler
│   │   ├── Authentication/
│   │   │   ├── AuthenticationCommand.cs
│   │   │   ├── AuthenticationHandler.cs
│   │   │   └── AuthenticationResult.cs
│   │   ├── Events/                           # Empty (folder placeholder)
│   │   ├── Tickets/
│   │   │   ├── TicketReservationCommand.cs / Handler.cs / Result.cs
│   │   │   ├── TicketConfirmationCommand.cs / Handler.cs / Result.cs
│   │   │   └── TicketCancelationCommand.cs / Handler.cs / Result.cs
│   │   └── Users/
│   │       ├── AddUserCommand.cs / Handler.cs / Result.cs
│   ├── DomainEventHandlers/                  # IDomainEventHandler<T> implementations
│   │   ├── TicketReservedDomainEventHandler.cs      # Stub
│   │   ├── TicketConfirmetDomainEventHandler.cs     # Stub (typo preserved)
│   │   ├── TicketReleasedDomainEventHandler.cs      # Stub
│   │   └── TicketCanceledDomainEventHandler.cs      # Stub
│   ├── Queries/
│   │   ├── IQuery.cs                         # IQuery<T> : IRequest<T>
│   │   ├── IQueryHandler.cs                  # IQueryHandler<T,T> : IRequestHandler
│   │   ├── Events/
│   │   │   ├── GetEventsQuery.cs / Handler.cs / Result.cs
│   │   │   └── GetEventByIdQuery.cs / Handler.cs / Result.cs
│   │   ├── Tickets/
│   │   │   ├── GetTicketByIdQuery.cs / Handler.cs / Result.cs
│   │   │   └── GetTicketsByEventQuery.cs / Handler.cs / Result.cs
│   │   └── Users/
│   │       └── GetUserQuery.cs / Handler.cs / Result.cs
│   └── Requests/                             # Request DTOs for controllers
│       ├── AddUserRequest.cs
│       ├── AuthenticationRequest.cs
│       ├── TicketCancelationRequest.cs
│       ├── TicketConfirmationRequest.cs
│       └── TicketReservationRequest.cs
├── Domain/
│   ├── Entities/
│   │   ├── EventStatus.cs                    # Enum: Scheduled, Ongoing, Ended, SoldOut
│   │   ├── SocialEvent.cs                    # AggregateRoot<SocialEventId>
│   │   ├── Ticket.cs                         # AggregateRoot<TicketId>
│   │   ├── TicketStatus.cs                   # Enum: Available, Reserved, Confirmed
│   │   └── User.cs                           # AggregateRoot<UserId>
│   ├── Events/
│   │   ├── DomainEvent.cs                    # Base class (no MediatR dependency)
│   │   ├── EmailVerifiedEvent.cs
│   │   ├── IDomainEventHandler.cs            # Generic handler interface
│   │   ├── TicketCanceledEvent.cs
│   │   ├── TicketConfirmedEvent.cs
│   │   ├── TicketReleasedEvent.cs
│   │   ├── TicketReservedEvent.cs
│   │   └── UserRegisteredEvent.cs
│   ├── Exceptions/
│   │   ├── DomainException.cs                # Base (internal abstract, extends InvalidOperationException)
│   │   ├── CurrencyMismatchException.cs
│   │   ├── EventNotAcceptingReservationsException.cs
│   │   ├── InsufficientNumberOfTicketsException.cs
│   │   ├── ReservationNotAvailableException.cs
│   │   ├── TicketNotAvailableException.cs
│   │   ├── TicketNotConfirmedException.cs
│   │   ├── TicketNotReservedException.cs
│   │   ├── TicketSeatIndexValidationException.cs
│   │   ├── UnauthorizedUserException.cs
│   │   └── UserTicketInvariantViolationException.cs
│   ├── Ids/
│   │   ├── SocialEventId.cs                  # readonly record struct SocialEventId(Guid Value)
│   │   ├── TicketId.cs                       # readonly record struct TicketId(Guid Value)
│   │   └── UserId.cs                         # readonly record struct UserId(Guid Value)
│   ├── Primitives/
│   │   ├── IEntity.cs                        # Interface exposing DomainEvents collection
│   │   ├── Entity.cs                         # Base entity (IEquatable, domain events list)
│   │   └── AggregateRoot.cs                  # Aggregate root base (Entity<TKey> + invariants)
│   ├── Repositories/
│   │   ├── IEventRepository.cs
│   │   ├── ITicketRepository.cs
│   │   ├── IUnitOfWork.cs                    # Facade over all repositories
│   │   └── IUserRepository.cs
│   └── ValueObjects/
│       ├── DateTimeRange.cs
│       └── Money.cs
├── Infrastructure/
│   ├── DomainEventsDispatcher/
│   │   ├── IDomainEventsDispatcher.cs        # Interface for dispatching DomainEvent collections
│   │   └── DomainEventsDispatcher.cs         # Reflection-based handler resolution
│   ├── Persistence/
│   │   └── ApplicationDbContext.cs           # EF Core DbContext (domain events on SaveChanges)
│   └── Repository/
│       ├── EventRepository.cs                # IEventRepository stub
│       ├── TicketRepository.cs               # ITicketRepository stub
│       ├── UnitOfWork.cs                     # IUnitOfWork implementation
│       └── UserRepository.cs                 # IUserRepository stub
└── Program.cs                                # Entry point + DI wiring (MediatR, OpenApi)
```

## Data Flow

### CQRS Pipeline

```
HTTP Request → Controller → MediatR ICommand<T> / IQuery<T>
                              │
                              ▼
                        CommandHandler / QueryHandler
                              │
                              ▼ (domain operations)
                    SocialEvent / Ticket / User aggregate
                              │
                              ▼ (AddDomainEvent)
                         DomainEvent
                              │
                              ▼ (IDomainEventsDispatcher.DispatchAsync)
                    IDomainEventHandler<T> (Application layer)
```

### Ticket Cancellation

```
Ticket.Cancel(UserId userId)
        │
        ├── validates Status == Confirmed, UserId == userId
        ├── updates Status → Available, resets UserId/ConfirmedAt
        └── adds TicketCanceledEvent(TicketId, UserId, EventId)
                │
                ▼
          IDomainEventsDispatcher → TicketCanceledDomainEventHandler (stub)
```

## API

- Swagger/OpenAPI at `/openapi/v1.json` in Development only (see `Program.cs`)
- `TicketReservationSystem.http` — manual request scratchpad (port 5218)
- Launch profiles: `http` (5218) and `https` (7116)

### Controllers

| Controller | Route | Actions |
|---|---|---|
| `EventsController` | `api/events` | GET (list all), GET (by id) |
| `TicketsController` | `api/tickets` | GET by id, GET by event, POST reserve, POST confirm, POST cancel |
| `UserController` | `api/user` | CRUD stubs |
| `AuthenticationController` | `api/authentication` | POST login |

## Quirks

- All strongly-typed IDs (`SocialEventId`, `TicketId`, `UserId`) are `readonly record struct` backed by `Guid Value` — no implicit conversion from/to `long` or `Guid`
- `TicketReservedEvent` carries `TicketId`, `UserId?`, `SocialEventId`, `ReservedAt` — no `Quantity` (quantity is managed by `SocialEvent.ReserveTickets`)
- `TicketStatus` has only three values: `Available`, `Reserved`, `Confirmed` (no `SoldOut`, `Cancelled`)
- `DomainException` base is `internal abstract` — all domain exceptions in this assembly
- Controllers live in `API/Controllers/` (not top-level `Controllers/`)
- `Domain/Primitives/` contains `Entity<TId>`, `IEntity`, and `AggregateRoot<TKey>` (correctly spelled — no `AgregateRoot` typo)
- `IDomainEventsDispatcher` dispatches to `IDomainEventHandler<T>` handlers via reflection (not MediatR) — separate from the CQRS MediatR pipeline
- `ApplicationDbContext` dispatches domain events on `SaveChangesAsync` before persisting
- All command/query handlers and domain event handlers are stubs (`throw new NotImplementedException()`)
- `Infrastructure/Repository/UnitOfWork.cs` exists and implements `IUnitOfWork`
- `IDomainEventsDispatcher` is not registered in DI (removed from `Program.cs`)
- There is no timeout/release mechanism yet — `Infrastructure/BackgroundServices/` and `Infrastructure/InMemory/` are placeholders for future implementation
- `Application/Commands/Events/` folder exists but is empty (placeholder)

## Planned / Not Yet Implemented

- `Infrastructure/BackgroundServices/ReservationTimeoutItem.cs` — record passed through the channel
- `Infrastructure/BackgroundServices/ReservationTimeoutService.cs` — BackgroundService, 5-min timer per reservation
- `Infrastructure/InMemory/ReservationTracker.cs` — in-memory store (ConcurrentDictionary) + timeout CTS registry
- `AddReservationTimeout()` extension method in DI
- `ITicketAvailabilityNotifier` implementation — SignalR hub broadcast for real-time ticket count
- Wiring of `IDomainEventsDispatcher` into DI
- Domain event handlers writing to timeout channel and calling notifier

## ToDo
- Add EventId to a TicketReservationRequest
