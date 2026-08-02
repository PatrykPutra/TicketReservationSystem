# 03 — Create and register Quartz cleanup job

**What to build:** An `ExpiredReservationsCleanupJob` (Quartz `IJob`) that runs every 10 minutes. It queries all tickets with `Status == Reserved` and `ReservedAt <= threshold` via the new `FindAsync`, calls `ReleaseReservation()` on each, and handles `DbUpdateConcurrencyException` per ticket (log + skip). Add `Quartz.Extensions.Hosting` package, register the job and trigger in `DependencyInjection.cs`. Integration tests (Seam 2) seed an in-memory DB and verify the job releases only expired tickets.

**Blocked by:** #01 (FindAsync), #02 (ReleaseReservation)

**Status:** ready-for-agent

- [ ] Add `Quartz.Extensions.Hosting` NuGet package
- [ ] Create `ExpiredReservationsCleanupJob` — query expired tickets, release each with concurrency handling
- [ ] Configure Quartz in `DependencyInjection.cs` — register job, simple schedule every 10 min
- [ ] Write integration tests (Seam 2): seed tickets, run job, assert expired released, non-expired unchanged
