# 01 — Prefactor: Add `FindAsync` to ticket repository

**What to build:** The `ITicketRepository` interface and `TicketRepository` implementation gain a general-purpose `FindAsync(Expression<Func<Ticket, bool>> predicate, CancellationToken cancellationToken)` method that returns matching tickets with their `SocialEvent` navigation included. This enables the cleanup job to query for expired reservations without coupling the repository to domain-specific queries.

**Blocked by:** None — can start immediately.

**Status:** ready-for-agent

- [ ] Add `FindAsync` signature to `ITicketRepository`
- [ ] Implement `FindAsync` in `TicketRepository` using EF Core `.Where(predicate).Include(t => t.SocialEvent)`
- [ ] Verify it compiles
