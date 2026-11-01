---
name: mutation-testing
description: Use when running Stryker.NET mutation tests, comparing mutation scores before and after a change, or deciding what to do about surviving mutants in the approval code.
---

# Mutation testing with Stryker.NET

## Install (version matters)

```bash
dotnet tool install -g dotnet-stryker --version 4.13.0
```

The newest release targets a later .NET runtime and fails to install on .NET 8.

## Run

```bash
cd backend
dotnet stryker -f stryker-config.json      # HTML + JSON report in backend/StrykerOutput/<date>/reports
```

`stryker-config.json` mutates only `ApprovalService.cs` and `ApprovalLimits.cs`, so the score is about the approval rules, not the untested discount calculator.

## Before and after

```bash
bash scripts/mutation-before-after.sh
```

It checks out the baseline commit (`main`) into a temporary worktree, runs Stryker there, runs it again on `HEAD`, and writes both scores to `docs/mutation/scores.md`. Paste that file into `ASSIGNMENT.md`.

## Surviving mutants

For each survivor, decide one of:

1. **Kill it**: write the missing test (most often a boundary `<` vs `<=`, or a removed assignment no test asserts on).
2. **Equivalent mutant**: the mutation cannot change behaviour (for example a string in an exception message no test checks). Note it in the PR with the reason.

Never raise the score by asserting on implementation details that the spec does not care about.
