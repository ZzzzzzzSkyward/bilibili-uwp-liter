# Reproducible verification evidence

Date: 2026-11-24 UTC. Source branch/HEAD: `dev`, `41dc562fba6e45896b0c4ba604f9ec38f4661e73`. Status: **FAILED baseline; no whole-suite, UWP-build or runtime pass claimed.**

## Environment and source integrity

- Host: Linux x86_64, Debian 13; `/bin/sh` resolves to `/usr/bin/dash`.
- Intake PATH had no `dotnet`, `powershell` or `pwsh`. Curl, Python and Bash were available.
- `git config --get remote.origin.promisor` reports `false`; `core.sparseCheckout` is unset; `git ls-files -v` has no `S` entries. `PlayerControl.xaml.cs`, `Modules/Home/DynamicVM.cs`, `Pages/VideoDetailPage.xaml.cs`, `g3/Program.cs` and `dynamic.proto` exist. This differs from the historical clone described in `cloud/REPO.md`. No sparse setting or excluded blob was changed/fetched.
- `git ls-files .github` returned no paths: no current repository CI workflow. Queries for tracked `global.json`, `NuGet.Config`, `nuget.config`, `packages.lock.json`, `Directory.Build.props` and `Directory.Packages.props` returned none.
- The Cloud Environment runtime skill and its Packages guidance were read before installation. `/usr/local/bin/cloud-agent-packages` is absent, so a user-level SDK archive was used. No home or system package installation was performed.
- Scratch root: `/workspace/work/maintenance-foundation/quality/`. SDK, package/CLI caches, raw logs, snapshot and runner helper remain there. The raw manifest and exact captured command output are in `logs/`; only curated evidence is in this round.
- To preserve shared-checkout ownership, copied 697 existing tracked text files to `baseline/`, byte for byte, and linked the 197 remaining existing tracked assets into the snapshot. All source inputs were read from existing working-tree files; no git objects were materialized. SHA-256 source manifest was checked again after the probes, with **zero mismatches**. No original product/test/build file was edited and no original `bin`/`obj` output was produced.

## SDK installation and reproducible invocation

Download URL:

`https://builds.dotnet.microsoft.com/dotnet/Sdk/8.0.427/dotnet-sdk-8.0.427-linux-x64.tar.gz`

Archive bytes: 217060685. Extracted to `quality/dotnet/`. SHA-512 matches Microsoft's exact-version Linux-x64 SDK entry in `https://builds.dotnet.microsoft.com/dotnet/release-metadata/8.0/releases.json`:

```text
85725692f465f5024b9b42a159a29d9f73e8c57ad9e8bfe1e7028a505d18031d68931e8aa3f9de44145b08170dde897c8a78e1aa18d4219698b6891af7095d39
```

`dotnet --info` exit 0: SDK 8.0.427, commit 38c6707c62; MSBuild 17.11.69; host/runtime 8.0.33, x64; no workloads. The archive includes the scripts' exact `Microsoft.NETCore.App.Ref/8.0.33/ref/net8.0` and SDK-internal Newtonsoft.Json inputs. Full details are in scratch `logs/sdk-info.txt` and `logs/sdk-integrity.txt`.

Environment used (PATH prepends the local SDK):

```bash
export UPSTREAM_UNIT_TOOLS=/workspace/work/maintenance-foundation/quality
export DOTNET_ROOT="$UPSTREAM_UNIT_TOOLS/dotnet"
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_CLI_HOME="$UPSTREAM_UNIT_TOOLS/cache/dotnet-cli"
export NUGET_PACKAGES="$UPSTREAM_UNIT_TOOLS/cache/nuget"
export XDG_CACHE_HOME="$UPSTREAM_UNIT_TOOLS/cache/xdg"
export XDG_DATA_HOME="$UPSTREAM_UNIT_TOOLS/cache/xdg-data"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
cd /workspace/work/maintenance-foundation/quality/baseline
```

The baseline helper is `quality/run-baseline.py`; its only role is sequential command execution with per-command exit/duration/output capture and continuation after failure. It does not patch product/probe inputs. Equivalent individual commands appear below. Reproduction on a normal complete checkout can use those same commands from that checkout; its regular ignored bin/obj outputs will be generated there. All scripts examined have LF endings.

## Initial prerequisite failures

| Command | Exit | Exact decisive output / classification |
| --- | --- | --- |
| `sh plan/tests/firstrun/run.sh` before SDK setup | 127 | `Bad substitution`; `dotnet: not found` — wrong interpreter plus absent SDK. |
| `bash plan/tests/firstrun/run.sh` before SDK setup | 127 | `dotnet: command not found` — environment prerequisite. |
| `sh plan/tests/firstrun/run.sh` after SDK setup | 1 | `Bad substitution`; `MSBUILD : error MSB1009: Project file does not exist. Switch: //plan/tests/firstrun/FirstRunProbe.csproj` — documented command uses dash on a Bash script. |
| `powershell -NoProfile -ExecutionPolicy Bypass -File ./build-release-x64.ps1 -Fast -NoVersionBump -Restore` | 127 | `powershell: command not found` — UWP compile was not reached. No VS2022 UWP workload or Windows SDK/runtime is available. |

The Bash runner's shebang and `BASH_SOURCE` are visible in `plan/tests/firstrun/run.sh:1–6`. `docs/DEVELOPMENT.md:76–79`, `cloud/REPO.md` and `AGENTS.md` recommend `sh`; Linux dash does not honor the Bash shebang when invoked explicitly through sh. LF pinning addresses a separate problem.

## All directory batches

Inventory checked from local files: 43 directories, 42 csproj files, 42 directory run.sh files. g1 uses `plan/tests/run-g1.sh`; stab is a shell/source-wiring batch. Commands were sequential to avoid concurrent mutation of the shared gRPC build outputs in the snapshot.

**Effective complete-snapshot result: 41/43 PASS, 2/43 FAILED.** The table retains each original runner and reports the successful cm1 correction described afterward.

| Batch | Command | Exit | Terminal result |
| --- | --- | --- | --- |
| `api02` | `bash plan/tests/api02/run.sh` | 0 | PASS 12 assertions |
| `auth1` | `bash plan/tests/auth1/run.sh` | 0 | PASS 35 assertions |
| `auth2` | `bash plan/tests/auth2/run.sh` | 0 | PASS 49 assertions |
| `auth3` | `bash plan/tests/auth3/run.sh` | 0 | PASS 239 assertions |
| `auth4` | `bash plan/tests/auth4/run.sh` | 0 | PASS 134 assertions |
| `c1` | `bash plan/tests/c1/run.sh` | 0 | PASS 161 assertions |
| `c2` | `bash plan/tests/c2/run.sh` | 0 | PASS 162 assertions |
| `c3` | `bash plan/tests/c3/run.sh` | 0 | PASS 117 assertions |
| `cap` | `bash plan/tests/cap/run.sh` | 0 | PASS 43 assertions |
| `cardlayout` | `bash plan/tests/cardlayout/run.sh` | 0 | CHECKS=761; TOTAL_FAILURES=0 |
| `cm1` | `bash plan/tests/cm1/run.sh` | 0 | PASS 71 assertions |
| `comments` | `bash plan/tests/comments/run.sh` | 0 | PASS 99 assertions |
| `d1` | `bash plan/tests/d1/run.sh` | 0 | PASS 82 assertions |
| `d2` | `bash plan/tests/d2/run.sh` | 0 | PASS 58 assertions |
| `dirpress` | `bash plan/tests/dirpress/run.sh` | 0 | dirpress PASS —— 31 项检查全过 |
| `firstrun` | `bash plan/tests/firstrun/run.sh` | 0 | firstrun PASS —— 15 项检查全过 |
| `focus` | `bash plan/tests/focus/run.sh` | 0 | focus PASS —— 33 项检查全过 |
| `g1` | `bash plan/tests/run-g1.sh` | 0 | PASS 102 assertions |
| `g2` | `bash plan/tests/g2/run.sh` | 0 | PASS 22 assertions |
| `g3` | `bash plan/tests/g3/run.sh` | 1 | CS0103: ReserveCardCopy missing from probe compile inputs |
| `g4` | `bash plan/tests/g4/run.sh` | 0 | PASS 149 assertions |
| `g5` | `bash plan/tests/g5/run.sh` | 0 | PASS 335 assertions |
| `localdir` | `bash plan/tests/localdir/run.sh` | 0 | PASS localdir 50 assertions |
| `ov` | `bash plan/tests/ov/run.sh` | 0 | PASS 29 assertions |
| `playersess` | `bash plan/tests/playersess/run.sh` | 1 | 1 failure / 203 checks: obsolete literal single-flight guard check |
| `playertoolbar` | `bash plan/tests/playertoolbar/run.sh` | 0 | playertoolbar PASS —— 103 项检查全过 |
| `season1` | `bash plan/tests/season1/run.sh` | 0 | PASS 117 assertions |
| `settings1` | `bash plan/tests/settings1/run.sh` | 0 | PASS 44 assertions |
| `smart1` | `bash plan/tests/smart1/run.sh` | 0 | PASS 160 assertions |
| `stab` | `bash plan/tests/stab/run.sh` | 0 | PASS 55 assertions |
| `toastalign` | `bash plan/tests/toastalign/run.sh` | 0 | toastalign PASS —— 41 项检查全过 |
| `ux0` | `bash plan/tests/ux0/run.sh` | 0 | PASS 50 assertions |
| `ux1a` | `bash plan/tests/ux1a/run.sh` | 0 | PASS 955 assertions |
| `ux1b` | `bash plan/tests/ux1b/run.sh` | 0 | PASS 178 assertions |
| `ux1c` | `bash plan/tests/ux1c/run.sh` | 0 | PASS 99 assertions |
| `ux1d` | `bash plan/tests/ux1d/run.sh` | 0 | PASS 107 assertions |
| `ux2a` | `bash plan/tests/ux2a/run.sh` | 0 | PASS 76 assertions |
| `ux2b` | `bash plan/tests/ux2b/run.sh` | 0 | PASS 151 assertions |
| `ux3` | `bash plan/tests/ux3/run.sh` | 0 | PASS 111 assertions |
| `ux4` | `bash plan/tests/ux4/run.sh` | 0 | PASS 55 assertions |
| `ux5` | `bash plan/tests/ux5/run.sh` | 0 | PASS 106 assertions |
| `uxfix` | `bash plan/tests/uxfix/run.sh` | 0 | PASS 85 assertions |
| `webrefresh` | `bash plan/tests/webrefresh/run.sh` | 0 | PASS 111 assertions |

The first snapshot intentionally omitted binary assets, so cm1 initially exited 1 on `FAIL the transparent asset exists in the repo` (`cm1/Program.cs:235`). The actual original `src/BiliLite.UWP/Assets/Thumbnails/transparent.png` exists. Adding links to existing tracked assets repaired the analysis snapshot, after which **the unchanged** `bash plan/tests/cm1/run.sh` exited 0 with `PASS 71 assertions`. This is an analysis-harness correction, not a repository defect or suppressed assertion. Both raw attempts remain in scratch.

## Single-file chain and independently exposed failures

`bash plan/tests/run.sh` exited **1**:

| Logical stage | Result | Decisive evidence |
| --- | --- | --- |
| LM01 | PASS | `LM-01 actual-source checks passed (12 validation cases plus filesystem load).` |
| LM02 | PASS | Import/reopen, invalid paths, missing assets, retained broken entry, reimport deduplication passed. |
| LM03 | PASS | Actual playlist mapping/default StorageFile tracks/saved grants/retained metadata/subtitle-off/resume IDs/contextual failures passed. |
| LM04 | PASS | `LM-04 actual-source switch coordinator probes passed.` |
| LM05 | FAILED — compilation | CS0246 for `SubtitleModel` and `SubtitleItemModel` in actual SubtitleTrackParser; stub namespace disagrees with actual model namespace. |
| API01 | NOT REACHED by chain | Invoked independently below. |
| AUTH01B | NOT REACHED by chain | Invoked independently below; includes Account-cookie and LoginVM stages. |

To expose failures masked by `set -e`, ran `bash plan/tests/run-api01.sh` and `bash plan/tests/run-auth01b.sh` independently, with no source or script modification:

| Command | Exit | Result |
| --- | --- | --- |
| `bash plan/tests/run-api01.sh` | 1 | CookieService CS0246: `RefreshTokenDecision`, `RefreshTokenMirror`, `WebRefreshPendingRecord`, `IWebRefreshStateStore`, `WebRefreshPhase` are not included in the runner's compilation. |
| `bash plan/tests/run-auth01b.sh` | 1 | Extracted Account wrapper CS0117 for absent stub `ACCESS_KEY_EXPIRE_ESTIMATED`/`ACCESS_KEY_SOURCE`; CS0103/CS0246 for refresh/bridge types and `refreshTokenMirror` instance field. The first Account-cookie executable never builds, so the later LoginVM extraction/executable is unexecuted. |

Seven logical single-file probes therefore have **4 PASS, 3 FAILED to compile**, with the later AUTH01B substage unexecuted. The root chain is FAILED even though its first four executables pass. The scripts use transient `/tmp` directories and clean those through their own traps; their retained logs/caches are under the assigned scratch root.

## Failure classification and source corroboration

1. **g3: confirmed stale probe compile list.** Error: `Controls/Dynamic/DisplayModel.cs(73,28): error CS0103: The name 'ReserveCardCopy' does not exist in the current context`. Existing `Parsing/Dynamic/ReserveCardCopy.cs:13` declares the class; UWP csproj includes it at line 192; `g3/G3Probe.csproj:24–40` omits it. Proto, gRPC and g3 Program.cs were available; g1/g2/g4/g5/smart1 built against the real gRPC library. This is not the historical sparse-clone issue.
2. **playersess: confirmed stale source-literal assertion; no demonstrated runtime overlap.** Output: `失败 1 项(共 203 项): 检查点有在飞判定,慢 POST 不叠加`. `playersess/Program.cs:450` demands the old one-line early return. Product `PlayerControl.xaml.cs:3462–3474` stores latest pending state and awaits `_historyReportTask` when a report is active; `3486–3514` drains the pending tail and clears state in finally. Behavioral serialization coverage is needed; neither deleting the assertion nor reverting product behavior is justified.
3. **LM05: confirmed stub namespace drift.** Actual `SubtitleTrackParser.cs:7` imports `BiliLite.Modules`; actual `Modules/Player/PlayerVM.cs:12,319–334` puts models there. `LM05Stubs.cs:5` places them in `BiliLite.Modules.Player`. Missing symbol errors are not missing SDK/reference-pack errors.
4. **API01: confirmed dependency-list drift.** `run-api01.sh:13–26` links CookieService but not its refresh-policy/transaction dependencies, unlike working `api02/API02.csproj` and `auth3/AUTH3.csproj`. Do not substitute a fake successful credential service to obtain green.
5. **AUTH01B: confirmed extracted-wrapper/stub drift.** `run-auth01b.sh:21–28` selects methods; their current bodies depend on fields/services beyond its wrapper. Actual `Helpers/SettingHelper.cs:151,156` contains the keys absent from the probe stubs. No account behavior assertion ran in this independent invocation.

The failures above establish harness health defects. The source evidence supports their likely causes, but no requirements-stage patches were applied to prove every downstream failure resolved. Additional defects may surface after compilation is restored; acceptance must execute all assertions and record any new failure explicitly.

## Dependency query and warnings

After gRPC restore completed through its existing ProjectReference consumers, ran:

```bash
dotnet list src/BiliLite.gRPC/BiliLite.gRPC.csproj package --vulnerable --include-transitive --format json
```

Exit 0; configured source `https://api.nuget.org/v3/index.json`; returned a projects entry with no vulnerable packages. This is a time-specific query of that restored graph only. UWP/Win32Tools/packages.config/native components/unshipped legacy applications were not restored/audited as a complete graph, so no repository-wide security claim follows.

Observed offline warning classes include CS8981 (lower-case generated-style names), CS1998 (async without await), CS0108 (hidden inherited members), CS0067 (unused event) and CS1701 (SDK-internal Newtonsoft.Json assembly-reference unification in csc runners). They were not suppressed and did not account for the listed nonzero exits. This run does not generate or validate the known UWP WMC/PRI warnings.

## Unavailable platform gates and preservation

Windows `-Fast -NoVersionBump -Restore`, full solution/MSIX build, root-launcher/entrypoint inspection of a real generated package, installed-app startup and playback/account/layout/export/device smoke all remain **FAILED/BLOCKED — environment unavailable**. Exact prerequisite and follow-up commands are in the requirements artifact. No attempt was made to run the UWP app under Linux or to infer a device pass from .NET probes.

A static build-gate gap also exists: `build-release-x64.ps1:426–432` validates entrypoint, Win32Tools and `fullTrustProcess` but does not check the root launcher's existence/size required by `AGENTS.md §6`. This was read, not executed or changed. Its safe repair needs separate build ownership and package fixtures, followed by actual Windows release validation.

Final source integrity check: all 697 copied tracked text files still match original SHA-256 values. The scoped tracked diff outside `cloud/` is empty. Shared round/memory artifacts are being written concurrently by their owners; no claim is made that the entire repository is clean during orchestration. No product/tests/build/main documentation, manifest version, assets, sparse configuration, branch or git history was changed by this requirements task.
