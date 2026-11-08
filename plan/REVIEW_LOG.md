# Review log — cached reviewer outcomes

Each row is a reviewer commit on `origin/dev`, what it found, and what happened. Read this before
re-opening an area: it records findings that are already fixed so they are not re-litigated, and
findings that were deliberately not fixed so they are not silently dropped. Rows older than the ones
below were removed on purpose — they are all fixed and `git log` has them.

| Review commit | Reviewed head | Finding | Disposition |
| --- | --- | --- | --- |
| `c4aa630` | `59e8175` | A pending 30 s history report makes the pause and exit checkpoints return at `PlayerControl.xaml.cs:3390` before the local position is read or written, so a seek followed by pause or close can keep the pre-seek tick | Accepted 2026-11-08, queued as `plan/PLAN.md` row 11; not fixed yet — the local write has to be split from the report gate, and this round is compile-only per the maintainer |
| `ac6867c` | `33debce` | Text reservation posts rendered the reservation twice: `DynamicCardProjection.Apply` projects `additional.reserve` into `Attachments` before `ParseOneRowInfo2024` promotes a text card to `DynamicDisplayType.Reserve`, so the standalone template and the shared attachment panel both draw the same panel | Fixed in `95934db`: `DynamicCardProjection.ApplyAttachmentOwnership` runs immediately after the parse and drops only the `Reserve` summary when the type is `Reserve`; vote/link/unsupported stay and photo posts keep their panel. `g5` 316 → 332, with the conversion anchor failing when the call is removed |
| `dae62b7` | `284a714` | Local playback lost its episode picker: `35f6905` deleted the shared list population and `EpisodeList`/`BottomBtnList`, but `LocalPlayerPage` (the download/local-collection caller) never subscribed `DirectoryRequested`/`DirectoryRowRequested` and never reported `UpdateDirectoryInfo`, so its directory button stayed collapsed | Fixed in `9c8920f`: the page builds `PlayDirectoryBuilder.FromParts` rows from its supplied `PlayInfos`, reports availability above one item, opens/refreshes the drawer, switches Parts rows through `ChangePlayIndex(index, PlayTargetStart.Resume)` and refreshes from `ChangeEpisodeEvent`; `plan/tests/localdir` 50 assertions, 28 of 32 page anchors red on the pre-change body |
| `924d1ab` | `4db379c` | `ResumeInterruptedAdvanceAsync` replays a saved step against the *current* continuation source, so switching source between the failure and the retry advances the wrong list | Fixed in `d0d6bd9` (`bug_report.md` BR-32): the record pins its row key, `DecideResume` drops it when identity/source/key changed, and a source switch invalidates it at the switch on the same generation that rejects page responses. `plan/tests/uxfix` executes the 9-cell table and the reviewer's scenario. |
| `4bc8cb9` | `d905726` | The widened `自动连播` label promised automatic continuation while the outer source still advanced regardless of `AUTO_NEXT` | Fixed in `1dd359c`: `PlayContinuationPolicy.AdvanceAllowed(manual, AUTO_NEXT)` gates the automatic path, manual next/previous stay ungated, `plan/tests/ux1c` pins both. |
| `edfd08c`, `08f64dd`, `78f8950`, `7cb4ba6` | — | Empty review markers (no findings) | No action. |
| `b7fb5bc` | `0839615` | No findings: the pass emptied `code_review.md` (106 lines) and touched nothing else — no code, no `bug_report.md` entry, no probe | No action. The four P4-season findings and the BR-44/BR-45 dispositions it read were closed in `0839615`; that commit's `code_review.md` is the evidence now that the working file is empty. |
| `37a39f4` | `2fdcb8a` | Reloading multiple evicted pages reverses their order: `DynamicFeedWindow` handed out `_evicted[0]` for peek and take while every reloaded page is inserted at the front, so two 重新加载 clicks produced `[2, 1, 3, …]` | Fixed in `4fb7cf4`: peek and take consume the end of the list (the page adjacent to the window), so reloads restore 1,2,3,… in order and a failed request still keeps its checkpoint. `g3` 342 → 351; putting `_evicted[0]` back fails the "page next to the window" assertion. The same review's BR-21 note is not a finding — it restates that evicted card models are dropped and repost depth is bounded, which is what `65fd423` does |
| `89ece7a` | `456d920` | Player session guard ordering: `PlayVideoUseSYEngine` (both branches) and three other native paths wrote the shared `_playerVideo` before checking the ticket, so a superseded open could still replace the selected video's source | Fixed in `340a031`: the player and its source stay in locals, the ticket is checked after every await and before any shared assignment, publishing waits for `Accept()`, and an abandoned player is released exactly once by the session. `plan/tests/playersess` (69 -> 95 checks) adds the reviewer's resume-after-replacement scenario plus order checks that fail 4 ways against the previous body. |
| `ad1bb39` | `094cdb0` | Empty review marker (no findings) | No action. |
| `8d496eb` | `09ee480` | The live-room parse guards still dereferenced null in their error branches (`data.message` on a failed model), and the ordinary legacy unsupported card had no `WebCommand`, so it had no web escape | Fixed in `2276f28`; `g5` pins both, BR-53 keeps its open status until the device log names the failing step. |

## Standing review notes

- **The external reviewer is no longer a scheduled watcher**; passes land when the maintainer runs one
  (`b7fb5bc` on its own, `ac6867c` 2026-11-07, `c4aa630` 2026-11-08). Rows above stay as the record of what was
  found and what happened to it.
- Reviews were static source inspection only: no build, no launch, no API call. A finding marked here
  as fixed means "fixed in source", not "verified on device".
- The reviewer re-reads earlier findings every round, so an open row must stay accurate; close it in
  the same commit that fixes it.
- Known reviewer-blind spots (state them when a batch relies on them): UWP XAML/visual layout, server
  behaviour, timing on a real device, and anything that needs an account.
