# 01 — Payment domain + persistence foundation

**What to build:** A reusable `Payment` aggregate that can represent a checkout payment for a single ticket, alongside ticket state. The aggregate owns a `PaymentId`, references the ticket and acting user, snapshots the money (amount + currency) at creation, stores the Stripe session id, and moves through defined statuses (`Pending → Completed / Failed / Expired`), raising domain events on those transitions. It is persisted via a repository wired onto the unit of work and mapped in the database, following the ticket entity's conventions (strong id value converter, enum handling, domain-event dispatch on save).

**Blocked by:** None — can start immediately.

**Status:** ready-for-agent

- [ ] New `Payment` aggregate root with a strong `PaymentId`, referencing `TicketId` and `UserId`, a snapshot of the money amount + currency, a Stripe session id, a status, and timestamps.
- [ ] `PaymentStatus` with the four states: `Pending`, `Completed`, `Failed`, `Expired`.
- [ ] Business-rule actions on the aggregate: move a payment to `Completed`, `Failed`, or `Expired`; guard against invalid transitions (throw rather than silently allow); domain events raised for completion and failure, mirroring the ticket events.
- [ ] `IPaymentRepository` registered on the unit-of-work object; repository supports add, delete, get by id, and predicate-based find.
- [ ] Persistence wiring: payment table registered, strong-id cross type conversion, status stored consistently with the rest of the schema.