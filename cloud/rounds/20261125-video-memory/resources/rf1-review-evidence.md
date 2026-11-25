# RF1 requirements review evidence

Verdict: **PASS / RELEASED**. Private review scope: `/workspace/work/video-memory/resources-rf1-review/`. Frozen current Control `284be647a5f633b4a39532d26b4a00856bab20eb901963ebafd545a8d52367c0`; no source repair or green RF1 run exists in this review.

Read ACTIVE rf1-review-dispatch, released rf1-requirements/evidence, previous accepted resource/residual/terminal contracts, fresh empty-refresh finding/comparison and actual methods/UI callers. No audit driver or owning/full suite was rerun for requirements prose. The original audit's broad Video reachability paragraph remains historical; the released RF1 evidence qualifies it accurately rather than editing historical proof.

## Independent binding checks

Private `binding-check.json` records26 existing frozen fixture/read-only dependency comparisons, current Control and seven page/XAML/VM bindings, contract/counterexample/released input hashes, six complete source method hashes and all44 helper-name occurrences (43 consumers plus declaration). Every catalogued line was independently matched to current source. The same-fixture comparison's source/input/log hashes were independently recomputed; other owners' scratch was read-only.

Complete actual methods checked:

- Constructor388–427: creates services/timers/controls but never assigns CurrentPlayItem; property264 has no initializer.
- IsPlaybackWorkCurrent371–374: accepts terminal-false/current-generation/both-null references.
- SetDanmaku2428+: obtains the item, passes that predicate, then dereferences loadingItem.LocalPlayInfo in its new precheck outside try before requests or guarded load.
- LoadDanmaku2548+: actual current item/cid/lease acquisition/response/finally behavior remains required.
- DanmuSettingUpdateDanmaku_Click4070–4073: complete async-void handler directly awaits SetDanmaku(true).
- TopBtnMore_Click3579+: null-safe target selection opens settings; constructor does not remove DanmakuPivot.

Actual XAML Update button245 is enabled by default and wired to that handler. SeasonDetailPage11–22 supplies an always-present player ancestor; centered error/progress at365–374 do not establish whole-player input suppression. Its complete pending detail init174–195 does not initialize a player without Detail, and its initialization198+ returns for NothingPlay. This supports the absent-item route without a new page/VM/UI policy. VideoDetailPage79 instead binds the whole content to Loaded, whose VM starts false; that instantiated but hidden player alone does not prove an enabled click before first load. Actual Windows hit testing/visibility/pointer delivery was not run.

## Helper-wide compatibility

Inspected all43 actual helper consumers in timer, item replacement, node/switch/local checks, quality/local open/info, picker/import, ended and load/error/finally publication. They guard item-owned work. No current caller requires null as a valid authorization identity. A non-null requirement leaves every old truth condition for non-null input unchanged; it does not add cid/avid/manifest/address constraints or reject valid generation0 contexts. Null-owner error/finally publication becoming ineligible is intentional. Terminal and Unloaded cleanup remain separate from this predicate, preserving cleanup for an empty disposed control and background/live-history semantics.

No actual source method or helper decision was changed by this review. The helper-strengthening alternative and explicit producer entry alternative are both required to reject before all absent-item dependent work; merely hiding/catching the click does not satisfy the contract.

## Reused same-fixture safe evidence

Authoritative `/workspace/work/video-memory/fresh-residual-audit/comparison.json` and actual raw logs match:

| Side | Current | Constructor-empty | Terminal | Recorded exit |
| --- | --- | --- | --- | --- |
| Old8682 |2 requests,0 unhandled |0 requests,0 unhandled |0 requests,0 unhandled |0 |
| Frozen284be |2 requests,0 unhandled |0 requests,1 NullReferenceException |0 requests,0 unhandled |1 |

Log hashes: old8682 `884426873e4f8cccf00cdfbf806bbbebe84dad16ebb18a214897933335ffac2b`; final284be `c3f78d6b35f35c92aa3e0262d795e614b91b108df882f68b148053e446c1ada1`. Same Main.cs `6af3c7c796da87284519420b838115c044ac37b717e63571cc40610a3d495d6c`; verbatim actual click wrapper `095f2597148f5646e346eace1005245b514cfd4eb9665babc09e864bd48f554c`. The wrapped command records expected old0/final1 with identical fixtures; wrapper0 is not repaired success.

The default current item in that small discovery driver is synthetic fixture construction, not proof of full successful public initialization; the required final current-item control explicitly goes through real initialization/current-source fixture paths and checks accepted cues/feedback plus baseline two-call behavior. Null assignment affects only the public item field/property to represent the real default, without private terminal/generation mutation. The actual async-void handler executes under ControlledContext with operation completion, callback pumping and posted-error capture; neither a direct Task-only call nor sleep establishes the result.

The old8682 empty click was contained by its older try/catch and may publish error/finally state. Its discovery exit0 therefore proves only the bounded crash regression/control, not the stronger quiet no-owner contract. The future same final intended-safe owning assertions must fail on284be and pass on repaired actual source with zero absent-owner work/publication. Missing/ambiguous unique click extraction must reject separately; assertions may not hide posted faults or return before positive controls.

## Release and preserved limits

All recorded scoped source/fixture/page/contract/released-input hashes were rechecked before release. Only rf1-review.md, this evidence and exclusive private scratch were written; previous owning/fresh/graphics reports, source/tests/main docs/index/refs and other scratch remained unchanged. No whole-tree/API equality or integration claim is made.

The future repair needs only the same Control writer, additive existing residual probe/stub instrumentation and mandatory actual click extraction. No new project/dispatch/stage, page/VM/native/transport/graphics/history/API/tooling change is supported. Existing58/39/fivegraphics/original240 plus accepted cross-scope receipts remain prerequisites. Native UWP build/UI/cache/dispatcher/media/Win2D/pixels/working set/private commit/GPU/handles and fifty watched videos remain externally unverified. This source exception proof makes no RAM-saving or BR-21 attribution claim. Read custody is released for root's later exact repair grant.
