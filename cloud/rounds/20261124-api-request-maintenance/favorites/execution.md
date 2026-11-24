# U9 execution

Status: **IMPLEMENTED + OFFLINE VERIFIED; independent implementation review pending; Windows/session gates BLOCKED.**

Started from `dev` / `858b97105ac8b46492af3810e5352afd9efbe8d5`; `git pull --ff-only` returned Already up to date. Existing cloud/other-owner changes preserved. Foundation commits remain unchanged.

## Changes and fidelity

Only two local FavoriteAPI source boundaries change: prepend `&` before `privacy` in CreateFavorite/EditFavorite. Public/private mapping, Android key/common arguments, field order, exact encoded text, IDs, configured host and metadata defaults stay unchanged. No shared helper/dialog/transport changes or live writes.

Owned tests: c2 project links complete real `Api/User/FavoriteAPI.cs`; C2Stubs adds deterministic common-parameter and signing **recorders** plus required account/key metadata. Common output ends at `ts=1700000000` with no separator. Recorder supplies a dummy suffix only and captures exact input/key/call count; it does not repair/reconstruct a form and does not certify cryptography. Existing fake MediaList transport and every old assertion remain unchanged.

Program adds 289 assertions over 16 cases: create/edit × public/private × guest/logged-in × reserved Unicode/emoji/newline/percent versus empty title/intro. Edit IDs include above-2^53 and leading-zero strings. Multiplicity-preserving pairs check exact common/feature/sign keys and values, one timestamp/privacy/signature, configured host/prefix, unchanged defaults, AndroidKey/true common inputs and complete encoded unsigned body before exactly one sign suffix. Original 162 c2 checks still run.

## Permanent real-source red/green

Environment/commands from repository root:

```bash
export PATH=/workspace/work/maintenance-foundation/quality/dotnet:$PATH
export UPSTREAM_UNIT_TOOLS=/workspace/work/maintenance-foundation/quality
export DOTNET_ROOT=$UPSTREAM_UNIT_TOOLS/dotnet
export DOTNET_CLI_HOME=$UPSTREAM_UNIT_TOOLS/cache/dotnet-cli
export NUGET_PACKAGES=$UPSTREAM_UNIT_TOOLS/cache/nuget
bash plan/tests/c2/run.sh
git diff --check
```

SDK 8.0.427/runtime 8.0.33; pinned existing Newtonsoft.Json 13.0.3. Permanent tests/project/stubs added **before source correction**: build succeeded, behavioral run exited **1**, with **64** failures across exact field count/set, contaminated `ts`, missing `privacy`, complete signing-feature input. Terminal: `FAIL favorite forms preserve fields/signing (failures=64)`. Raw log: `/workspace/work/maintenance-foundation/api-action-execution/u9-red.log`.

After only the two delimiter corrections, **unchanged assertions** returned **0 / PASS 451 assertions**. Raw log `u9-green.log` in the same scratch directory. `git diff --check` returned 0. Existing c2 parser/service/paging/single-flight controls pass on actual post-PB1 sources, not an old snapshot.

Windows command `powershell -NoProfile -ExecutionPolicy Bypass -File ./build-release-x64.ps1 -Fast -NoVersionBump -Restore` returned **127 / powershell: command not found**. Build never started; WinRT cookie/transport, installed app and folder persistence remain unverified. No live folder create/edit/delete/cleanup action, package/release or device PASS is claimed.

## Commit/handoff

Exact owned files: FavoriteAPI.cs, c2/C2Probe.csproj, c2/Program.cs, c2/C2Stubs.cs and this evidence. Commit introducing this artifact: `fix: separate favorite folder privacy form fields`. Stable source/test SHA and immediate exact remote confirmation are recorded after push in the next sequential slice/index. U9 must finish push before E1 begins.

U9 stable commit/push: `7ff548b24dc70a335c6d0d44c4ebc86a984ce96c`. `git push origin dev` returned 0; immediate local HEAD and remote refs/heads/dev both exactly matched. E1 started only afterward (`git pull --ff-only`: Already up to date). Corrected FavoriteAPI SHA-256 `1e1d56bdeb5b9716e49c6160e92a3a31bcd17362121db0a80bbd7717bb81105d`; shared ApiHelper remains `dfb07f377db95023aebce4c68101c6d334444722cf79b07d7d39a6126ba302d9`.
