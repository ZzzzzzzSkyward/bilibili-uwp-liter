# Review origin/work cdfc3ba691835b80efe93d9ddd61372976ffc456

Read the current `bug_report.md` and compared the REST-parser extraction with its callers. One worthwhile incomplete failure-handling issue; source inspection only, with no tests, builds, or application execution.

## Card-deserialization failures cannot update the selector's outcome

`src/BiliLite.UWP/Modules/User/DynamicVM.cs:1130-1139` records `Success` before `TryDeserialize`, then tries to record `MalformedResponse` if a card cannot be deserialized. `DynamicApiSelector.Record` ignores a second record with the same attempt ID (`Services/DynamicApiSelector.cs:653`), so that failure record never changes the saved success. The space leg has the same ordering; Home records success first and returns malformed without a failure record (`Modules/Home/DynamicVM.cs:360-366`).

Trigger: a valid `code:0` envelope and `cards` array whose elements cannot deserialize to the caller's card model. The current request now returns a failure into the fallback chain, but the persisted selector still learns that Legacy succeeded, clears its failure state, and can prefer the unusable source again on a later space refresh.

The success-before-deserialization ordering predates this extraction; the new failure-return handling leaves that classification problem unresolved. This is an incomplete handling path, rather than a newly introduced scoring regression.

Suggested fix: deserialize first, then record exactly one outcome for the attempt. Record `MalformedResponse` once on failure in all three legs, and record success only after the page is usable.
