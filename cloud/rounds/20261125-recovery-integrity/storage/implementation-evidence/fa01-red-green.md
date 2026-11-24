# FA-01 fix execution and exact evidence

Status: **LOCAL PASS; final source/test bytes frozen and released for re-verification**. Root's fix-only grant is review-fix-dispatch.md; no index/ref/publication grant. Local dev/HEAD remains `e928bf0e818869c03485f8ef6f31fa47e98ac0f0`. No fetch/pull/auth retry, staging, commit, push or ref mutation was performed. Other recovery/root inputs were preserved. This amendment supplements the original published S1–S4 receipt; it does not rewrite its historical PASS or close independent FA-01 review by assertion.

## Exact bounded repair

Only two product/test paths changed: `src/BiliLite.UWP/Services/LocalMedia/LocalMediaLibraryService.cs` (three added lines), `plan/tests/LM02.cs` (104 additive lines). The guarded registry read now rejects a null entry or null/empty/whitespace Token, CollectionId or RootPath before returning the snapshot. Existing try/catch produces typed `registry-read`/`unknown`. No normalization, filtering, repair, schema, duplicate rule, FAL/asset lookup or display-metadata requirement was added. Existing null defaults and all gate/admission/acquisition/rollback paths remain intact.

Shared primitive stubs `plan/tests/LocalMediaStorageStubs.cs` are **unchanged**. Existing create/write/replace/remove hooks and exact grant identity/count prove side effects: any new live provisional grant changes Count; any added-then-removed token reaches the removal hook. Therefore no new primitive hook or grant algorithm was needed. History, playersess, page/XAML/project/runner/inventory/source outside this exact grant remain untouched.

## Identical-fixture genuine red → green

Scratch root: `/workspace/work/recovery-integrity/storage-review-fix/`. Toolchain `/workspace/work/maintenance-foundation/quality`: SDK8.0.427, runtime/ref8.0.33. Before adding tests the executor saved current service/LM02/stubs in `baseline/`. Final new LM02 bytes were copied to `LM02-final-fixture.cs` and hashed before changing product source. A reporting-count correction preceded the final red receipt; final red and green fixture bytes are byte-identical.

Command:

```bash
UPSTREAM_UNIT_TOOLS=/workspace/work/maintenance-foundation/quality bash plan/tests/run.sh --stage LM02
```

- Final unchanged-service red `lm02-red.log`: **exit134/nonzero**,439 passing plus214 failing additive predicates. All14 malformed shapes × before/after valid row × list/reopen/import = **84 invalid calls** execute, followed by compatibility controls. The three-line product fix had not yet been applied. Existing S1–S4/original checks pass first.
- Fixed-service green `lm02-green.log`: **exit0**, **653/653 additive check lines**:50 previously accepted S1–S4 checks +603 FA-01 predicates; original LM02 Assert statements also pass. No assertion body was removed, weakened or changed.
- 214 reds are84 whole-snapshot typed-boundary predicates plus130 no-side-effect/retry predicates on previously accepted partial identities. Bad data before and after a valid entry includes null,{}, and all three required fields independently missing/null/empty/whitespace. Valid-token reopen with malformed trailing data must reject the full snapshot.
- Each invalid operation checks exact committed JSON, existing temporary bytes, original grant identity/count and zero create/write/replace/remove activity. Then only known valid bytes are restored using the **same static service and semaphore**, list and old-token reopen finish within10 seconds, a distinct import succeeds, exactly the old/new reachable grants remain and the new token reopens. No fixture resets a service or semaphore to manufacture recovery.
- Compatibility controls cover absent/null-root/missing-null-empty Entries; non-GUID token and omitted display metadata; unknown/stale input stages; identity-complete broken asset retained/listed, typed asset-resolution failure, repair/reopen without registry rewrite; blank public-token argument precondition. Existing malformed JSON, sequential replacement, staged faults, parallel import/list/reopen and all original assertions remain.

Every initiated service operation is awaited sequentially/bounded; no event gates, observers or background fixture tasks were added. Existing LM02 finally resets hooks/grants and deletes the temporary tree. No sleeps, network or retry polling. Shared stubs did not change, so LM03/LM05/playersess and unchanged runner controls were not redundantly rerun; they remain required in root's next common51 logical/52 executable gate.

## Actual page-refresh composition

`PageProof.csproj` compiles the **whole actual service/model/loader/validator closure** with unchanged primitive stubs and Newtonsoft13.0.3. `PageRefresh.cs` embeds the complete unique actual `RefreshSavedCollectionsAsync` method verbatim; wrapper fields/log/toast counters only observe calls. Method bytes are uniquely bound to the actual current DownloadPage, SHA256 `49cbc84da98359c46d233d396e71bf8a079180642bf5540e1a324bd81c5d34f2`. No page source edit or permanent runner/stage was added.

Executed twice with the exact portable dotnet, `DOTNET_ROOT` pointed at its dotnet directory, `DOTNET_CLI_HOME` at scratch `cli`, `NUGET_PACKAGES=/workspace/work/maintenance-foundation/quality/cache/nuget`, and telemetry/no-logo enabled:

```bash
/workspace/work/maintenance-foundation/quality/dotnet/dotnet run --project /workspace/work/recovery-integrity/storage-review-fix/PageProof.csproj -c Debug
```

- `page-red.log`: **exit1**, exact refresh faults NullReferenceException, logs0/toasts0.
- `page-green.log`: **exit0**, exact refresh Task completes, exactly one typed registry-read/unknown log and one toast, the seeded visible row retains object identity, and original malformed bytes/prior grant identity/count remain intact. This is actual source-call composition, with no rendered UI/native toast claim. Fixture cleanup runs in finally.

## Immutable bindings and preservation

`preservation.log` proves complete prior LM02 bytes are restored exactly after removing only one using, one call and the new method; red/green final LM02 bytes are identical; shared stubs equal baseline/HEAD; exact page method occurs uniquely and unchanged in source/wrapper. `final-inputs/` holds immutable copies of12 full repository inputs; `final-inputs.sha256` verifies all12. `artifacts.sha256` binds the final fixture, wrapper/project, all four logs and receipts. `git diff --check` for both touched paths passed; index remains empty.

| Input | SHA256 |
| --- | --- |
| Pre-fix service | `335d35363f65fa03e57fc94193ce9e2c08aafd58cea83a40e24a0833e19a2dd0` |
| Final service | `28f03d444c40b79da8fd93f034f00ef6be5f2265355451799d7807bdf6aff514` |
| Final/red LM02 fixture | `d3b32fe7830d712f5cad403bdea20946eaedca976b80debc5c1bb36c91c1cae3` |
| Unchanged shared stubs | `a1c38806369b348a089032cc559b165cd941b4fb46a8c242b58024456e2c9caf` |
| Actual DownloadPage | `c80f7267f7db8000c6f97f3dab1bfb37f631291c463a791a9f78db9e2423e56f` |
| Final input receipt | `9fe8f980eb582f9f0100a4bcf2683aab77a4349b1d9ba94fa613f5171449c90b` |
| LM02 red log | `52c337f6465c271d33204eb65916ff760014d90f160ba88adf9ed94b714f68d1` |
| LM02 green log | `471bf166757ee567118d13ad88dc967712a15f8d0eee9330c06abfc788211801` |
| Page red log | `e44f9e08e674cc909fbf3c084a4b65b4b06aa87db6ad1d4b736baa34a98c720e` |
| Page green log | `eb5def162f428a87c294dae3d1e5ee3382a9a22d46e36c60eb04472b2493ba60` |

No source mutation will follow this local completion without a further grant. Root now owns common verification/independent FA-01 re-review/fresh audit and later supported publication. Existing old published source SHA bdba9eb and receipt e928bf0 remain provenance, not a commit containing these unstaged amendment bytes. Native UWP compile/rendering, FAL/file replacement/restart semantics and hosted CI remain separate unverified prerequisites; local PASS makes no new native guarantee.
