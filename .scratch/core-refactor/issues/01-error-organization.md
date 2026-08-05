# 01 — Error organization + new error types

**What to build:** Each application error type lives in its own file under a dedicated errors folder, and two new error types exist for payment/webhook failure reporting. Finding and extending an error type no longer requires opening a single monolithic file.

**Blocked by:** None — can start immediately.

**Status:** ready-for-agent

- [ ] Every sealed error record is extracted from the shared error file into its own file under the Application errors folder; the abstract base remains single-sourced.
- [ ] Namespaces keep the existing Application.Abstractions scope so existing usings remain valid.
- [ ] New `PaymentProcessingError` (code `PaymentProcessing`) and `UnsupportedCurrencyError` (code `UnsupportedCurrency`) exist and are usable.
- [ ] `dotnet build` and existing tests pass with no behavior change.
