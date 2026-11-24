# UI/UX source audit evidence

Audit date: 2026-11-24 UTC. Repository: `/workspace/bilibili-uwp-liter`. Read-only source/document inspection; writes limited to this directory. No branch operation, commit/push, product edit, test edit or dependency installation was performed by this owner.

## Baseline commands and observed results

```text
git rev-parse HEAD
41dc562fba6e45896b0c4ba604f9ec38f4661e73

git status --short
?? cloud/rounds/20261124-maintenance-foundation/

uname -s
Linux

command -v dotnet
(no output; not on PATH)
command -v pwsh
(no output; not on PATH)
command -v powershell
(no output; not on PATH)

ls -l src/BiliLite.UWP/Controls/PlayerControl.xaml.cs \
      src/BiliLite.UWP/Modules/Home/DynamicVM.cs \
      src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs
PlayerControl.xaml.cs: present, 185090 bytes
DynamicVM.cs: present, 47753 bytes
VideoDetailPage.xaml.cs: present, 103564 bytes

git ls-files -v <the same three paths>
H src/BiliLite.UWP/Controls/PlayerControl.xaml.cs
H src/BiliLite.UWP/Modules/Home/DynamicVM.cs
H src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs
```

The round's `intake.md` independently records no promisor/sparse configuration for this checkout and source presence. This corrects applying the historical sparse-clone note as an observed current failure; no clone configuration was changed.

No build/probe command was executed by this requirements owner. Tool availability, source inspection and existing documented outcomes are not a build/runtime pass. Quality stream may supply separate SDK/probe evidence; consult its artifact before reporting aggregate results.

## Inspection commands and excerpts

Commands used `rg --files`, `rg -n` and `nl -ba <path> | sed -n '<range>p'`; the requirements artifact cites the corresponding current line numbers. No network/API request was needed.

### Existing acceptance policy

Read: `AGENTS.md`, `cloud/CONTRACT.md`, `cloud/REPO.md`, memory INDEX/decisions, round plan/intake, README, `docs/README.md`, `docs/FEATURES.md`, `docs/ROADMAP.md`, `plan/PLAN.md`, `plan/VALIDATION.md`, `plan/SETTINGS_PLAN.md`, `plan/HOT_CARD_PLAN.md`, API research, diagnostics guide, bug/review records.

```text
plan/VALIDATION.md:6–8
Policy in force: the device pass counts as passed. This file is for optional confirmation, not a gate.
A shipped item reopens only when a real bug is reported, and a new top-level bug_report.md entry starts that.

plan/VALIDATION.md:10–12
维护者 2026-11-04：本文件内的设备行一律按通过处理（默认通过）。
...下面的表格保留为说明「这条改动在设备上意味着什么」，不再是待办。
```

### U1 request construction

```text
Api/ApiHelper.cs:70
public static string GetCSRF(bool isparam = false) => Cookie.GetCSRF();

Api/CommentApi.cs:85,90
string csrf = ApiHelper.GetCSRF(true);
body = $"&oid={oid}&rpid={root}&action={action}&type={type}{csrf}",

Api/CommentApi.cs:99,104
string csrf = ApiHelper.GetCSRF(true);
body = $"&oid={oid}&root={root}&parent={parent}&type={type}&message={message}{csrf}"

Api/CommentApi.cs:111,116
string csrf = ApiHelper.GetCSRF();
body = $"&oid={oid}&rpid={rpid}&type={type}&csrf={csrf}"

Api/CommentApi.cs:123,128
string csrf = ApiHelper.GetCSRF(true);
body = $"&oid={oid}&type={(int)type}&message={Uri.EscapeDataString(message)}{csrf}"

Controls/CommentControl.xaml.cs:523,569
ReplyComment receives Uri.EscapeDataString(m.replyText) / Uri.EscapeDataString(txt).

plan/tests/comments/CommentsProbe.csproj:17–27
Compile includes ApiParseResult, ApiEnvelope, comment page/action parsers, mapper, DTOs and Common.cs;
does not include CommentApi.cs.
```

### Source-visible recovery gaps, not runtime reproductions

```text
Dialogs/SendCommentDialog.xaml.cs:28–58
Primary click is async void. Blank input toasts then returns.
No args.Cancel assignment or GetDeferral call.
IsPrimaryButtonEnabled = false before awaited request.
Nonzero code only toasts; only catch restores IsPrimaryButtonEnabled = true.

Modules/Home/RecommendVM.cs:103–121
Business/transport/exception failures toast; finally resets Loading.
Pages/Home/RecommendPage.xaml:142–188
Grid/feed/loading/manual load/refresh; no persistent initial empty/error presentation in this body.

Modules/DownloadVM.cs:659–665
Confirm “取消任务 / 确定要取消任务吗?” then cancel token and PermanentDelete folder.
Modules/DownloadVM.cs:698–711
Confirm “取消任务 / 确定要取消全部任务吗?” then cancel/delete; catch body empty.
Completed-download deletion at Pages/DownloadPage.xaml.cs:288,347 explicitly warns permanent deletion.

Pages/User/FavoriteDetailPage.xaml:85
Empty-state TextBlock has HorizontalAlignment="Left" and Margin="444,0,0,0".

Pages/HomePage.xaml:13,116,118,130
Avatar/login/download/settings buttons have no explicit AutomationProperties.Name at these declarations.
Pages/Home/RecommendPage.xaml:185–186
Refresh RoundButton has only Refresh SymbolIcon as content, no explicit tooltip/name.
Dialogs/SendCommentDialog.xaml:39–42
Face picker button has FontIcon only, no explicit tooltip/name.
Controls/RoundButton/RoundButton.cs:6–10 and RoundButton.xaml:14,67
Inherits Button; system focus visuals enabled; ContentPresenter is Raw; no custom name supplier.
```

Additional current-source reads substantiated the core journey matrix: MainPage accelerators; Home navigation/actions; Search filters/results/empty/loading; Recommend card titles; Dynamic feed failure/source/cursor handling; Video/Season error+refresh; PlayDirectory named controls/keyboard; PlayerControl key routing/sidebar presence; LiveMessage reconnect/leave; Login QR and session UI; download bulk/confirmation/local import flow; LocalPlayer subscription/disposal/index; settings ownership/diagnostic controls; DiagnosticBundle metadata/export implementation.

## Limits and next evidence

- Confirmed source defects/omissions are identified separately from UIA/layout/timing hypotheses. The audit did not measure actual focus, default accessibility names, clipping, contrast, reconnect, credential validity or memory retention.
- No live comment was posted/liked/deleted, no account login occurred, and no diagnostic package containing user data was exported or shared.
- Windows follow-up, owning existing probe batches, exact U1 checks and bounded criteria are specified in requirements sections 4–6. Do not infer any passed runtime test from these proposed checks.
