# 05 — Group Infrastructure services under Services/

**What to build:** The Infrastructure layer is organized by concern: Email, Jobs, InMemory, and Payments live under a `Services` folder with matching namespaces, so the layer structure is navigable and infrastructure concerns are separated from persistence/plumbing.

**Blocked by:** 03 — both touch the Infrastructure dependency registration; keep the rename in first so the moved registrations already use `VerificationCode`.

**Status:** ready-for-agent

- [ ] Email, Jobs, InMemory, and Payments infrastructure move under a `Services` folder; namespaces become `...Infrastructure.Services.*`.
- [ ] All usings (dependency registration, Program wiring, any other references) are updated.
- [ ] The `Services/Payments` folder keeps its plural name to match the existing `StripePaymentsService` vocabulary.
- [ ] `dotnet build` and all tests pass.
