# Issue 625: UGC collection comment dispatch

Run from the repository checkout with `work/upstream-bugs/unit-tests/batch-043/issue-625/run-ugc-collection-comment-dispatch.sh`. The runner extracts and executes the actual `UserDynamicPage` open-comment handler from `src/BiliLite.UWP/Pages/Home/UserDynamicPage.xaml.cs` lines 100–148 with a recording `Utils.ShowComment` stub.

The valid fixture uses numeric IDs: target mapping sets `basic.rid_str` from the dynamic ID and separately maps `DynUgcSeason.Avid` to `major.ugc_season.aid`; `ConvertToDisplay2024` copies `basic.rid_str` to `ReplyID`; the display parser tags UGC season cards as `合集`. The current handler chooses Video comments for that tag but passes `ReplyID`.

Current assertion result:

```text
Video comment dispatch: oid=901234567890123456, type=2
FAIL: UGC collection comment used the dynamic ID instead of the mapped video AID
```

This unit proves the handler dispatches the dynamic ID for the source-supported display state. It does not execute the full mapper/converter or call the comment API; the mapped video AID expectation comes from the source DTO mapping. It does not claim an end-to-end backend response.
