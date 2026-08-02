# 05 — Expiry and in-flight reservation guard jobs

**What to build:** Background maintenance that keeps abandoned payments from lingering and stops the existing reservation cleanup from invalidating a tracked in-flight payment. A new cleanup job marks stale `Pending` payments as `Expired` and releases their ticket reservations, mirroring the existing reservation cleanup job's scheduling. Meanwhile the existing reservation-expiry job is updated to skip any reserved ticket that holds an active pending payment, so a checkout in progress is never torn down underneath the buyer.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] New cleanup job marks `Pending` payments older than the configured threshold as `Expired` and releases the corresponding ticket reservation.
- [ ] New cleanup job is scheduled consistent with the existing reservation cleanup job's scheduling.
- [ ] Existing reservation-expiry job skips tickets that have an active pending payment, so in-flight checkouts are not released by the reservation timer.
- [ ] Both jobs stay safe under concurrent runs.