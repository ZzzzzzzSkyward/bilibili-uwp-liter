# Review log — cached reviewer outcomes

Each row is a reviewer commit on `origin/dev`, what it found, and what happened. Read this before
re-opening an area: it records findings that are already fixed so they are not re-litigated, and
findings that were deliberately not fixed so they are not silently dropped. Rows older than the ones
below were removed on purpose — they are all fixed and `git log` has them.

| Review commit | Reviewed head | Finding | Disposition |
| --- | --- | --- | --- |
| `924d1ab` | `4db379c` | `ResumeInterruptedAdvanceAsync` replays a saved step against the *current* continuation source, so switching source between the failure and the retry advances the wrong list | Open — `bug_report.md` BR-32. |
| `4bc8cb9` | `d905726` | The widened `自动连播` label promised automatic continuation while the outer source still advanced regardless of `AUTO_NEXT` | Fixed in `1dd359c`: `PlayContinuationPolicy.AdvanceAllowed(manual, AUTO_NEXT)` gates the automatic path, manual next/previous stay ungated, `plan/tests/ux1c` pins both. |
| `edfd08c`, `08f64dd`, `78f8950`, `7cb4ba6` | — | Empty review markers (no findings) | No action. |

## Standing review notes

- **The external reviewer is off duty as of 2026-11-01**; no further review commits are expected unless
  the maintainer asks for one. Rows above stay as the record of what was already found and fixed.
- Reviews were static source inspection only: no build, no launch, no API call. A finding marked here
  as fixed means "fixed in source", not "verified on device".
- The reviewer re-reads earlier findings every round, so an open row must stay accurate; close it in
  the same commit that fixes it.
- Known reviewer-blind spots (state them when a batch relies on them): UWP XAML/visual layout, server
  behaviour, timing on a real device, and anything that needs an account.
