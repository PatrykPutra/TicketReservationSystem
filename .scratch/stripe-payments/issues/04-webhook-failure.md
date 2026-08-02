# 04 — Webhook failure and expiry

**What to build:** The failing side of the payment lifecycle, driven by the webhook. When a Stripe payment failure or expiry event arrives, the handler marks the payment `Failed` or `Expired` and releases the ticket's reservation so the ticket becomes `Available` again for others. Already-terminated payments are not touched, so the failure branch is idempotent. This completes the full set of webhook event types the endpoint is able to handle.

**Blocked by:** 03

**Status:** ready-for-agent

- [ ] Webhook failure/expiry event types mark the payment `Failed` or `Expired` (matching the event's semantics) and release the ticket reservation.
- [ ] Re-delivered or already-terminal payments are no-ops; releasing an already Available ticket is safe and non-destructive.
- [ ] The webhook handler's branch set is complete and consistent with the completion branch from the prior ticket.