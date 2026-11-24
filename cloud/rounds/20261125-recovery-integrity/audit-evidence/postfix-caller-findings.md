# Post-fix real-caller findings — CHANGES

The additional candidate pointers are now actual-source findings. Original FA-01–04 repair assessment is recorded separately; the new complete gate must wait for these caller/completion repairs. No product/test/index/ref mutation occurred. Frozen915-input manifest SHA2563cb4d3c1ca57b935a738ab882943f1086c6f280587c56d58c61ccf1667842ff7 matches root's receipt exactly.

## FA-05 / P1 — supported multi-segment FLV aliases every required segment to one file

Actual chain: the web playurl ParseJson durl branch (`BiliPayUrlRequest.cs:270–289`) preserves two ordered distinct URLs and produces Success=true/MultiFLV; PlayerVM's real GetPlayUrls forwards the result; DownloadDialog's actual primary handler177–188 creates every request with0.blv because i is never advanced. Full actual AddDownload/DownloadVideo/SaveInfo then creates two operations at the same destination and persists repeated names.

Observed with two ordinary ordered URLs:

```text
MAPPER_MULTI success=True;type=MultiFLV;urls=https://v.bilivideo.com/first,https://v.bilivideo.com/second
MULTI_DESTINATIONS operations=2;distinct_paths=1;paths=.../cid1/0.blv,.../cid1/0.blv;manifest=["0.blv","0.blv"];state=2;hide=1
```

Expected: two distinct ordered destinations and matching ordered VideoPath entries, preserving each URL/header. A single path cannot represent both intended independent segments; this is a supported FLV-format download failure, not an artificial direct-helper call. No native overwrite/file-lock outcome is asserted; the destination/metadata alias itself is the failure. All-success SingleFLV and DASH controls preserve0.blv and video.m4s/audio.m4s respectively.

**Minimal owner/scope:** original recovery executor, DownloadDialog.xaml.cs MultiFLV naming and an additive exact actual caller/mapper/helper case in the existing recovery batch/extractor. Advance the per-segment index once per appended request. Preserve source order, headers, single/DASH naming, collisions/options/schema and awaited helper preparation. No format/parser/transfer redesign or extra stage. File-based amendment and independent PASS before execution; final identical fixture must be genuinely red before green.

## FA-06 / P2 — reachable zero-media admission fabricates successful replacement evidence

The actual same mapper accepts durl=[] with one advertised quality: Success=true, MultiFLV, FlvInfo.Count0, HasPlayUrl=true. The real download handler therefore supplies an empty Urls list. Actual helper admits a new creation/generation with Required=0, writes new metadata and settles preparation. `Generation.Complete`35 accepts Succeeded0==Required0, so Blocked83–93 clears the old partial marker and the completed scan retires its cleanup recovery record.

The reproducer first establishes the old partial via actual LoadDownloading → DeleteItemAsync cancellation with a failed PermanentDelete, then executes the real dialog with the actual empty-mapper result:

```text
EMPTY_BEFORE cleanup=1;completed=0;proof=0
MAPPER_EMPTY success=True;type=MultiFLV;urls=0;has_url=True
EMPTY_AFTER state=2;hide=1;operations=0;proof=4;cleanup=0;completed=1;play_paths=0;manifest=..."VideoPath":[]...
```

The remaining old nonzero file now passes the existing file scan while new metadata has no playback paths. No actual transfer succeeded; nonetheless the dialog says added and the known cleanup target disappears. Web video request success returns this mapper result without another media-count guard (ordinary noncapped case); this is reachable from a real response path rather than an unsupported AddDownload(empty) invocation.

**Minimal owner/scope:** original recovery executor, DownloadHelper admission plus a coordinator guard if needed, and the existing real caller/helper tests. Reject an absent/empty unusable required-media set before generation/admission/file mutation; zero required media must never establish completion/supersession evidence. The existing real dialog exception path can retain the retryable failure, without a broad playurl parser change. Preserve known-partial metadata/recovery and stale-cleanup safety, unrelated valid siblings and later valid nonempty completion. Test real empty-durl mapper→dialog→helper composition after known partial; zero operations, no metadata overwrite, no successful generation evidence/cleanup retirement/completed publication. All prior nonempty generation schedules remain required. Precise amendment before implementation; no disk schema or new stage.

## FA-07 / P2 — typed playurl failure closes the retry row and reports success

In actual primary handler134–138, Success=false sets State99/ErrorMessage then continues without setting hide=false. Final216–219 emits `已添加至下载列表` and hides the dialog. With one selected failed episode there are zero operations but the caller reports success. The full actual model's ErrorMessage setter additionally emits PropertyChanged("Message") while the actual XAML binds ErrorMessage OneWay, so simply keeping the dialog open would still omit the correct bound-property change notification.

```text
TYPED_FAILURE state=99;error='typed playurl failure';hide=1;enabled=True;toasts=已添加至下载列表;notifications=State,State,Message;operations=0
```

Expected: keep the failed selection available, reenable the primary action, show failure feedback and notify ErrorMessage. Successful siblings must remain admitted/discoverable; their presence must not turn a mixed result into all-success. No rendered binding/Narrator claim follows from the recorded event name.

**Minimal owner/scope:** original recovery executor, DownloadDialog.xaml.cs typed-failure branch and DownloadEpisodeItem.ErrorMessage notification, plus existing batch's exact-handler/model assertions. Keep the dialog open for typed failure as for its existing exception path, retain State99/accurate text, avoid all-success toast, allow explicit retry without duplicating already admitted State2 siblings. Preserve all-success close/toast and zero-selection behavior. File-based amendment plus independent PASS first; no XAML redesign is needed.

## Actual-source reproducibility and bounds

Scratch `/workspace/work/recovery-integrity/fresh-audit-postfix/caller/`. Command with exact SDK8.0.427/runtime/ref8.0.33:

```bash
DOTNET_ROOT=/workspace/work/maintenance-foundation/quality/dotnet \
DOTNET_CLI_HOME=/workspace/work/recovery-integrity/fresh-audit-postfix/cli \
NUGET_PACKAGES=/workspace/work/maintenance-foundation/quality/cache/nuget \
DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 \
/workspace/work/maintenance-foundation/quality/dotnet/dotnet run \
  --project /workspace/work/recovery-integrity/fresh-audit-postfix/caller/CallerAudit.csproj -c Debug
```

Final acceptance-red: **exit1, four failing predicates / two passing SingleFLV+DASH controls**. It compiles the full actual DownloadDialog, actual playurl model file, DownloadHelper, coordinator, DownloadVM and recovery source closure. Seven uniquely extracted exact source bodies/types include the complete ParseJson and HandleUrl, real Dash/Flv DTO/conversion types and the complete one-line PlayerVM forwarding method. No caller request-list, mapper, metadata, completion/classification or failure/hide algorithm is copied into a fake.

Primitive UI fields/selection/Hide counter replace generated XAML/native objects; a passive PlayerVM delivery bridge supplies the actual mapper result and an ordinary no-subtitle response. Actual HandleUrl runs on ordinary bilivideo URLs/default nonproxy behavior, so no network is contacted. Existing recovery primitive storage/transfer stubs are copied into scratch with only missing API/setting constants, enum value and ParseArea delivery shape; additional primitives contain no business decision. Cancellation and all admitted transfers/observer callbacks are drained through a bounded explicit continuation pump; the final known-partial coordinator state is resolved by a real cleanup lease for teardown. Initial two compile attempts lacked wrapper imports/used a wrong inert hook name; their logs are retained and are not source defects. Final fixture/expected predicates were added only after those wrapper compile corrections.

All18 input hashes still match current source after the run; full source/slice manifest is source-bindings.json. SHA256:

| Input/artifact | SHA256 |
| --- | --- |
| actual DownloadDialog.xaml.cs | c49a3ab7fecd9c2ed23bf17e7de1b1f60fda3ebbb9f65ec7f70262efd708dd92 |
| actual BiliPayUrlRequest.cs | 726e435d3756f2cc394b715aaee79945ec05e9fe94d26dcab9b23ad0aa281aee |
| actual BiliPlayUrlInfo.cs | d717d1c1e09a084880fe1b3fe73c8efa597f6934926425e430ab33f87ff5d534 |
| actual PlayerVM.cs | 7e0e0653c244759539ab8b0defebf0dd00eacb9758dd652913836096fc3aa3d6 |
| actual DownloadHelper.cs | c741f289128875f477545fa1e8d912c66a402ea24a4050def01bb4f1d58ca293 |
| actual coordinator | a8516adee00ec48a3a511e8ea7b52b873d3dba77a4ec9aba83e15115da533746 |
| Program.cs | 07164dfb49bb8b41993490ad37a638efbc704802d02c142d6f84233ed77a93a8 |
| ExtraPrimitives.cs | 43c1283bcae85643f91fc9b45ce6782380506c54a57f26dd10c553c6c4cd360c |
| PrimitiveStubs.cs | a2a02521b27bf341ef960380af4f3306eca15121763a174a8f34208997d2394e |
| SourceSlices.cs | 0f3e3365a300a156df1a479dabb50768dfa97a6489b3c490ad8445eaf79348a2 |
| CallerAudit.csproj | b902d6bb56fd95f17dcdc3912f6bd6ce2cf12bf7354013792d05e050100dda65 |
| source-bindings.json | a52e41643d5ba38fdefd2dcb2fe2b6ebc5303faa841914100bab51354536584a |
| acceptance-red.log | efc3180c5874865ef19a46a8ff669ebd1b49045fa1f23af14d15e42ae23a32bf |

No live media/account request, actual user-file deletion, native UWP/BackgroundTransfer/file-lock behavior or current common-suite PASS is claimed. Original full51/52 remains pre-fix history; no redundant complete run was made during this audit.
