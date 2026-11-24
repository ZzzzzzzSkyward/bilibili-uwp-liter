# In-progress implementation observations

These are coordination notes, not final review findings or permission to expand scope.

- During QF1 editing, `plan/tests/g3/G3Probe.csproj` temporarily contained three identical Compile includes for `Parsing/Dynamic/ReserveCardCopy.cs`. Ensure the final project includes the real source exactly once, with no duplicate-source warnings introduced by the repair. No orchestrator product/test edit was made.
- Search next-wave requirements propose changing shared `plan/tests/API01Stubs.cs` only after QF1 releases it. Preserve that single-writer boundary; report final shared-stub contract and release explicitly in quality execution evidence.
