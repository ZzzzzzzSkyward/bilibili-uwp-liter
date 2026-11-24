# QF2 — stage-aware offline runner and minimal CI

Status: **READY FOR FULL RE-REVIEW**. P2; the released comments integration is resolved in §3a. The complete requirements, exact current manifest and ownership return for independent PASS before runner/workflow implementation.
Requirements owner: `logic_requirements`. Inspected on 2026-11-24 at product/source HEAD `10dd848`; QF1 was initially in flight and has since recorded harness commit `ca991490` and final evidence HEAD `858b97105ac8b46492af3810e5352afd9efbe8d5`. These states are distinct; the later executor must record its actual starting/final SHA.
Integration source: E1 source/test commit `1c7f6e4aacf3c0fa4bb7f7d70851308405b995cd`, release metadata `5210148fb8b3e63b32fb9f1f843ff8b5a063f1a6`; current read HEAD `01c503f0b878e4949ec0aa61ef441ee2950fea03` contains identical bound comments inputs. Inputs: repository/cloud instructions and memory index/run; this round's `plan.md`; foundation verification requirements/review/amendment, baseline, QF1 execution/results/ownership release; current scripts/projects; package-layout round plan; official GitHub Actions documentation. Evidence: [evidence.md](evidence.md).
Revision response to independent `review.md`: **P1-01** resolves the released one-program comments contract, exact input hashes, 50/51 current manifest and read-only ownership (§3a); **P1-02** defines the active earlier-deadline/signal outcomes and meaningful suite-budget/SIGINT/SIGTERM controls (§5/§7). Independent full re-review remains required.
No product, repository test/runner, workflow, main documentation or git history was changed for this requirements task. All experiments/read-only evidence are under `/workspace/work/maintenance-foundation/verification-runner/`.

## 1. Problem, goal and scope

The legacy `plan/tests/run.sh` compiles/runs LM01→LM05 then invokes API01 and AUTH01B under `set -e`; AUTH01B similarly runs Account-cookie before LoginVM. A failed prefix masks suffixes. Foundation QF1 restored the behavior/harness: its immutable snapshot records **50/50 logical entries and 51/51 executable stages PASS**, but its scratch sweep is not a permanent repository interface or a GitHub run.
Provide one local/CI command that executes every existing required stage sequentially, continues after a stage failure, preserves denominators and logs, and returns a truthful failure. Add granular selection to existing scripts without changing their default fail-fast behavior, compiled product bodies, extraction boundaries, platform stubs or assertions. Add a single read-only offline GitHub Actions workflow. This does not build or run UWP.
Non-goals: QF1 fixture/behavior redesign; new framework/dependency/SDK migration; changing product code/protos/manifests; package build/release/install; account/network writes; Windows runner provisioning; parallel test execution; changing branch protection; nightly scheduling; silently adding package-layout to the legacy denominator.

## 2. Exact entry point and command interface

New entry point: **`bash plan/tests/run-all.sh`**, a small Bash launcher for standard-library Python **`plan/tests/run-all.py`**. It derives the repository root from its own path, not the caller's cwd; process argv is an array, not `eval`/shell interpolation. Bash ≥4, Python ≥3.9 and standard Linux tools used by the existing scripts are prerequisites. Missing launcher prerequisites exit 2 with a precise diagnostic; do not pretend a JSON suite report was produced if Python cannot start.

| Flag | Exact behavior |
| --- | --- |
| No selection flags | Run all **51** required executable stages in the approved current manifest, sequentially (§3/§3a). Comments is one integrated physical executable, not a hidden second stage. |
| `--list` | Print all approved current stage/logical IDs and argv mappings, historical baseline 50/51 and current totals; no build, restore or SDK requirement. Mutually exclusive with execution/selection flags. |
| `--only ID` (repeatable) | Select named stage IDs; logical `AUTH01B` selects both its stages. Exact case-sensitive IDs; unknown/empty ID is usage error 2. Deduplicate selections and retain inventory order. |
| `--tools-root PATH` | Override `UPSTREAM_UNIT_TOOLS`; default is its existing value or `/workspace/work/upstream-unit-tools`. Root contains `dotnet/`, not just an SDK directory. |
| `--output PATH` | Exclusive report directory; create a missing directory or accept an existing empty one, reject nonempty output to preserve old evidence. Default is unique `${TMPDIR:-/tmp}/bili-offline-<UTC>-<pid>`; print resolved path. |
| `--stage-timeout SECONDS` | Positive finite integer, default **300**. One compile/extract/run command, including its descendant processes, shares this budget. |
| `--suite-timeout SECONDS` | Positive finite integer, default **1800**. Overall execution budget; later unstarted selected stages become BLOCKED/budget, rather than disappearing. |
| `--repo-root PATH` | Explicit alternate source root for byte-identical snapshots/isolated negative controls. Must have `src/` and `plan/tests/`. Record root/provenance separately; this does not change the fixed manifest or permit arbitrary commands. |

No failure-ignore, optional-required, manifest-override, parallel, arbitrary-command or install/restore-upgrade switch. Package restores already required by original `dotnet run` remain allowed; no live API test is added.
Exit **0** means every selected executable stage passed and no inventory/report/preflight error affects that selection; **1** means selected FAIL/MISSING/BLOCKED/TIMEOUT/ERROR or inventory drift; **2** means invalid invocation, unusable launcher or report-write failure; interrupt/termination exits **130/143**, never 0. A partial selection can exit 0 but is labeled **PARTIAL_PASS**, with `complete_required=false`; it is not a full-suite gate.

## 3. Approved current inventory: 50 logical entries / 51 executable stages

There are **43 directory batches**, **42 csproj files**, **42 directory run.sh files**: g1 has a root-level runner; stab is shell-only. Register explicit IDs; do not derive the denominator only from paths that happen to exist. The resolved current inventory has the same 50/51 count as the foundation baseline, with broader comments coverage in the existing single executable (§3a). Counts alone do not certify old source inputs or a new suite pass. Future physical stages still require an explicit manifest/count review. Current directory IDs (43), all prefixed `batch:`:

```text
api02 auth1 auth2 auth3 auth4 c1 c2 c3 cap cardlayout cm1 comments
d1 d2 dirpress firstrun focus g1 g2 g3 g4 g5 localdir ov playersess
playertoolbar season1 settings1 smart1 stab toastalign ux0 ux1a ux1b
ux1c ux1d ux2a ux2b ux3 ux4 ux5 uxfix webrefresh
```

Directory order is the alphabetical order above; `batch:comments` retains its single accepted physical-stage mapping under §3a. `batch:g1` invokes `bash plan/tests/run-g1.sh` and requires `g1/G1Probe.csproj`; `batch:stab` invokes `bash plan/tests/stab/run.sh` and requires no project. Every other batch invokes `bash plan/tests/<name>/run.sh` and requires its current csproj. Nested checks such as g3 RichTextFlow and playersess history checks remain inside the owning batch; do not inflate the stage count with individual assertions.

Append these independent executable stages in order:

| Stage ID | Logical ID | Exact command |
| --- | --- | --- |
| `LM01` | `LM01` | `bash plan/tests/run.sh --stage LM01` |
| `LM02` | `LM02` | Same, `--stage LM02` |
| `LM03` | `LM03` | Same, `--stage LM03` |
| `LM04` | `LM04` | Same, `--stage LM04` |
| `LM05` | `LM05` | Same, `--stage LM05` |
| `API01` | `API01` | `bash plan/tests/run-api01.sh` |
| `AUTH01B.account` | `AUTH01B` | `bash plan/tests/run-auth01b.sh --stage account` |
| `AUTH01B.loginvm` | `AUTH01B` | Same, `--stage loginvm` |

Validate required script/project/selected probe-source paths before execution. In a partial selection, unavailable unselected entries retain NOT_SELECTED with availability metadata and do not fail the selection; they still remain in the full manifest. In a default run those same entries are selected and MISSING/nonzero. Missing selected paths remain rows with MISSING, never omitted. Validate directory inventory against the explicit set, excluding generated `bin`/`obj`; unexpected new runnable directories produce an inventory error and nonzero while known stages still report. Adding a new required stage needs an intentional manifest/count review.
The independently owned **package-layout PowerShell probe is optional/separate**; its standalone path is `plan/tests/package-layout-probe.ps1`, while aggregate integration remains unconfigured under this contract. Report `extra_gates.package_layout = NOT_CONFIGURED` with reason `separately-owned, excluded`; never PASS or add it to the legacy/current required denominator. Do not invoke/discover/integrate it automatically. When its reviewed implementation lands, its own standalone command/evidence remains separate; adding it to this aggregate requires an explicit reviewed amendment. Windows/UWP build/device gates likewise are not legacy stages.

## 3a. Resolved comments integration and exact input binding

The updated plan and `20261124-api-request-maintenance/execution-index.md` release comments/tests after source/test commit **`1c7f6e4aacf3c0fa4bb7f7d70851308405b995cd`**, evidence metadata **`5210148fb8b3e63b32fb9f1f843ff8b5a063f1a6`**. Independently compared every bound comments source/test input with the released commit: all match. The final source owner records **DIALOG_CHECKS=153 FAILURES=0; PASS 1070 assertions**, retaining all prior 917 intents. This is affected-batch evidence, not a new whole-suite or QF2-runner pass.

**Final manifest:** `batch:comments` → logical `batch:comments` → **`["bash","plan/tests/comments/run.sh"]`**, one executable stage, required paths runner + `CommentsProbe.csproj` (and its real generated-source inputs). The runner's lines 10–17 extract actual full HttpResults into ignored `obj/probe-generated/HttpResults.cs`; line 33 makes **one** `dotnet run --project "$probe" -c Debug`. The csproj links that class, actual dialog/builder/parsers/DTOs; Program.Main calls original checks then `DialogRecoveryChecks.Run` before its single final result. Raw-body/dialog checks run inside this same executable. There is no second strict-fixture executable, separate raw-body invocation, or extra selector requirement.

SHA-256 binding (exact source at release and current read):

| Input | SHA-256 |
| --- | --- |
| `plan/tests/comments/run.sh` | `18881fb069e255e686a2f22ab2b2b38e8e60e37c3dc76cf824b2cf06766094b4` |
| `plan/tests/comments/CommentsProbe.csproj` | `7e177f90708ca7b19811868b376a5044970afc34679c91998744d63696fd9bfe` |
| `plan/tests/comments/Program.cs` | `d0ffdb212e5d3467f1f5db2e1d3d53240a11430523afc2e76c4cea548fc881e9` |
| `plan/tests/comments/DialogRecoveryChecks.cs` | `105d587329b3a87662d78460d430e09e12e69179329a90c712436b96546f41f3` |
| Actual product `Helpers/HttpHelper.cs` | `b12e457a75a133e9298fb77ad982b33986c2f95da2562b4f031b1fd692424d13` |
| Exact generated HttpResults wrapper | `8aa4c8734aed85517df1dc3863a479fc076a262e775a7c657e45e9f8b9bc6a93` |

Evidence binds every additional compile/default input and the exact 51-entry command/project manifest. Canonical manifest SHA-256: **`3b240e48f78bd7020abcb038eaa37a005a198452c44828f3ae0e0fd80ea676e6`** (UTF-8 JSON, sorted object keys, compact separators, inventory array order, no trailing newline in digest). This binds the proposed argv including LM/AUTH selectors; it does not claim those not-yet-implemented selectors executed.

**Current total: 43 directory batches + 7 logical single-file entries = 50 logical; 43 batch executables + 8 single-file executables = 51 executable stages.** Report both historical foundation 50/51 and current 50/51, with source/input/manifest identities so equal counts cannot disguise changed coverage. A complete default run requires every current stage PASS, including all integrated comment checks. Package-layout and runner self-controls remain separate. The former possible 50/52 branch is resolved and withdrawn; no conditional denominator remains.

Ownership amendment is unnecessary: comments ownership is released, but this QF2 executor **does not need and is not assigned edits** to comments/run.sh, its csproj/tests, decoder or dialog. Consume the existing default entry point unchanged. Missing extraction boundary, generated source or any actual check failure must propagate from that runner, never be replaced by a fake success or marker. If comments inputs change before execution, rebind/prove the current contract; any new physical invocation or requested selector edit needs explicit review/ownership, not an automatic extra stage or overlapping writer.

## 4. Legacy selectors and preservation contract

`plan/tests/run.sh` gains only `--stage all|LM01|LM02|LM03|LM04|LM05|API01|AUTH01B` and `--help`. No args and `--stage all` retain current order, compile lists, runtimeconfigs, extraction/copy behavior and fail-fast exit; do not make the legacy chain itself fail-soft. Selected LM stages must not compile or execute earlier LM stages. `API01` delegates to the existing API runner; `AUTH01B` delegates to its default both-stage mode. Invalid options exit 2 before compilation.
`plan/tests/run-auth01b.sh` gains only `--stage all|account|loginvm` and `--help`. Default/all preserves Account-cookie→LoginVM and stops on Account failure. `loginvm` independently performs only the shared primitive setup it requires (including actual ICookieStore extraction), then the existing LoginVM extraction/compilation/executable; it must not depend on a successful Account compile/run. `account` runs no LoginVM code.
Keep QF1's current real static readonly refresh-token mirror initializer, real service inputs, exact Account bodies, wrapper contract and signature-only async-void→Task instrumentation. Do not reinterpret the earlier withdrawn namespace review or modify any credential scenario/stub to implement selectors.
In explicit single-stage mode, emit deterministic stage/phase markers around actual `compile`, `run`, `complete` transitions (e.g. `PROBE_PHASE LM05 compile`). Completion is emitted only after the real executable exits 0. The aggregate verifies the selected LM/AUTH marker, so a selector that silently does nothing cannot pass. Marker output adds no substitute assertion or business behavior. The unchanged API01/direct directory runners need not acquire new interfaces/markers.
Use one shared selected-stage implementation per legacy block; no copied second compile list or duplicated method body. Common preparation may move to a local function/block, but actual inputs and default behavior must remain equivalent. `--help`/invalid arguments must not create compilation side effects.

## 5. SDK, execution, statuses and evidence contract

Before starting any .NET stage, validate the resolved root's executable and **SDK 8.0.427, runtime/ref pack 8.0.33** together: exact csc path, `sdk/8.0.427/Containers/tasks/net8.0/Newtonsoft.Json.dll`, nonempty `packs/Microsoft.NETCore.App.Ref/8.0.33/ref/net8.0/*.dll` including System.Runtime, runtime directory/list, and `dotnet --version` from source cwd equals 8.0.427. Do not substitute another SDK through symlinks or rely on a machine's newer default.
Set `UPSTREAM_UNIT_TOOLS`, `DOTNET_ROOT=<tools>/dotnet`, prepend that executable directory to PATH (g1 uses PATH), and put CLI/NuGet/XDG caches under `<tools>/cache/` using existing conventions; disable CLI telemetry/first-run prompts. Preserve caller source/settings; never repurpose HOME. Installing tools is outside the aggregate. A wrong/incomplete toolchain blocks affected stages without launching them; independent stab may still run if its own prerequisites exist. No sparse configuration change or lazy blob retrieval.
Run one command at a time in source root; no concurrent gRPC output generation. Use monotonic deadlines: the active child observes `min(stage_start + stage_timeout, suite_start + suite_timeout)`; suite time starts before execution preflight/first spawn, and a shorter remaining suite budget is never replaced by the default 300-second stage budget. Each child has an owned process group. At the earlier deadline, terminate, allow ≤5 seconds to drain, then kill/reap remaining descendants. An attempted active child is **TIMEOUT/reason=suite_budget** if the suite deadline wins (including an equal deadline), or **TIMEOUT/reason=stage_budget** if its stage deadline wins. Do not relabel an attempted child BLOCKED/unexecuted. After suite exhaustion no next stage launches; mark unstarted selected rows **BLOCKED/reason=suite_budget**, finalize reports and exit 1. Stage-only timeout may continue only while suite budget remains. On SIGINT/SIGTERM the active child is **CANCELLED/reason=signal:SIGINT|signal:SIGTERM**, unstarted selected rows are **BLOCKED/reason=cancelled**, same bounded cleanup/finalization, aggregate exit **130/143** respectively; cancellation cannot become success or an unexecuted row. Cleanup/final report writing is the bounded allowance after the expired execution budget, not authorization to start more stages. A stage failure does not stop later independent stages. At overall budget/cancellation, finalize every remaining selected unstarted row as BLOCKED with exact reason (unselected rows stay NOT_SELECTED); do not claim those stages executed. External job kill may prevent final output; missing/unfinished reports cannot pass.

| Terminal stage status | Meaning |
| --- | --- |
| PASS | Selected actual command exit 0; required completion marker present where specified. |
| FAIL | Actual nonzero command exit; record exact code and phase/diagnostic. |
| MISSING | Selected required entry-point/project/probe source absent. |
| BLOCKED | Known prerequisite unavailable, suite budget exhausted or execution cancelled before start. |
| TIMEOUT | Attempted child reached the earlier deadline: reason=stage_budget or suite_budget; capture/reap result. Only stage-only timeout with remaining suite budget may continue. |
| ERROR / CANCELLED | Runner launch/internal protocol error / active child interrupted. Both prevent success. |
| NOT_SELECTED | Intentionally outside a partial selection; never counted PASS. |

Classify prerequisite, compile/restore, assertion, crash, timeout and unknown-command failure separately **when supported by evidence**. Explicit phase markers make selected csc compile/run distinctions reliable. Unmodified `dotnet run` mixes build and assertion output: retain FAIL/exit and an honest `failure_kind=command` plus bounded compiler/MSBuild hints when exact classification is unavailable; do not fabricate an assertion or environmental diagnosis. Missing product-source diagnostics must name paths and distinguish known absent/sparse inputs from code errors; no auto-fetch/repair. Raw output is authoritative evidence, not a classifier's guessed cause.
Logical PASS requires all its stages selected and PASS. AUTH01B with only one stage selected is PARTIAL (or FAIL if that selected stage fails), not logical PASS. Every summary contains the fixed historical baseline 50/51 and the approved current manifest totals from §3a, selection totals, PASS/failed/missing/blocked/not-selected counts and `complete_required`. No denominator shrinks due to a missing path or failed prefix.
Outputs: atomic incremental `results.json`, human `summary.md`, and `logs/<safe-stage-id>.log`. Initialize rows before execution and checkpoint after each terminal result. Record schema version, source root, UTC time, git HEAD/dirty state, platform/Python/Bash/dotnet versions, resolved inputs, selected argv/cwd, status/exit/reason/phase/duration, log path and truncation. A non-git snapshot records source SHA unavailable/unverified rather than borrowing another cwd's SHA; acceptance of snapshot runs additionally needs the executor's input hash manifest.
Bound each stage's retained combined stdout/stderr to **1 MiB** (bounded head+tail while continuing to drain child output), record bytes seen and truncation. Normal terminal output is a stage-status table, not complete logs. Reports plus the 51 current required bounded logs stay under **60 MiB**; do not upload source/extracted files, bin/obj, caches, account data, environment dumps, real cookies/tokens or API captures. Preserve only synthetic-test diagnostics. Log truncation must not change the child's exit status or turn a failure into a pass.

## 6. Minimal GitHub Actions contract

One new `.github/workflows/offline-probes.yml`, workflow **Offline probes**, job ID `offline`, job name **Offline probes (legacy)**, `ubuntu-24.04` x64. Trigger on push to `dev` and `pull_request` targeting `dev`, with no path filters; no pull_request_target, workflow_run, schedule, deployment or release trigger. Workflow `permissions: { contents: read }`; no secrets, token forwarding, credential persistence, branch protection writes or live requests. Set `defaults.run.shell: bash`.
Use these official action refs verified from current upstream tags/README; implementation pins the full SHA, with major-version comment. Do not silently replace with a moving tag:

| Action | Verified immutable ref |
| --- | --- |
| actions/checkout v7 | `3d3c42e5aac5ba805825da76410c181273ba90b1` |
| actions/setup-dotnet v6 | `0f4a354dd5547a28c70b7853dbd95d5b67886e73` |
| actions/upload-artifact v7 | `cf430e030ddbb5b0abf93d22962f4752f3646cd9` |

Checkout full source content with `fetch-depth: 1`, `persist-credentials: false`, no filter/sparse-checkout. History depth one suffices for recording the tested HEAD; no git history mutation occurs. Official current Node24 actions require a compatible hosted runner (setup-dotnet documents ≥2.327.1); a self-hosted/old runner fallback is not authorized.
Install exact `dotnet-version: '8.0.427'`, `check-latest: false`, `cache: false` into `DOTNET_INSTALL_DIR=${{ runner.temp }}/offline-tools/dotnet`; set `UPSTREAM_UNIT_TOOLS=${{ runner.temp }}/offline-tools`, DOTNET_ROOT and cache paths accordingly. No `8.0.x`, automatic roll-forward, global.json/dependency migration or setup-dotnet cache requiring nonexistent lockfiles. The aggregate's exact runtime/ref/compiler validation still runs; setup success alone is insufficient. This pins verified version/source and checks installed inputs, not a claim that this task already ran GitHub's installer.
Job timeout **40 minutes**, test step **35 minutes**, aggregate suite budget **1800 seconds**, upload step **3 minutes**. After checkout/setup, run the meaningful contract controls (§7) and the **unselected-default complete suite**; capture both statuses explicitly and fail the step if either is nonzero. Run the suite even when a control fails so diagnostics are not masked. No `continue-on-error`, `|| true`, or pipe-to-tee exit masking. A successful upload never clears earlier failure.
Use `actions/upload-artifact` with `if: ${{ always() }}`, name `offline-probes-${{ github.run_id }}-${{ github.run_attempt }}`, path only `${{ runner.temp }}/offline-results/`, `retention-days: 7`, `if-no-files-found: error`, `include-hidden-files: false`, `compression-level: 6`. Contract-control reports and suite reports use separate children of that directory; no broader workspace/cache upload. Under cancellation/hard job termination upload is best effort, not guaranteed; the run cannot be accepted from a partial/missing report.
The combined CI run step uses this exact command shape; implement explicit captured-status branching so both commands run even if the first fails (Bash starts with `-e`, so temporarily disable it only for these captures):

```bash
set +e
python3 plan/tests/verify-suite-runner.py --output "$RUNNER_TEMP/offline-results/contract"
contract_status=$?
bash plan/tests/run-all.sh --output "$RUNNER_TEMP/offline-results/suite"
suite_status=$?
set -e
if [ "$contract_status" -ne 0 ] || [ "$suite_status" -ne 0 ]; then exit 1; fi
```

Official references (read with read-docs workflow): [workflow syntax](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax), [setup-dotnet](https://github.com/actions/setup-dotnet), [checkout](https://github.com/actions/checkout), [upload-artifact](https://github.com/actions/upload-artifact). Exact-version syntax, explicit install directory, permissions, branch filters, Bash/timeouts/conditions and artifact options were verified; pinned README hashes and retrieval limits are in evidence.

## 7. Meaningful acceptance, not implementation-mirroring tests

New `plan/tests/verify-suite-runner.py` accepts `--tools-root PATH` (default same tools environment) and `--output PATH` (new/empty report root); unknown arguments exit 2. It exits 0 only when all runner contract controls pass, 1 for a failed control. It drives the **actual** aggregate/selector interfaces in isolated temporary fixture/source roots **outside the report/upload subtree** (e.g. `$RUNNER_TEMP/offline-fixtures` as a sibling of `offline-results`, or an independent TMPDIR). Reject a fixture/source/cache location nested inside the output tree; only bounded report/log files go under `--output`, including on failure. It may supply tiny controlled exit scripts as orchestration fixtures, but must not implement a second runner or emulate product assertions. Assert externally visible status/exit/denominators/continuation, not private function structure. Keep these controls separately labeled from legacy totals.

1. **Failure propagation:** select `batch:firstrun` and later `batch:focus` in an isolated control root. First actual invoked stage exits 7 deliberately; later stage emits its control marker and exits 0. The aggregate exits 1, lists both terminal rows (FAIL 7, PASS), retains inventory 50/51, selected 2 logical/2 executable and complete=false. No global missing-SDK error may confound this fixture: supply/check the real declared toolchain.
2. **Missing required stage:** same selected pair with the first required runner path absent. Aggregate exits 1, reports MISSING then later PASS, retains historical 50/51 plus current manifest totals and selected 2 logical/2 executable. Removing a path cannot reduce the count or silently skip it.
3. **Timeout cleanup:** first fixture spawns a bounded child that would write a delayed marker; stage timeout fires, descendant is reaped, later control still reports. Assert TIMEOUT/nonzero and no late child marker; do not leave a background process.
3a. **Active suite deadline:** reuse the controlled process fixture with stage budget 30 seconds and suite budget 2 seconds; the first stage announces its spawned child and remains active. The aggregate must terminate it at the earlier suite deadline (within the existing ≤5-second cleanup allowance), report **TIMEOUT/suite_budget** for that attempted stage, **BLOCKED/suite_budget** for later unstarted selected rows, exit 1, retain exact current totals 50/51 and selected counts, and never launch the later fixture. Final reports must be parseable and no owned descendant/delayed marker survives. This fails if budgeting is checked only between stages or waits 30/300 seconds.
3b. **SIGINT and SIGTERM finalization:** two bounded isolated runs using the same fixture. After it explicitly announces a spawned child, the control sends SIGINT or SIGTERM **to the aggregate**, exercising its handler/forwarding/cleanup rather than directly killing the child. Require active CANCELLED with the exact signal reason, unstarted selected BLOCKED/cancelled, no later launch, unchanged 50/51/current selection denominators, parseable final reports under graceful cleanup, no surviving/delayed child marker, and aggregate **130/143** respectively. Coordinate from readiness rather than an arbitrary race sleep. Hard SIGKILL/host death remains outside guaranteed report finalization.
4. **True selector independence:** in a byte-identical repaired scratch checkout, inject `#error` only into LM01.cs; default legacy chain must fail there, but `run.sh --stage LM05` must still execute the real LM05 assertions and pass. Similarly break only AUTH01B.cs: default AUTH runner fails Account, while `--stage loginvm` runs the unchanged real LoginVM scenarios and passes without Account completion. Retain source hashes, selected completion markers and actual counts. These destructive controls touch scratch only and need not recompile the full current suite twice.
5. **Default compatibility and real suite:** original no-arg `run.sh` still executes LM01–LM05/API01/both AUTH01B stages in order and stops on a failing prefix. On the final repaired/current sources, run the actual new aggregate default and require **all current logical/executable entries PASS** (**50/50 logical, 51/51 executable**, exact integrated manifest in §3a), exit 0, complete=true; every original behavior intent remains. QF1's earlier scratch sweep is corroboration, not this gate's result.
6. Check missing/wrong exact SDK input produces BLOCKED/nonzero with path/version evidence and no substituted compiler; `--list` still works. Verify unknown selector returns 2 without execution. Reports must be parseable and bounded. `bash -n` for touched shell files, Python compilation/syntax check, `git diff --check` and independent workflow syntax review are appropriate targeted checks.

Run contract controls and full aggregate locally at final implementation SHA, record exact commands/outputs/source integrity, then serialized commit/push. After workflow publication, query the **actual GitHub check/run for that pushed SHA**, event, runner, conclusion and artifacts. A local Linux run or YAML inspection is not a GitHub PASS. If CI permissions/service/queue/network are unavailable, report CI BLOCKED with evidence and exact next action; do not mark the workflow verified or required based on fixture results. No Windows/device pass follows.

## 8. Exact later ownership, gates and blockers

One executor after independent PASS and explicit release owns only new `plan/tests/run-all.sh`, `run-all.py`, `verify-suite-runner.py`; selector-only changes to `plan/tests/run.sh` and `plan/tests/run-auth01b.sh`; new `.github/workflows/offline-probes.yml`; this round's assigned execution evidence. `comments/run.sh` and tests have been released, but remain read-only to this executor: their existing single-stage default already provides all required behavior; no comments ownership/selector amendment is needed for this scope. `run-api01.sh`, g1/stab/directory runners, all product/stub/scenario files, dependencies, build scripts and optional package-layout probe are read-only. No .gitignore change is needed with temp outputs.
**QF1 ownership takes precedence:** its execution now records commit/push and release after its evidence-only commit. The orchestrator must verify the final commit, completed writer/process and actual ownership release before assigning either existing LM/AUTH script; the completed comments release and read-only consumption are bound in §3a. Review fixes to QF1 require their own owner/window; do not overwrite them with an earlier snapshot. Shared index/commit/push windows remain serialized.
The later single documentation owner updates the new aggregate/Bash/selector commands in `docs/DEVELOPMENT.md` and designated repository instruction references (`AGENTS.md`, `cloud/REPO.md`) without adding round memory there. Runner executor does not edit main docs or another round's reviewed artifacts. Any ownership expansion needs explicit dispatch/review.
Current status: current inventory/interface/source/official CI behavior specified and released comments integration resolved; **READY FOR FULL RE-REVIEW**, no execution or CI rollout before independent PASS; **implementation NOT STARTED**, selector/runner/CI fixtures NOT RUN because those interfaces do not yet exist. QF1 full sweep passed its recorded snapshot; no new-runner full-suite/CI pass is claimed. Package-layout integration is NOT_CONFIGURED/separate. UWP compilation, real package inspection, installed startup/device/live-session checks remain FAILED/BLOCKED on this Linux environment and are excluded from this offline gate.
Independent implementation review must confirm no lost compile input/extraction/assertion, fixed denominators, genuine negative controls and full final-source suite result, clean declared ownership, truthful partial/cancellation/optional statuses, exact action/tool pins and the pushed-SHA GitHub result (or explicit external block). Passing this offline workflow cannot satisfy repository-wide Windows/runtime stop conditions or authorize release/master changes.
