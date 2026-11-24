# Comment recovery evidence

Source: `dev` / `41dc562fba6e45896b0c4ba604f9ec38f4661e73`, 2026-11-24 UTC. Requirement-stage only. Product/test/main docs/history untouched by this owner; scratch only under `/workspace/work/maintenance-foundation/comment-recovery/`.

## Official lifecycle contract

Read `c0/read-docs` skill, then Firecrawl developer search for `Windows.UI.Xaml.Controls ContentDialog PrimaryButtonClick ContentDialogButtonClickEventArgs GetDeferral Cancel async UWP`. Used UWP Windows.UI.Xaml references, not Microsoft.UI.Xaml Windows App SDK references. Relevant primary-source excerpts:

- [Cancel property, requested winrt-19041](https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.contentdialogbuttonclickeventargs.cancel?view=winrt-19041): “A true value for Cancel cancels the default behavior.”
- [GetDeferral, requested winrt-19041](https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.contentdialogbuttonclickeventargs.getdeferral?view=winrt-19041): “Gets a ContentDialogButtonClickDeferral that the app can use to respond asynchronously to a button click event.” Both pages list builds 18362 and 19041 in Applies to. Scrape status 200; returned metadata canonical moniker defaults to latest, so this is documented API availability across those builds, not a newly tested SDK-specific behavior.
- [ContentDialog UWP example](https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.contentdialog?view=winrt-22621), retrieved developer-index passage: set `args.Cancel=true` for missing fields to keep open; “If you're performing async operations in the button click handler, get a deferral before you await the operation. Then, complete the deferral when the async operation is complete.”
- Revision CR-R1: [Closing, requested winrt-19041](https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.contentdialog.closing?view=winrt-19041) says “Occurs after the dialog starts to close, but before it is closed and before the Closed event occurs.” [Closed](https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.contentdialog.closed?view=winrt-19041) says “Occurs after the dialog is closed.” Both retrieved successfully (200), list builds 18362/19041, with the same latest-moniker/page-chrome limitation above. Revised contract invalidates synchronously on Closing; Closed is fallback/idempotent cleanup, not dismissal-start evidence.

Initial scrape calls omitted required `objective` and failed schema validation; corrected calls retrieved the pages. No sign-in/browser or SDK installation was performed. The page chrome mentions authorization but API descriptions/signatures/availability were returned in full. Requirements choose a synchronous cancel-and-launch adapter with an owned Task, not an async awaited event with missing deferral.

## Real-source scratch reproduction

Toolchain reused read-only from quality baseline: `/workspace/work/maintenance-foundation/quality/dotnet/dotnet`, SDK 8.0.427. Own CLI cache is under comment-recovery/cache; existing NuGet cache reused. No package download needed; the scratch references the SDK Newtonsoft.Json assembly.

Scratch project links unchanged actual:

- `Dialogs/SendCommentDialog.xaml.cs` (whole code-behind);
- `Api/CommentApi.cs` (whole builder);
- `Models/ApiModel.cs` (real legacy ApiDataModel);
- `Parsing/ApiParseResult.cs`, `Parsing/Comment/WebCommentActionParser.cs`, `Models/Api/Comment/WebCommentActionResultDto.cs` (real parser inputs).

It copies **exact unmodified class bodies** of HttpResults from `Helpers/HttpHelper.cs:499–642` and ApiModel from `Api/ApiHelper.cs:236–278` into scratch wrappers with required usings/namespaces; no method extraction/rewriting or recovery algorithm was substituted. The real HttpResults.GetData/GetJson exercises the legacy default code bug. Correction for CR-R2: existing comments fixture uses only strict JObject.Parse and omits real TryGetJObject's salvage fallback. It may test UI mechanics, but cannot be the sole raw-body compatibility evidence even after GetData is removed. Revised permanent acceptance requires actual extracted decoding plus real parser/dialog.

Stubs: UWP control/generated partial fields (TextBox/Flyout/Button/Hide/Cancel arg); EmoteVM; RestSharp enum; ApiHelper's unused host/WBI/CSRF capabilities; scripted HTTP Request delivery; toast/log recorder. These are **UI plus transport/application dependency fakes**, not a claim that only UI was fake. Real builder creates the model; fake transport controls completion, never response acceptance. No real request leaves the process.

A small SynchronizationContext tracks async-void OperationStarted/Completed and pumps queued callbacks; TaskCompletionSource holds delayed requests. Reflection invokes the real private event handlers. It does not simulate the WinRT default-close renderer; assertions inspect real Cancel value, enabled state, transport attempt count and toast/Hide calls. Forced second callback checks missing code guard, not actual OS acceptance of clicks on disabled buttons.

Command (from repository root):

```bash
DOTNET_ROOT=/workspace/work/maintenance-foundation/quality/dotnet \
DOTNET_CLI_HOME=/workspace/work/maintenance-foundation/comment-recovery/cache \
NUGET_PACKAGES=/workspace/work/maintenance-foundation/quality/cache/nuget \
DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 DOTNET_CLI_TELEMETRY_OPTOUT=1 \
/workspace/work/maintenance-foundation/quality/dotnet/dotnet run \
  --project /workspace/work/maintenance-foundation/comment-recovery/DialogRecoveryScratch.csproj -c Debug
```

Observed exit **1**, after compilation. Real HttpResults warning CS1998 (its GetJson has no await), no compilation error. Exact behavioral output:

```text
FAIL missing-code must not announce success/hide
FAIL transport failure with code zero must not succeed
FAIL rejection must re-enable retry
FAIL rejection must cancel default close
FAIL blank input cancels default close
FAIL delayed send cancels default close before await
FAIL second callback while pending cannot send again
FAIL late success after explicit cancel has no UI publication
CHECKS=10 FAILURES=8; actual dialog/builder/HttpResults/DTO source; UI+transport fakes
```

Two controls passed: rejection preserves exact draft; blank input sends no request. None of these baseline checks implements a prospective recovery solution. Current failure cases: `{}`, failed transport with `{"code":0}`, valid rejection with -400, whitespace input, delayed rejection/reentrant callback, explicit secondary cancel followed by delayed zero-code success.

The already-built binary was run once more with `--no-build` to retain raw output, again exit 1 and the same 8/10 failures. `red-baseline.log` and `source-sha256.txt` remain in scratch. The audited dialog SHA-256 is `4cc635221899d3a8e537af55306ed9f8ee15c11d723ea85dab5fa2a124c83871`; CommentApi is `f6c75e293baafeb13a21de03c85a532b95548d2961579fde9025cdf9cb19549f`. A scoped `git diff --name-only -- src plan/tests` returned no paths after reproduction.

Scratch program/project/stubs and extracted classes remain under the stream scratch root for reviewer/executor inspection. They are not repository tests or user deliverables, and should not be committed wholesale. Requirements define expanded green coverage; green was not attempted because product implementation is not authorized at this stage.

## Existing tests and limits

Read `plan/tests/comments/CommentsProbe.csproj:17–27`, Program.cs:409–441, stubs and run.sh. Baseline comments batch links real parsers/DTOs/mapper, not dialog; 99 assertions passed in preceding quality snapshot. Existing parser success without data and missing/string-code rejection checks are real tests, but do not exercise SendCommentDialog, event lifetime, missing-code legacy decode or retry state. U1/PB1 may extend this batch first; wait for release and preserve their assertions.

Read current source `SendCommentDialog.xaml:2,38–43`, `SendCommentDialog.xaml.cs:28–64`, `Models/ApiModel.cs:3–5`, `HttpHelper.cs:553–574,619–628`, `CommentControl.xaml.cs:299–318,671–674`, `Helpers/Utils.cs:35–48`. No parser code was changed to work around its known overflow; PB1 remains a dependency.

No UWP build, WinRT/XAML rendering, real Escape/Closed/default close behavior, file/device smoke, live login or live comment write was executed. Prior quality baseline already records `powershell: command not found` on Linux. Existing accepted features remain accepted under VALIDATION policy; scratch failures are source-level diagnoses, not fabricated maintainer reports or proof of observed device draft loss.

## Revision evidence: CR-R1 Closing→Closed interval

Scratch: `/workspace/work/maintenance-foundation/comment-recovery-closing-races/ClosingRaces.csproj`. Links **unchanged frozen quality-baseline copies** of real dialog, CommentApi and legacy DTO plus the original exact HttpResults/ApiModel extractions. This avoids rebuilding the original scratch binary against actively changing preceding-round product sources. Dialog hash remains `4cc635221899d3a8e537af55306ed9f8ee15c11d723ea85dab5fa2a124c83871`.

UI stub provides independently raisable Closing and Closed events; it does not retire operations or automatically close on the probe's behalf. Real private primary handler starts the send; test raises Closing, completes controlled success/rejection/fault, pumps the real continuation **before Closed**, then separately raises Closed twice. Also checks success-first ordering and fresh dialog B controls while A finishes. No sleeps, new recovery logic or product handler is substituted.

Command uses the same DOTNET_ROOT/own CLI cache environment as above; cached **Newtonsoft.Json 13.0.3/lib/net6.0** replaces the SDK assembly to match the existing batch's dependency. Run `dotnet run --project /workspace/work/maintenance-foundation/comment-recovery-closing-races/ClosingRaces.csproj -c Debug`. Exit **1**, warning CS1998 only; output:

```text
FAIL Closing->success->Closed suppresses publication/restoration
PASS repeated Closed cleanup success
FAIL Closing->rejection->Closed suppresses publication/restoration
PASS repeated Closed cleanup rejection
FAIL Closing->fault->Closed suppresses publication/restoration
PASS repeated Closed cleanup fault
PASS success-first closes/notices once
PASS fresh B controls unaffected by A late completion
CLOSING_RACE_CHECKS=8 FAILURES=3; actual baseline dialog; independent Closing/Closed stub signals
```

The five passes are controls, not evidence of implemented cleanup: old dialog has no Closing/Closed handlers, so duplicate Closed currently does nothing, and the B check only verifies its controls/draft. The three failures establish the missing retirement boundary; real platform routing/animations remain untested. Revised requirements demand broader permanent tests after U1/PB1, including no global stale notice while B is active. Raw output remains `pinned-newtonsoft.log` in scratch.

## Revision evidence: CR-R2 actual raw-body compatibility

Orchestrator decision preserves current typed-parser/HttpResults salvage; it does not authorize a strict document boundary or a shared helper change. Earlier blanket rejection of HTML/malformed input was incorrect and is withdrawn explicitly. Missing-code, noninteger-code and failed-transport protections remain required; this correction does not loosen those assertions.

Scratch: `/workspace/work/maintenance-foundation/comment-recovery-body-compatibility/RawBodies.csproj`; actual exact HttpResults extraction linked with frozen quality-baseline typed parser/DTO/ApiParseResult/legacy DTO. No HttpResults stub or copied decoding algorithm. Only logging/toast service dependencies are substituted. Decoding is called directly, then the real typed parser is called on the same response. This is a **parser/decoder compatibility experiment**, not E1 dialog green coverage or post-PB1 range verification.

Source SHA-256: full unchanged production HttpHelper `b12e457a75a133e9298fb77ad982b33986c2f95da2562b4f031b1fd692424d13`; generated wrapper `849b7d5b4fa4d087e9118d376f1e90f3048bb58465c4141c10fbd69cf5eaa9b7`; frozen action parser `e5d322818aa1563f1decbbe62cb2931666a31a72ed9255d69eaab0ad771877a5`. Revision-time active HEAD was `10dd84857c748f6638e187d891d6a6649f887bb6`; **do not relabel all frozen inputs with that SHA**. Checked current dialog/HttpHelper hashes against original evidence; both unchanged. Scoped status for those product files was empty.

Run `dotnet run --project /workspace/work/maintenance-foundation/comment-recovery-body-compatibility/RawBodies.csproj -c Debug` under its own CLI cache, same local SDK and cached pinned Newtonsoft.Json 13.0.3. Exit **0**, warning CS1998 only. Exact outcome summary:

| Fixture | TryGetJObject / selected code | Typed action outcome |
| --- | --- | --- |
| `{"code":0}` | true / 0 | Success |
| `<html>error</html>` | false | Envelope |
| `<html>{"code":0}</html>` | true / 0 | Success |
| `prefix {"code":0} suffix` | true / 0 | Success |
| `{"bad":} prefix {"code":0}` | true / 0 | Success |
| `{"bad": {"code":0}` | false | Envelope |
| `{"code":0}{"code":-400,"message":"long rejection"}` | true / -400 | Rejected -400 |
| Same two objects in reverse order | true / -400 | Rejected -400 |
| `{"code":0}{"code":1}` | true / 0 | Success (first on equal length) |
| `{"code":1}{"code":0}` | true / 1 | Rejected 1 (first on equal length) |
| `[{"code":0}]` | false | Envelope |
| `<html>{}</html>` | true / missing | Envelope |
| status=false with `<html>{"code":0}</html>` | true / 0 | **Transport**, success suppressed |
| `{"code":` | false | Envelope |
| `prefix {"code":"0"}` | true / string 0 | Envelope |

`RAW_BODY_FIXTURES=15 FAILURES=0; real extracted HttpResults + baseline typed parser`. Raw output and source manifest remain in scratch (`pinned-newtonsoft.log`, `source-sha256.txt`). Initial SDK-assembly run had the same outcomes; pinned-dependency rerun is the authoritative revision compatibility result.

Current fallback is a brace/bracket-depth scan, not a complete syntax/string-aware JSON extractor. These **exact fixtures** are proven; arbitrary malformed wrappers/quoted braces are not promised recoverable. Longest parseable top-level candidate wins; equal candidate lengths keep the first. No strict JSON-document claim or universal salvage claim follows. Permanent acceptance must compile the actual post-dependency response methods and real dialog against these fixtures rather than promote this compatibility-only pass into a UI fix.

**Re-review disposition:** CR-R1 now specifies earliest synchronous Closing retirement, independent Closing/Closed race tests and Windows routing checks; CR-R2 preserves existing body policy and requires faithful actual-source fixtures, with strict-stub limits disclosed. Only this requirements/evidence pair and stream-owned scratch changed. No product/test/review/main-doc write or implementation green claim was made.
