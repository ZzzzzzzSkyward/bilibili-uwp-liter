# Residual fresh audit — CHANGES / RELEASED

Independent auditor `memory_fresh_audit`, gpt-6.1-sol/xhigh, 2026-11-25. Reviewed final Control SHA256 `284be647a5f633b4a39532d26b4a00856bab20eb901963ebafd545a8d52367c0` against residual baseline8682 and the accepted PF1/FA1/FA2/FA3 amendment. HEAD7356b948 and all27 bound source/fixture/read-only dependency inputs remain exact before/after. Original `final-audit.md` and its unsafe discovery proofs are preserved as historical evidence.

**The four amended findings are closed at source level. One new P1 regression, RF1, requires a narrow correction before source acceptance.** No P0/P2 finding or additional scope expansion.

## RF1/P1 — enabled update-danmaku before first item throws outside async-void protection

Actual `CurrentPlayItem` is null after the real constructor until detail initialization supplies an item. Video/Season XAML displays the player while its async detail request is pending; it does not disable/collapse the player settings. More opens the settings with a null-safe item check; the persistent Danmaku tab's enabled Update button invokes actual `DanmuSettingUpdateDanmaku_Click:4070–4073` → `SetDanmaku(true)`.

`IsPlaybackWorkCurrent(null,currentGeneration)` returns true in this constructor state. The new precheck at `SetDanmaku:2433` dereferences `loadingItem.LocalPlayInfo` before its try. The task fault therefore escapes the actual async-void click handler as NullReferenceException. Baseline8682 contains the same empty-item click failure within its existing try/catch; final284be introduces the unhandled boundary regression. The ordinary source route requires only opening settings before initial detail completes or after initial load failure, not arbitrary concurrent node calls or a speculative native event.

The **same intended-safe private fixture** compiles complete actual SetDanmaku/consumers and the verbatim click handler on both source roots. Actual captured subprocess exits: old8682 **0**, final284be **1**. Current-item positive refresh issues two requests without exception; terminal click issues zero requests without exception. Constructor-empty makes zero requests but final source posts one unhandled NullReferenceException. Operation tickets are deterministically settled through the synchronized context; no sleep or process-memory assertion is used.

Required fix: reject an absent current item before dereferencing it or acquiring load work, while preserving terminal/generation checks and real current refresh. Do not create a fake product item, suppress current functionality or broadly swallow unrelated failures. Add the complete enabled-click → load boundary with constructor-empty, current data/requests and terminal controls; expected no unauthorized work or unhandled UI exception. [Exact caller/preconditions, safe logs and commands](residual-audit-evidence/empty-refresh.md).

## Completed amendment assessment

[Detailed bindings and consumer review](residual-audit-evidence/bindings-and-assessment.md) records the full assessment:

- **PF1:** real selector/Next eligibility composes current switch and transition reasons. Replacement restores usable current options without A's completion; stale A cannot enable/rollback B, and terminal close cannot re-enable.
- **FA1:** terminal latch precedes child identity/DataContext detachment and other close callbacks. Full real child/base transport/decode/error/finally publishes only to detached old instances; current/live/fresh replacements remain bound and usable. Actual Loading/choice-collapse preconditions do not justify an arbitrary concurrent-child redesign.
- **FA2:** node/item reset transfers cues/segments/settled/load lease, initial metadata blocks old-position polling/rendering while keeping controls progressing, missing segment1 has priority, skipped callers acquire no release finally, and obsolete token/cid/generation work cannot unlock or render a replacement. Current retry/manual/later-segment/cue and local/download controls remain meaningful. RF1 is the separate absent-item entry omission.
- **FA3:** ended captures item/generation before effects/report and rechecks after report/error. Old suffixes cannot advance the live sequence or loop/play/index/toast against B. Current preferences/modes and original history report/save/drain/tail policy remain intact; unexpected propagated report faults are contained/logged.

Independent Roslyn parsing verifies all52 old8682 and55 final complete method/property extraction spans plus the verbatim newly exercised click entry. Original HEAD Program bytes are recovered by removing only additive dispatches; original240 checks, prior39 lifetime groups, three history fixtures and five graphics groups remain meaningful. Exact owning same-fixture red-per-finding/58-green executions were reused and raw log hashes independently verified. No full/unchanged owning suite was rerun. All builds here use exclusive private snapshot projects.

The touched structure remains coherent: consumed private eligibility/detach/load-reset helpers in the existing owner, full source extraction and explicit primitive boundaries, no copied owner algorithm, private product-flag reset, new registry/dependency/project/stage or unrelated rewrite. Watch/close/replace, subtitle/danmaku, tracks/transition and history satisfy the amended ownership contracts. The initial loading/error settings journey remains unsafe until RF1 closes. No unrelated feature or UX expansion is requested.

G1 source/graphics ownership and Season inputs are unchanged and retain their scoped acceptance. Native disposal-exception R4, streams/adaptive ownership, global slot/SMTC/display/background policy, finite detached work and cache/history/diagnostic conditions remain unchanged. No Windows UWP build/restore, native XAML/decoder/WinRT/GPU/pixels, fifty watched videos or memory-slope improvement is claimed.

## Custody release

Audit read custody is fully released. Only this new report, `residual-audit-evidence/**` Markdown and `/workspace/work/video-memory/fresh-residual-audit/**` scratch were written. No source/test/tooling/main-document/index/ref/package/settings/publication action. Root may route RF1 through its precise amendment/single player writer after the other readers release. Owning/fresh re-review and the later stable common51/52/publication/native gates remain separate.
