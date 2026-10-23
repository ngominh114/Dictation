---
title: 'Runnable app skeleton with lesson storage'
type: 'feature'
created: '2026-10-22'
status: 'done'
route: 'dispatch'
baseline_commit: 'fbd895260501b0dc772db942a857768843cbb7cf'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
  - '{project-root}/_bmad-output/architecture-dictation-practice/architecture-dictation-practice.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** There is no code yet. Every later story needs the agreed solution layout, the `Lesson`/`Segment` storage and a host that starts cleanly.

**Approach:** Create the 4-project .NET 10 solution with the Ports & Adapters dependency rule. Add the `Lesson`/`Segment` domain model, persisted in SQLite via EF Core with the first migration, which is applied on startup. Add a localhost-only Minimal API host that serves a placeholder page and returns `ProblemDetails` for every error, plus a test project proving the setup.

## Boundaries & Constraints

**Always:**
- **Projects and packages:** `src/Dictation.Core`, `src/Dictation.Infrastructure`, `src/Dictation.Api` and `tests/Dictation.Tests`, all `net10.0`. Versions are pinned: EF Core SQLite + Design 10.0.13, Mvc.Testing 10.0.13, xunit.v3 4.0.2. If xunit.v3 4.0.2 does not work with Mvc.Testing, pin 3.2.2 and record why in Implementation Notes.
- **Dependency direction:** `Core` references no project and only `Microsoft.Extensions.*.Abstractions` packages. `Infrastructure` references only `Core`. `Api` references both.
- **Model:** `Lesson { Guid Id, string Source, string ExternalId, string Title, string Language, string SourceUrl, string? SourceAudioUrl, LessonStatus Status, string? TranscriptionProvider, string? TranscriptionJobId, DateTime? TranscriptionStartedAt, DateTime CreatedAt, Segments }`. `Segment { int Index, string? ExternalKey, string Text, string AudioUrl, int StartMs, int? EndMs, Attribution? }`. `Attribution { Author, License, Url }`. `LessonStatus { Discovered, Transcribing, Ready, Failed }`. The Epic 2 columns ship now so later stories need no schema rework.
- **Persistence:**
  - Segments are an EF owned collection keyed `(LessonId, Index)`, and `Attribution` is an owned type stored as 3 columns.
  - Enums are stored by name. Dates are UTC `DateTime`, never `DateTimeOffset`.
  - Unique indexes on `Lessons(Source, ExternalId)` and on `Segments(Source, ExternalKey)` where `ExternalKey` is not null. `Segment.Source` is a column copied from its lesson.
- **Startup:** `MigrateAsync()` runs in `Program.cs` before `app.Run()`, then SQLite is switched to WAL mode. The connection string is `ConnectionStrings:Dictation` (default `Data Source=dictation.db`).
- **Host:**
  - Kestrel listens on `http://localhost:5080` only.
  - `AddProblemDetails()`, `UseExceptionHandler()` and `UseStatusCodePages()` mean every error is `application/problem+json`, with exception detail only in Development.
  - Config also loads an optional `appsettings.Local.json`, which is already gitignored.
  - Static files are served from `wwwroot`, with a placeholder `index.html`.

**Never:** No import, adapters, endpoints beyond the placeholder page, or lifecycle methods. Those belong to stories 1.2 and 1.3. Never use `EnsureCreated`. Never add MediatR or CQRS. No API keys in committed files.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| First start | No database file | Tables created by the migration; `GET /` serves `index.html` | N/A |
| Restart | Database already migrated | Starts normally; no schema change; existing rows kept | N/A |
| Unknown route | `GET /api/does-not-exist` | 404 with `application/problem+json` | N/A |
| Unhandled exception | An endpoint throws, environment not Development | 500 `ProblemDetails`, no stack trace in the body | N/A |
| Duplicate lesson | Two lessons with the same `(Source, ExternalId)` | The second save fails on the unique index | DB update exception |
| Duplicate segment key | Two segments with the same `(Source, ExternalKey)` | Save fails on the unique index | DB update exception |
| Null segment keys | Several segments with `ExternalKey = null` | Saved without conflict | N/A |
| Round trip | Lesson with 2 segments, one with attribution and `EndMs = null` | Reloaded with the same values, segments in index order | N/A |

</frozen-after-approval>

## Code Map

- Greenfield: no source code exists yet. Repo root has `.gitignore` (already ignores `bin/`, `obj/`, `*.db*`, `appsettings.Local.json`) and `_bmad*/` folders, which must not be touched.
- `_bmad-output/architecture-dictation-practice/architecture-dictation-practice.md` -- source of AD-1 (dependencies), AD-3/AD-4 (model), AD-7 (startup), the ER diagram and the source tree.

## Tasks & Acceptance

**Execution:**
- [x] `Dictation.slnx`, `src/*/*.csproj`, `tests/Dictation.Tests/Dictation.Tests.csproj` -- create the solution and the 4 projects with references and pinned packages -- layout from the architecture source tree.
- [x] `src/Dictation.Core/Lessons/` -- `Lesson`, `Segment`, `Attribution`, `LessonStatus` (plain domain types, private setters, no EF attributes) -- AD-3/AD-4 shape.
- [x] `src/Dictation.Infrastructure/Persistence/` -- `DictationDbContext`, entity configuration, initial migration, `AddPersistence(IServiceCollection, IConfiguration)` extension -- owned types, indexes, enum-by-name.
- [x] `src/Dictation.Api/Program.cs`, `appsettings.json`, `Properties/launchSettings.json`, `wwwroot/index.html` -- host, migrate + WAL on start, ProblemDetails, localhost URL, `appsettings.Local.json` loading -- AD-7 and conventions.
- [x] `tests/Dictation.Tests/` -- a small business-focused suite: segment indexing, storage round trip and duplicate rules, migrations in sync, and host start plus 404 via `WebApplicationFactory`, each on a temporary SQLite file -- AD-12.

**Acceptance Criteria:**
- Given a fresh clone with the .NET 10 SDK, when `dotnet run --project src/Dictation.Api` runs, then the app listens on `http://localhost:5080`, creates `dictation.db` and serves `index.html` at `/`.
- Given the solution, when it builds, then `Core` references no project, `Infrastructure` references only `Core`, and `Api` references both (enforced by the project references).
- Given the repository after this story, when `git status` runs after a local start, then no database file or `appsettings.Local.json` is shown as untracked.

## Implementation Notes

- **TDD run:** the test agent wrote 54 tests, of which 30 were red. The implementation agent never saw them and turned all 54 green. The test-file fingerprint was unchanged.
- **Test runner:** xunit.v3 4.0.2 works with Mvc.Testing 10.0.13. The test project uses `xunit.v3.mtp-off` with `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio`, so plain `dotnet test` works without a `global.json`.
- **Construction hooks:** `Lesson` has an internal constructor and `AddSegment()`; `AddSegment` assigns the 0-based index and copies `Source` onto the segment. Both are exposed to Infrastructure and Tests through `InternalsVisibleTo`. Story 1.2's `CreateReady` must build on them.
- **Error handling:** `UseExceptionHandler()` runs outside Development; in Development the built-in developer exception page reports exception detail. Any future SPA fallback must exclude `/api/*`.
- **Migrations:** `DesignTimeDictationDbContextFactory` (internal) exists only so `dotnet ef` can create migrations. Install the tool with `dotnet tool install -g dotnet-ef`.
- **Attribution removed (user direction, personal use only):** `Attribution` and its 3 columns were removed from `Segment`, the EF mapping and the `InitialCreate` migration (edited in place; no database is deployed, so delete any local `dictation.db`). The Tatoeba licence filter stays.
- **Domain/Application split (user direction):** `Dictation.Core` became `Dictation.Domain` (`Lesson`, `Segment`, `LessonStatus`). A new `Dictation.Application` project sits between Domain and Infrastructure; it is empty until story 1.2 adds the import use case and its ports. Dependencies: Api → Application + Infrastructure, Infrastructure → Application → Domain.
- **Simplification (user direction):** the suite was cut from 66 to 8 tests that cover the main business. Schema-shape, architecture, repository-hygiene and host-configuration tests were removed. The review patches for triage #3 and #17 were reverted to plain defaults:
  - `appsettings.Local.json` is added with `AddJsonFile`, so it overrides environment variables. That is acceptable for a single-user local app.
  - The SQLite `Data Source` is relative to the working directory; `dotnet run` uses the project directory, so the database lands in `src/Dictation.Api/dictation.db` (gitignored).

## Spec Change Log

## Review Triage Log

| # | Layer | Finding | Verdict | Evidence / route |
|---|-------|---------|---------|------------------|
| 1 | blind | `.gitignore` missing from diff | false | Committed in the baseline: `*.db`, `*.db-shm`, `*.db-wal` and `appsettings.Local.json` were already present. |
| 2 | blind, edge | Hygiene test fails without git or `.git` | low | Real, but the repo is always a git checkout here; the fix adds skip logic. Rejected. |
| 3 | blind, edge | Relative `Data Source=dictation.db` resolves against the working directory, not the content root | medium | Running from the repo root and from `src/Dictation.Api` creates two databases. Patch: resolve relative paths against `ContentRootPath`. |
| 4 | blind, edge | Architecture rule allows any third-party assembly in Core | medium | `IsAllowedCoreReference` returns true for names outside `Dictation.`/`Microsoft.`, contradicting AD-1. Patch (test): whitelist `System.*`, `netstandard`, `mscorlib` and the Abstractions. |
| 5 | verif, blind | UTC `Kind` is never asserted; `DateTime` equality ignores `Kind` | medium | Removing the converter or `ToUtc` would keep every test green. Patch (test): assert `Kind` after a round trip, plus a theory over Local/Unspecified input. |
| 6 | blind | UTC conversion written 3 times | low | Drift risk only; the fix adds a helper. Rejected. |
| 7 | blind, edge | No domain guards (negative `startMs`, `endMs <= startMs`, blank strings, undefined enum) | low | No caller can produce these in 1.1, and the guards add branches. Rejected; revisit when 1.2 adds the factories. |
| 8 | blind | Connection-string fallback in `AddPersistence` is muddled | low | Works; the tests confirm the override. Rejected. |
| 9 | blind | `Import:Enabled=false` is dead config | false | Intentional per AD-12, so the factory is ready for 1.2; harmless. |
| 10 | blind | Restart test reloads by `ExternalId`; `lessonId` unused | low | Weak assertion; direct correction. Patch (test). |
| 11 | blind | API-key test only matches `"ApiKey"` | low | Matches the only key shape the architecture defines (`Transcription:{Provider}:ApiKey`). Rejected. |
| 12 | blind | VSTest packages referenced alongside the MTP runner | low | `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio` are unused under MTP; deleting them is a direct correction. Patch (test). |
| 13 | blind | `Segments` allocates on every read | low | Negligible at 10 segments. Rejected. |
| 14 | blind, edge | No logging around startup migration and WAL | low | The host already logs unhandled startup exceptions. Rejected. |
| 15 | blind | URL set in both `launchSettings.json` and `appsettings.json` | low | The spec asks for both; consistent values. Rejected. |
| 16 | blind | `InternalsVisibleTo(Infrastructure)` unnecessary | false | `LessonConfiguration` maps the internal `Segment.Source`, so it needs the grant. |
| 17 | verif, edge | `appsettings.Local.json` is added after env vars and command line, so it overrides them (and could override the test connection string) | medium | Breaks the usual precedence and puts test isolation at risk. Patch: env vars and command line must still win. |
| 18 | edge | Non-JSON `Accept` header gets a non-problem+json error | low | The app's own JS sends `*/*`; browser-only edge. Rejected. |
| 19 | edge | Config-order test passes trivially if the `appsettings.json` source is missing (`FindIndex = -1`) | low | Direct correction. Patch (test). |
| 20 | edge | Temp-file cleanup can throw `UnauthorizedAccessException` | low | Rare on a dev box; the fix adds a catch. Rejected. |
| 21 | edge | Architecture test misses unused csproj references | low | An unused reference creates no code dependency. Rejected. |

## Design Notes

- **`Segment.Source`** is a denormalized copy of the lesson's `Source`. An index cannot span the lessons and segments tables, and AD-6 needs uniqueness per source. `Lesson` sets it when segments are added; it never comes from callers.
- **Database for tests:** use a unique temporary file per test, not in-memory SQLite. Migrations and WAL then run exactly as in production.

## Verification

**Commands:**
- `dotnet build Dictation.slnx` -- expected: 0 warnings treated as errors, success.
- `dotnet test Dictation.slnx` -- expected: all tests pass, none skipped.
- `dotnet run --project src/Dictation.Api` then `curl -i http://localhost:5080/api/does-not-exist` -- expected: `404` with `Content-Type: application/problem+json`.
