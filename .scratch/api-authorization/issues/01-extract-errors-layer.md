# 01 — Extract Errors layer under Application

**What to build:** The Errors folder moves out from under Abstractions to sit directly under Application, and each error type's namespace changes to `TicketReservationSystem.Application.Errors`. This keeps the folder structure aligned with the layering, with no behavioral change.

**Blocked by:** None — can start immediately.

**Status:** ready-for-agent

- [ ] The Errors folder with all 13 error types sits directly under Application.
- [ ] Each error type lives in the `TicketReservationSystem.Application.Errors` namespace; the base `Error` record and `Result`/`Result<T>` stay in `Application.Abstractions`.
- [ ] Every consumer (handlers, infrastructure services, controllers, test files) compiles via per-file usings for the new namespace.
- [ ] `dotnet build` and the existing test suite pass with no behavior change.
