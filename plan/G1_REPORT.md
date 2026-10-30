# G1 execution report — app-key-aware gRPC headers

Orchestrated round on 2026-10-29. Branch `work`, entry HEAD `ae3cfac` (clean, in sync with `origin/work`).

## What this round did

1. `git pull` from `origin/work` (fast-forward; removed obsolete generated CSX artifacts).
2. Scanned the retained state and picked the first pending batch from `plan/PLAN.md`'s implementation order: **G1**, the remaining transport part of the old GRPC-01. Bug series stays at 14/15 (`#1311` partial mitigation, user visual review outstanding); AUTH/LM/API/gRPC-feed work is complete and needs user app testing.
3. Orchestrator dispatched all detailed work; no application code was written by the orchestrator.

## Dispatch chain

| Step | Agent | Outcome |
| --- | --- | --- |
| Reference retrieval | `scout` (G1ReferenceFetch) | Fetched all 10 files of the pinned sibling `src/BiliLite.gRPC/Api/` at `ca2caddc597b3faf05b4dac6dcb226aa0694365b` verbatim (byte sizes match the GitHub blobs), copied to `C:/temp/g1-ref/`. Corrected the plan's file locations: everything lives under `Api/`, not the project root. |
| Target inventory | `scout` (G1TargetInventory) | Established that `APP-KEY` is the literal `"android"`, the whole profile is frozen in `HttpClient.DefaultRequestHeaders`, `UserMid`/`AppKey` are dead payload, `GrpcBiliUserInfo` has exactly three construction sites (all passing `ApiHelper.LoginKey.Appkey`), and no test project touches gRPC headers. |
| Implementation | `task` (G1ImplementD) | Commit `cfcd3de`. |
| Review | `reviewer` (G1Review) | No blockers; four real gaps (iOS dispatch untested, base64 padding untrimmed-assertion gap, trace-id derivation unpinned, dead null-userInfo guard). |
| Review fixes | `task` (G1ReviewFixes) | Commit `75e8ecb`. |

Delegation note: the configured `modelRoles.task` (`opencode-go/longcat-2.5-preview-free`) resolves to "No model selected", so `task`/`subagent` presets failed. The orchestrator temporarily repointed the role at the working default model, dispatched, and restored the original value afterwards.

## Shipped behaviour

- Added `src/BiliLite.gRPC/Api/`: `Constants.cs`, `IGrpcRequestHeader.cs`, `GrpcRequestHeaderFactory.cs`, `AndroidGrpcRequestHeader*.cs`, `IosGrpcRequestHeader*.cs` — the pinned sibling implementation with only the proto namespace adapted (`Proto.Header`; `Network.Types.TYPE.Wifi`, `Locale.Types.LocaleIds`, `Device_`, `Metadata.Device`). All ported types stay `internal`.
- Deleted `src/BiliLite.gRPC/GrpcRequestHedaer.cs` (`GrpcRequestHeaderConfig`); repo-wide grep finds no remaining reference.
- `GrpcRequest.CreateSharedHttpClient()` now sets only the 100 s timeout. Every profile and account header is built per request by `GrpcRequest.ResolveHeader(userInfo)` and applied to each `HttpRequestMessage`, so one shared `HttpClient` serves Android, iOS and different accounts without header races. Both public `SendMessage` overloads share one core path; framing, logging, `GrpcResult` and the timeout are unchanged.
- Unknown, empty or user-typed app keys fall back to a shared Android profile instance, so the app's default login key `4409e2ce8ffd12b8` keeps its pre-G1 wire behaviour.

## Two parity decisions

1. Android sends `x-bili-network-bi` — the sibling's actual wire name (a long-standing typo in the reference). The pre-G1 target sent the correctly spelled `x-bili-network-bin`; the pinned reference wins. iOS keeps `x-bili-network-bin`.
2. The iOS profile is genuinely unreachable from this app's call sites today (all three construction sites pass the login key). The profile is ported because G1's deliverable is app-key-aware header selection, and because a user-configured `appkey_login_1` of `27eb53fc9058f8c3` reaches it. No caller was changed to force it.

## Verification actually run

- `dotnet build src/BiliLite.gRPC/BiliLite.gRPC.csproj` → 0 warnings, 0 errors.
- `"E:/Git/bin/bash.exe" plan/tests/run-g1.sh` → `PASS 102 assertions`, exit 0, stable across runs. `bash` on `PATH` is a WSL stub; the runner must be invoked through the Git bash.
- Probe coverage: factory key mapping and null handling, `ResolveHeader` dispatch for both app keys and all fallback inputs, complete Android/iOS header key sets and values, unpadded base64 for all `*-bin` payloads, protobuf round-trip of the metadata/device/network/locale/fawkes payloads, `GetAuroraEid` against an independently computed XOR vector, `GenTraceId` length/colon/second-segment/timestamp derivation, empty `DefaultRequestHeaders` on the shared client, and three loopback `TcpListener` captures (Android, iOS, anonymous) asserting the real request head.
- Bite tests: forcing the factory to the Android fallback, bypassing `ResolveHeader` in the send loop, dropping `TrimEnd('=')`, changing the trace-id slice, and removing the null normalization each produced a distinct probe failure before being restored. The assertions fail on real regressions rather than snapshotting output.
- `git diff --check` clean; working tree clean.

Not run: any UWP build (workload absent), application launch, live Bilibili API call, and `plan/tests/run.sh` (it depends on the `/workspace/work/upstream-unit-tools` SDK, which does not exist on this machine).

## Deviations and residual risk

- The review's prescribed null normalization used `null` for the access key; the protobuf `Metadata.AccessKey` setter calls `ProtoPreconditions.CheckNotNull`, so `string.Empty` is used instead and a null `GrpcBiliUserInfo` now produces a real anonymous Android request instead of an exception-derived failure.
- Real gateway behaviour of the renamed `x-bili-network-bi` header is untested by design: no live API call is permitted, and the sibling sends exactly this.
- The Android/iOS profile payloads use the sibling's fixed `buvid` values. This batch does not establish a real-buvid solution and does not claim the gRPC feed rejection is fixed; REST fallback stays.

## Open items handed forward

- **Published:** `cfcd3de`, `75e8ecb` and `e54557c` are on `origin/work`. The first push attempt failed while `github.com:443` was unreachable (`api.github.com` and `raw.githubusercontent.com` still answered); a later retry succeeded unchanged, so no history repair was needed.
- Next batch is **G2** (user-video keyword search through `Space/SearchArchive`) per `PLAN.md`; its G1 prerequisite is now satisfied. D1/D2/N1 remain independent.
- `#1311` (fully unmarked AI subtitle tracks) and the user visual review assignments are still open from the bug series.
