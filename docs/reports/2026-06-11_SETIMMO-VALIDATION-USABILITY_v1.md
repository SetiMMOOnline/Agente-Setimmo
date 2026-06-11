# Agente Setimmo Validation And Usability Report

Date: 2026-06-11
Executor: Codex
Workspace: `E:\Ragnarok\Projeto\Agente_Setimmo`

## Summary

This validation focused on making the standalone Agente Setimmo more reliable and less blocked by false negatives, without weakening the core safety model. The main fixes were:

- restore parity between source and `dist`
- allow safe scaffold creation through `plan/dry-run implement` when the target file does not exist and the instruction is clearly a creation request
- align `health` with the real rollback capability already implemented
- strengthen `scripts/release-check.ps1` so the packaged executable proves the same behavior as the source build

## Objective

Validate the standalone Agente Setimmo, reduce unnecessary operational friction, and keep the agent safe, traceable, and functional for real creation flows inside authorized writable roots.

## Scope

- Read allowed: repository files under `E:\Ragnarok\Projeto\Agente_Setimmo`, local config, local logs, local cache, local packaged executable behavior
- Write allowed: repository source/tests/scripts/docs, local cache refresh, local `dist`, local temp smoke files created and rolled back by the agent
- Forbidden: destructive edits outside repo scope, GRF writes, `.lub` edits, rAthena/Patch real writes, production apply

## Files Read

- `AGENTS.md`
- `README.md`
- `config\paths.json`
- `config\safety.json`
- `src\RagnaForge.Agent.Core\Implementation\SemanticPatchPlanner.cs`
- `src\RagnaForge.Agent.Core\Implementation\ImplementationWorkflowService.cs`
- `src\RagnaForge.Agent.Core\Commands\HealthCommand.cs`
- `src\RagnaForge.Agent.Cli\Program.cs`
- `tests\RagnaForge.Agent.Core.Tests\ImplementationWorkflowTests.cs`
- `tests\RagnaForge.Agent.Core.Tests\OperationalUxTests.cs`
- `scripts\install.ps1`
- `scripts\release-check.ps1`
- `scripts\package-clean.ps1`
- `docs\EXTERNAL_DATA_TRIAGE.md`

## Files Changed

- `src\RagnaForge.Agent.Core\Implementation\SemanticPatchPlanner.cs`
- `src\RagnaForge.Agent.Core\Commands\HealthCommand.cs`
- `tests\RagnaForge.Agent.Core.Tests\ImplementationWorkflowTests.cs`
- `tests\RagnaForge.Agent.Core.Tests\OperationalUxTests.cs`
- `scripts\release-check.ps1`
- runtime artifacts refreshed:
  - `dist\agente-setimmo\agente-setimmo.exe`
  - `dist\agente-setimmo\ragnaforge.exe`
  - `cache\agent\project_index.json`
  - `cache\agent\entities_index.json`
  - `logs\operations\*.json`
  - `logs\diffs\*.diff.json`
  - `logs\rollbacks\*.rollback.json`

## Files Created

- `docs\reports\2026-06-11_SETIMMO-VALIDATION-USABILITY_v1.md`

## Files Removed

- none

## Key Findings

1. The packaged executable in `dist\agente-setimmo` was stale relative to the source code. This produced false `doctor` failures and did not expose the current `export api-readiness` command.
2. `dry-run implement` could not create a new file from a clearly safe creation instruction, even though the README documents that flow. The operation was incorrectly blocked as `non_semantic_patch`.
3. `health` still reported rollback as effectively blocked even after successful concrete `apply/rollback` implementation tests.
4. The standalone already uses `operationProfile = local-dev`, so the right fix was not to weaken policy gates globally, but to remove false negatives and stale packaging.

## Changes Applied

1. Added instruction-aware scaffold creation for missing targets in `SemanticPatchPlanner`.
2. Added regression tests covering:
   - safe creation from `dry-run implement`
   - apply and rollback of that generated operation
   - continued rejection of non-creation instructions on missing targets
3. Updated `HealthCommand` so rollback capability is reported consistently with the actual implementation.
4. Expanded `scripts\release-check.ps1` to verify:
   - `export api-readiness`
   - `field test run`
   - `dry-run implement` create smoke
   - `apply implement`
   - `rollback`
   - current direct `apply --json` contract
5. Republished the standalone executable with `scripts\install.ps1`.

## Commands Executed

- `git -C "E:\Ragnarok\Projeto\Agente_Setimmo" status --short --branch`
- `git -C "E:\Ragnarok\Projeto\Agente_Setimmo" branch --show-current`
- `git -C "E:\Ragnarok\Projeto\Agente_Setimmo" rev-parse HEAD`
- `dist\agente-setimmo\ragnaforge.exe status --json`
- `dist\agente-setimmo\ragnaforge.exe doctor --json`
- `dist\agente-setimmo\ragnaforge.exe health --json`
- `dist\agente-setimmo\ragnaforge.exe validate --json`
- `dotnet build RagnaForge.Agent.slnx`
- `dotnet test RagnaForge.Agent.slnx --no-build`
- `dotnet ...RagnaForge.Agent.Cli.dll doctor --json`
- `dotnet ...RagnaForge.Agent.Cli.dll index --entities --json`
- `dotnet ...RagnaForge.Agent.Cli.dll validate --json`
- `dotnet ...RagnaForge.Agent.Cli.dll export api-readiness --json`
- `dotnet ...RagnaForge.Agent.Cli.dll field test run --json`
- `dotnet ...RagnaForge.Agent.Cli.dll scan --project --json`
- `dotnet ...RagnaForge.Agent.Cli.dll health --json`
- `dotnet ...RagnaForge.Agent.Cli.dll dry-run implement --target temp/validation-smoke.ps1 --workspace agent --language powershell --instruction "..."`
- `dotnet ...RagnaForge.Agent.Cli.dll apply implement --operation <id> --confirm --json`
- `dotnet ...RagnaForge.Agent.Cli.dll rollback --id <id> --confirm --json`
- `powershell -ExecutionPolicy Bypass -File .\scripts\install.ps1 -AgentRoot "E:\Ragnarok\Projeto\Agente_Setimmo"`
- `powershell -ExecutionPolicy Bypass -File .\scripts\release-check.ps1`

Note: the `git` commands failed because `git.exe` was not available in the current shell PATH.

## Validation Results

- Build: passed
- Targeted implementation workflow tests: passed
- Full test suite: 298 passed, 0 failed
- Entity index refresh: passed
- Project scan refresh: passed
- API readiness export: passed
- Field test harness: passed, 6/6 scenarios
- Source CLI smoke (`dry-run -> apply -> rollback` on a new file): passed
- Packaged executable release check: passed
- Packaged executable health: passed

## Operational State After Fix

- `doctor`: OK
- `health`: OK
- project cache: trusted
- entity cache: trusted
- creation flow in `workspace agent`: functional
- rollback capability: reported consistently
- direct global `apply --json`: still blocked as intended
- GRF and `.lub` protections: still active
- production apply: still disabled

## Remaining Risks

1. `validate --json` still reports 1083 external-data warnings, currently non-blocking for read-only and dry-run, but noisy for human operators.
2. `git.exe` was not available in the current shell, so real `git status` and `git diff` evidence could not be collected from the CLI.
3. The release check is now stronger, but it is still process-dependent: teams must keep using it before distributing a new `dist`.

## Recommended Next Step

Add a compact validation output mode or grouped warning summary for repeated external-data warnings, so operators can see real blockers faster without hiding the full evidence.
