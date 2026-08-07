Status: ready-for-agent
Type: task

# Spec: JSON serialization of Money and DateTimeRange in persistence

## Problem Statement

Money and DateTimeRange value objects are stored as pipe-delimited strings
(`150|PLN`, `2027-01-15T19:00:00.0000000Z|2027-01-15T23:00:00.0000000Z`) by the
ApplicationDbContext value converters. This ad-hoc format is opaque, fragile, and
cannot be queried or filtered without parsing in application code.

## Solution

Serialize Money and DateTimeRange as JSON using System.Text.Json when storing
them through the EF Core value converters. Storage remains a text column via the
existing `ValueConverter<Money, string>` / `ValueConverter<DateTimeRange, string>`,
so the InMemory provider keeps working unchanged.

## User Stories

1. As a developer, I want Money persisted as a JSON object so that its structure
   is self-describing and readable in the database.
2. As a developer, I want DateTimeRange persisted as a JSON object so that its
   start and end timestamps are stored as one structured value.
3. As a developer, I want serialization to use camelCase property names so that
   stored values follow ASP.NET Core conventions and read back case-insensitively.
4. As a developer, I want deserialization to accept only the new JSON format so
   that legacy pipe-delimited data cannot silently mask format bugs.
5. As a developer, I want the serializer to be System.Text.Json so that no new
   dependency is introduced.
6. As a developer, I want round-trip persistence through the EF converters to be
   lossless so that saved Money and DateTimeRange values load back equal.
7. As a system, I want Money and DateTimeRange to keep their domain invariants on
   load (e.g. DateTimeRange requires EndTime >= StartTime) so that persisted data
   stays valid.
8. As a future feature owner, I want the stored JSON addressable by SQL JSON
   functions (JSON_VALUE / JSON_QUERY) so that filtering by amount, currency, or
   time window becomes possible without loading rows.

## Implementation Decisions

- Keep `ValueConverter<Money, string>` and `ValueConverter<DateTimeRange, string>`;
  only the string format changes (pipe-delimited → JSON). Storage stays a text
  column, compatible with the InMemory provider.
- Replace the `ParseMoney` / `ParseDateTimeRange` bodies with
  `JsonSerializer.Deserialize<T>`; the serialization side is inline
  `JsonSerializer.Serialize`. Method names are retained.
- A single shared `JsonSerializerOptions` configured with
  `JsonSerializerDefaults.Web` (camelCase property naming, case-insensitive reads)
  is used by both converters.
- DateTime values use System.Text.Json's default ISO-8601 round-trip serialization.
- Clean switch: no legacy pipe-format fallback in deserialization.
- Money and DateTimeRange value objects are unchanged — no serialization attributes,
  no source generators.
- No migrations involved: the database is InMemory with no persisted legacy data.

## Testing Decisions

- Good tests assert external behavior — a value saved through the context reads
  back equal — not converter internals.
- Single seam: ApplicationDbContext round-trip persistence over the InMemory
  provider. This is the same seam used by every existing handler test and exercises
  the real converter code path.
- New test file in the test project: save a Payment carrying Money and a
  SocialEvent carrying DateTimeRange, read both back, and assert value equality.
- Prior art: existing tests construct ApplicationDbContext over
  UseInMemoryDatabase (e.g. ExpiredReservationsCleanupJobTests).

## Out of Scope

- Native JSON columns (SQL Server json type) and JSON SQL function querying —
  deferred until the app moves off the InMemory provider.
- Legacy pipe-format data migration or fallback parsing.
- Changes to Money, DateTimeRange, DTOs, or query handlers.
- Changing the database provider.

## Further Notes

- Project targets net10.0; System.Text.Json is in-box, no package needed.
- Because the InMemory provider is used, converters apply on save/materialize
  with no migration tooling involved.
