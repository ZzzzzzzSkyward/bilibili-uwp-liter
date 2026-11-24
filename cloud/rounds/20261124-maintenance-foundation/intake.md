# Repository intake evidence

## Branch synchronization

At intake and after user instruction to remain synchronized, ran:

```sh
git fetch origin --prune
git pull --ff-only
git branch --show-current
git rev-parse HEAD origin/dev
git ls-remote origin refs/heads/dev
```

Result: branch `dev`; all three SHA observations were `41dc562fba6e45896b0c4ba604f9ec38f4661e73`; `Already up to date.` Initial working tree was clean. The only new untracked path after planning started was this round directory.

## Architecture comprehension

- The shipped application is UWP (`src/BiliLite.UWP`), with a protobuf/gRPC library, JS login bridge, full-trust converter and MSIX packaging project. `WebApi` and `ApiTester` are legacy, unshipped tools.
- REST flow: request builder → HTTP transport → `ApiEnvelope`/feature parser → DTO mapper → generation-owning VM → XAML/control. Business rejection, malformed envelope/payload and transport failure have separate parse results.
- Existing plans already cover dynamic cards, hot cards, settings and refresh transactions. Preserve their completed acceptance and use current roadmap evidence to select new work.
- Resource/state owners already exist: `PlayerSessionState` for in-flight media, `DynamicFeedWindow` for bounded cards/checkpoints, credential committer + refresh transaction for account snapshots, settings key families for persisted options.
- Device-only issues include SMTC/background ownership, BR-21 memory profiling, window restoration, live reconnect, text scale/theme and playback history on close. Offline logic probes cannot establish those outcomes.

## Current checkout differs from historical environment notes

Observed, do not infer from the documented Windows clone:

```text
uname -s -> Linux
command -v dotnet / powershell / pwsh -> no executable on PATH
git config --get remote.origin.promisor -> false
git config --get core.sparseCheckout -> unset
```

`test -f` succeeded for `Controls/PlayerControl.xaml.cs`, `Modules/Home/DynamicVM.cs`, `Pages/VideoDetailPage.xaml.cs`, and `plan/tests/g3/Program.cs` under their documented roots. Thus those files are present here; the old partial-clone blocker must not be repeated as an observed failure without checking. No sparse settings or excluded blob retrieval were changed. Requirements owners should verify every needed source independently.

## Confirmed source example for priority selection

`Api/ApiHelper.cs:70` returns the raw cookie CSRF token regardless of its boolean argument. `Api/CommentApi.cs:85–128` appends `GetCSRF(true)` directly to the preceding `type` or `message` value for Like, ReplyComment and AddComment. DeleteComment already uses an explicit `csrf=` key. The existing `comments` probe compiles parsers/DTOs, but its project does not link the request builder. The requirements/reviewer must set the exact behavior and regression coverage before execution; do not change the shared helper globally as a shortcut.
