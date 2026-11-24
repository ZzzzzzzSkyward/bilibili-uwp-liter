# QF1 final offline verification evidence

Product source: `10dd84857c748f6638e187d891d6a6649f887bb6` on dev, after U1/PB1 source ownership release. Harness: final QF1 in-flight files captured before testing. **Effective complete-snapshot result: 43/43 directory batches + 7/7 logical single-file entries = 50/50 logical entries PASS; all 51/51 executable stages PASS.** Both AUTH01B executables ran. No unexecuted suffix remains. No Windows UWP/device/release pass is claimed.

## Environment, commands and source integrity

Linux Debian 13 x86_64; Bash; SDK 8.0.427 / runtime and ref pack 8.0.33 from the exact verified existing scratch SDK in baseline.md. No dependency upgrade/install, source migration or source modification by this executor. Environment:

```bash
export UPSTREAM_UNIT_TOOLS=/workspace/work/maintenance-foundation/quality
export DOTNET_ROOT="$UPSTREAM_UNIT_TOOLS/dotnet"
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_CLI_HOME="$UPSTREAM_UNIT_TOOLS/cache/dotnet-cli"
export NUGET_PACKAGES="$UPSTREAM_UNIT_TOOLS/cache/nuget"
export XDG_CACHE_HOME="$UPSTREAM_UNIT_TOOLS/cache/xdg"
export XDG_DATA_HOME="$UPSTREAM_UNIT_TOOLS/cache/xdg-data"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
cd /workspace/work/maintenance-foundation/quality-execution/snapshot
```

All tests use an independent copied snapshot: 917 existing tracked files plus the new HistoryCheckpointProbe.cs. SHA-256 manifest before/after covers all 918 input files, zero mismatches; generated bin/obj files are scratch only. Product source has no shared-checkout diff from the recorded HEAD. Raw evidence and helpers: `/workspace/work/maintenance-foundation/quality-execution/logs/` (`snapshot-manifest.json`, `snapshot-source-state.txt`, `snapshot-integrity.json`, `whole-results.json`, `effective-results.json`, per-batch logs). A serial scratch Python subprocess helper runs the exact Bash commands below with a 240-second per-command timeout and keeps every exit/output; it implements no test logic, modifies no code and is not a new repository runner. No gRPC-dependent processes overlapped.

Initial corrected-code sweep retained one **analysis snapshot setup failure**: copyfile had dropped executable modes, so the unmodified chain reached LM05 PASS then exited 126 invoking run-api01.sh (`Permission denied`). The original checkout and git index already have executable scripts. Copymode restored original executable bits in the scratch snapshot only; content hashes remained identical. Re-running the same unchanged `bash plan/tests/run.sh` exited 0 and ran all eight executables. Original whole-single-files.log (failure), corrected whole-single-files-corrected.log and both result manifests remain; the first failed command has not been recast as a pass. The effective inventory uses the successful exact-input mode correction; there were 44 initial sweep commands plus this one corrected-chain rerun, excluding targeted/negative controls.

## Red reproduction before harness restoration

| Command | Exit / evidence |
| --- | --- |
| `bash plan/tests/g3/run.sh` | 1: CS0103 ReserveCardCopy missing from compile inputs. |
| `bash plan/tests/playersess/run.sh` | 1: 1/203 failed, obsolete literal guard assertion. |
| `bash plan/tests/run.sh` | 1: LM01–LM04 PASS; LM05 CS0246 subtitle types; later stages unexecuted. |
| `bash plan/tests/run-api01.sh` | 1: CS0246 actual refresh policy/transaction types omitted. |
| `bash plan/tests/run-auth01b.sh` | 1: actual Account extraction dependencies absent; LoginVM unexecuted. |

Raw red logs: red-g3-run.sh.log, red-playersess-run.sh.log, red-run.sh.log, red-run-api01.sh.log, red-run-auth01b.sh.log. Additional genuine compile-red from the incorrect namespace dispatch is compile-run-api01.sh.log (CS0266 at both ApiHelper.Cookie assignments). Independent amendment PASS restores the original real wrapper; see ownership-conflict.md / amendment-review.md. Later AUTH01B compilation exposed duplicate substitute types when linking real UserModels; only those duplicate declarations were removed, preserving scenario assertions.

## Final directory sweep

Every row exits 0. Terminal result is copied from retained command output; source-wiring checks remain identified as such in their owning probes and are not rendered UI claims.

| Batch | Command | Terminal result | Seconds |
| --- | --- | --- | --- |
| `api02` | `bash plan/tests/api02/run.sh` | PASS 12 assertions | 9.01 |
| `auth1` | `bash plan/tests/auth1/run.sh` | PASS 35 assertions | 1.34 |
| `auth2` | `bash plan/tests/auth2/run.sh` | PASS 49 assertions | 1.95 |
| `auth3` | `bash plan/tests/auth3/run.sh` | PASS 239 assertions | 1.64 |
| `auth4` | `bash plan/tests/auth4/run.sh` | PASS 134 assertions | 1.53 |
| `c1` | `bash plan/tests/c1/run.sh` | PASS 186 assertions | 2.13 |
| `c2` | `bash plan/tests/c2/run.sh` | PASS 162 assertions | 1.8 |
| `c3` | `bash plan/tests/c3/run.sh` | PASS 117 assertions | 1.64 |
| `cap` | `bash plan/tests/cap/run.sh` | PASS 43 assertions | 1.58 |
| `cardlayout` | `bash plan/tests/cardlayout/run.sh` | TOTAL_FAILURES=0 | 1.35 |
| `cm1` | `bash plan/tests/cm1/run.sh` | PASS 71 assertions | 1.73 |
| `comments` | `bash plan/tests/comments/run.sh` | PASS 917 assertions | 1.66 |
| `d1` | `bash plan/tests/d1/run.sh` | PASS 82 assertions | 1.26 |
| `d2` | `bash plan/tests/d2/run.sh` | PASS 58 assertions | 1.81 |
| `dirpress` | `bash plan/tests/dirpress/run.sh` | dirpress PASS —— 31 项检查全过 | 1.39 |
| `firstrun` | `bash plan/tests/firstrun/run.sh` | firstrun PASS —— 15 项检查全过 | 1.36 |
| `focus` | `bash plan/tests/focus/run.sh` | focus PASS —— 33 项检查全过 | 1.36 |
| `g1` | `bash plan/tests/run-g1.sh` | PASS 102 assertions | 4.57 |
| `g2` | `bash plan/tests/g2/run.sh` | PASS 22 assertions | 1.99 |
| `g3` | `bash plan/tests/g3/run.sh` | PASS 386 assertions | 1.8 |
| `g4` | `bash plan/tests/g4/run.sh` | PASS 149 assertions | 2.25 |
| `g5` | `bash plan/tests/g5/run.sh` | PASS 335 assertions | 3.72 |
| `localdir` | `bash plan/tests/localdir/run.sh` | PASS localdir 50 assertions | 1.59 |
| `ov` | `bash plan/tests/ov/run.sh` | PASS 29 assertions | 1.69 |
| `playersess` | `bash plan/tests/playersess/run.sh` | 全部通过:217 项 | 1.54 |
| `playertoolbar` | `bash plan/tests/playertoolbar/run.sh` | TOTAL 103 checks, 0 failed | 1.45 |
| `season1` | `bash plan/tests/season1/run.sh` | PASS 117 assertions | 2.02 |
| `settings1` | `bash plan/tests/settings1/run.sh` | PASS 44 assertions | 1.73 |
| `smart1` | `bash plan/tests/smart1/run.sh` | PASS 160 assertions | 2.44 |
| `stab` | `bash plan/tests/stab/run.sh` | PASS 55 assertions | 0.26 |
| `toastalign` | `bash plan/tests/toastalign/run.sh` | toastalign PASS —— 41 项检查全过 | 1.67 |
| `ux0` | `bash plan/tests/ux0/run.sh` | PASS 50 assertions | 1.99 |
| `ux1a` | `bash plan/tests/ux1a/run.sh` | PASS 955 assertions | 1.68 |
| `ux1b` | `bash plan/tests/ux1b/run.sh` | PASS 178 assertions | 1.7 |
| `ux1c` | `bash plan/tests/ux1c/run.sh` | PASS 99 assertions | 1.79 |
| `ux1d` | `bash plan/tests/ux1d/run.sh` | PASS 107 assertions | 1.8 |
| `ux2a` | `bash plan/tests/ux2a/run.sh` | PASS 76 assertions | 1.99 |
| `ux2b` | `bash plan/tests/ux2b/run.sh` | PASS 151 assertions | 1.94 |
| `ux3` | `bash plan/tests/ux3/run.sh` | PASS 111 assertions | 1.74 |
| `ux4` | `bash plan/tests/ux4/run.sh` | PASS 55 assertions | 1.72 |
| `ux5` | `bash plan/tests/ux5/run.sh` | PASS 106 assertions | 1.43 |
| `uxfix` | `bash plan/tests/uxfix/run.sh` | PASS 85 assertions | 1.76 |
| `webrefresh` | `bash plan/tests/webrefresh/run.sh` | PASS 111 assertions | 1.66 |

## Single-file chain and independent credential gates

`bash plan/tests/run.sh`: exit 0 after scratch mode correction; LM01 PASS (12 validation cases plus filesystem load), LM02 PASS (import/reopen/path/assets/dedup), LM03 PASS (playlist/grants/tracks/off/resume/context), LM04 PASS (switch coordinator), LM05 PASS (22 assertions), API01 PASS (76), AUTH01B Account-cookie PASS (70), AUTH01B LoginVM PASS (39). Seven logical entries, eight executables. Original assertions are retained except the independently reviewed refresh literal/setup mapping; AUTH01BLoginVM.cs is byte unchanged.

Independent `bash plan/tests/run-api01.sh` exits 0, 76 checks (`api-fixtures.log`). Independent `bash plan/tests/run-auth01b.sh` exits 0, both 70/39-check executables (`next-run-auth01b.sh.log`). `bash plan/tests/auth1/run.sh` (35), `api02/run.sh` (12), `auth3/run.sh` (239), `auth4/run.sh` (134) and `webrefresh/run.sh` (111) all exit 0 independently and again in the final sweep. API01Stubs.cs itself is unchanged; the newly planned search owner may use its existing contract only after QF1 release. Original current global compatibility-wrapper assignments both remain valid actual-product constructions.

## Actual-source session negative controls

The unchanged owning playersess command passes **217** checks: all 203 original check positions/intents (one stale literal replaced by wiring plus stronger behavior) and 14 new actual-source behavioral checks. The final runner extracts actual history field initializers and four method bodies; see source-extraction.md for exact ranges/hashes. The platform wrapper supplies no serialization, pending-tail or flush implementation.

1. Scratch `/workspace/work/maintenance-foundation/quality-execution/PlayerControl-overlap.cs`: replace sole `if (_historyCheckpointRunning)` by `if (false)`, leaving other current methods unchanged. Command `PLAYER_HISTORY_SOURCE=/workspace/work/maintenance-foundation/quality-execution/PlayerControl-overlap.cs bash plan/tests/playersess/run.sh` exits **1**, 3/217 failed: concurrent checkpoints overlap, latest-only pending tail is lost, identical tail suppression fails. Final raw log negative-overlap-final.log. Expected CS0162 warning belongs to this scratch mutation only.
2. Scratch PlayerControl-drop-tail.cs: insert `if (_historyCheckpointRunning) { await _historyReportTask; return; }` immediately before the real pending assignment, retaining serialization but dropping updates during the active POST. Command `PLAYER_HISTORY_SOURCE=/workspace/work/maintenance-foundation/quality-execution/PlayerControl-drop-tail.cs bash plan/tests/playersess/run.sh` exits **1**, 1/217 failed: latest pending tail missing. Raw log negative-drop-tail.log.
3. Unchanged real source with the same checks exits **0**, 217/217 (`green-history-fields.log` and final whole-playersess.log). No negative mutation touches the repository or enters the aggregate denominator.

Credential permanent negative controls remain: missing business code reaches the real refresh response with otherwise-valid replacement jar/token and no confirmation/commit; endpoint-specific confirmation 503 retains its bound pending transition and retries only confirmation. Rollback SetCookie/setting failures and secret-free logs remain, as do unchanged LoginVM cancel/late-response/logout/retry/one-commit cases. Per-intention mapping: assertion-mapping.md.

## Warnings, structural scope and platform limits

No warning suppression or assertion-count reduction. Existing generated/source warning classes: CS8981, CS0108, CS1998, CS0067; csc SDK-internal Newtonsoft.Json unification CS1701. AUTH01B's test-only async-void-to-Task instrumentation exposes CS4014 at original unawaited UI call sites; bodies remain verbatim and existing deterministic lifecycle assertions pass. g3 has exactly one new real formatter include, without duplicate-source warnings.

`git diff --check` exits 0. Programmatic outside-scope text comparison confirms API01 outside its three named methods unchanged; AUTH01B outside its three named methods unchanged; AUTH01BLoginVM.cs byte unchanged; LM05.cs differs only in its one import. Quality changes are only assigned harness/evidence; product/comments/c1/season1/manifest/build/main-doc files are not edited by this executor. No fake global service, successful credential bridge/policy or pending-tail implementation was added. Actual product helper files compile under existing stubs; UI/rendering/real WinRT transport remains outside offline scope.

Windows `powershell ... build-release-x64.ps1 -Fast -NoVersionBump -Restore` remains FAILED/BLOCKED, prerequisite exit 127 `powershell: command not found` from baseline.md. This Linux environment lacks the VS/UWP/Windows SDK/runtime gate. Full MSIX/package inspection, installed startup, real cookie manager/login/session behavior, playback/layout/accessibility and device journeys remain FAILED/BLOCKED; no package/version/live action performed. QF2 aggregate/CI and QF3 build gate work remain separately reviewed future slices, not silently implemented here.
