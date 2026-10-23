---
title: 'Test plan: Runnable app skeleton with lesson storage'
spec: 'spec-1-1-runnable-app-skeleton-with-lesson-storage.md'
created: '2026-10-22'
status: 'green'
---

# Test plan: Story 1.1, runnable app skeleton with lesson storage

The tests cover the main business only: lessons and their sentences are stored correctly, and the same content is never imported twice. They live in `tests/Dictation.Tests`; run them with `dotnet test Dictation.slnx`. Every persistence and API test uses its own temporary SQLite file.

## Automated tests

| Test | Covers |
|---|---|
| `LessonTests.AddSegment_assigns_sequential_zero_based_indexes` | `Index` is 0-based and assigned only by `Lesson` |
| `LessonStorageTests.Lesson_and_segments_round_trip_in_index_order` | Matrix: round trip (`EndMs = null`, UTC dates, index order) |
| `LessonStorageTests.The_same_lesson_cannot_be_imported_twice_from_a_source` | Matrix: duplicate lesson; the same `ExternalId` under another source is allowed |
| `LessonStorageTests.The_same_sentence_cannot_be_imported_twice_from_a_source` | Matrix: duplicate segment key |
| `LessonStorageTests.Sentences_without_a_key_never_conflict` | Matrix: null segment keys |
| `LessonStorageTests.Migrations_match_the_model` | The migration is in sync with the model |
| `AppStartupTests.First_start_creates_the_database_and_serves_the_home_page` | Matrix: first start; AC1 |
| `AppStartupTests.Unknown_api_route_returns_404_problem_details` | Matrix: unknown route |

## Manual checks

**M1: real host (AC1, restart)**
1. Delete `src/Dictation.Api/dictation.db` if present, then run `dotnet run --project src/Dictation.Api`.
2. The log shows `Now listening on: http://localhost:5080` only; `src/Dictation.Api/dictation.db` exists; `http://localhost:5080/` shows the placeholder page.
3. `curl -i http://localhost:5080/api/does-not-exist` returns `404` with `Content-Type: application/problem+json`.
4. Stop and start the app again. It starts with no migration errors.

**M2: git status (AC3)**
1. After M1, create `src/Dictation.Api/appsettings.Local.json` containing `{}`.
2. `git status --porcelain` shows no `*.db*` or `appsettings.Local.json` entry.

## Not automated

The unhandled-exception row (500 `ProblemDetails` without a stack trace) relies on the framework's `UseExceptionHandler`. It will be covered by an endpoint test once story 1.3 adds real endpoints.
