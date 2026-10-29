# Actionable logic findings

Assessment complete: 15 retained findings in oldest-first order. Six have direct isolated unit evidence; nine are source-only. The findings and evidence below record assessment against the then-current target source. Fixes implemented afterward are tracked in [UPSTREAM_BUG_FIX_PROGRESS.md](../../UPSTREAM_BUG_FIX_PROGRESS.md). Unit probes use stubbed boundaries and establish only their stated local behavior.

| Issue | Retained defect | Evidence | Detail |
|---|---|---|---|
| [#31](https://github.com/ywmoyue/biliuwp-lite/issues/31) | Cookie refresh dispatches the earlier GET instead of the confirm POST. | Extracted-source unit | Below; [unit](unit-tests/batch-002/README.md) |
| [#105](https://github.com/ywmoyue/biliuwp-lite/issues/105) | Player disposal waits for history reporting to finish. | Extracted-method unit | [Batch 008](batches/008.md) |
| [#121](https://github.com/ywmoyue/biliuwp-lite/issues/121) | Player pointer handling swallows middle/side-button back navigation. | Source | [Batch 009](batches/009.md) |
| [#146](https://github.com/ywmoyue/biliuwp-lite/issues/146) | Arrow keys on the focused episode list change parts instead of seeking. | Source | [Batch 011](batches/011.md) |
| [#275](https://github.com/ywmoyue/biliuwp-lite/issues/275) | Pinned comments supplied through `top_replies` are ignored. | Source | [Batch 020](batches/020.md) |
| [#358](https://github.com/ywmoyue/biliuwp-lite/issues/358) | History uses the endpoint identified as closed in the sibling discussion. | Source | [Batch 026](batches/026.md) |
| [#378](https://github.com/ywmoyue/biliuwp-lite/issues/378) | Invalid anchor URLs reach the `NavigateUri` binding. | Source | [Batch 027](batches/027.md) |
| [#404](https://github.com/ywmoyue/biliuwp-lite/issues/404) | Live-home loading indexes an empty `area_entrance_v2` collection. | Source and reported stack | [Batch 028](batches/028.md) |
| [#598](https://github.com/ywmoyue/biliuwp-lite/issues/598) | Natural playback completion dispatches no final history report. | Extracted-handler unit | [Batch 041](batches/041.md) |
| [#611](https://github.com/ywmoyue/biliuwp-lite/issues/611) | A persisted invalid regex aborts a danmaku render pass. | Extracted-source unit | [Batch 042](batches/042.md) |
| [#625](https://github.com/ywmoyue/biliuwp-lite/issues/625) | Collection-card comments pass a dynamic ID as a video OID. | Extracted-handler unit | [Batch 043](batches/043.md) |
| [#1216](https://github.com/ywmoyue/biliuwp-lite/issues/1216) | A terminal comment page can leave load-more enabled and repeat the initial request. | Source only | [Batch 079](batches/079.md) |
| [#1243](https://github.com/ywmoyue/biliuwp-lite/issues/1243) | An empty note-card URL throws at `First()`. | Extracted-prefix unit and reported stack | [Batch 081](batches/081.md) |
| [#1305](https://github.com/ywmoyue/biliuwp-lite/issues/1305) | Closing a video opened without playback submits zero-progress history. | Source | [Batch 084](batches/084.md) |
| [#1311](https://github.com/ywmoyue/biliuwp-lite/issues/1311) | AI-subtitle auto-selection relies on title labels the provider removed. | Source and maintainer confirmation | [Batch 085](batches/085.md) |

The #625 unit covers the handler argument, not the full mapper or a proposed fix. The #1243 unit covers the failing prefix, not the full navigation flow. #1216 has no direct source-unit result in this handoff.

## #31 — RefreshCookie dispatches the wrong request

The target builds a confirm-refresh POST request in `api3`, but awaits `api.Request()` on the earlier correspond-path GET. The extracted target fragment was exercised with a recording stub, which confirms the wrong dispatch without making an HTTP request. This establishes a target source logic defect; it does **not** establish that the defect caused the historical 480p playback symptom. The adjacent `async void` refresh call also is not awaited before the play-URL request, but the isolated unit did not prove that race or its symptom impact.

The defect is in the current assessed target source at `src/BiliLite.UWP/Api/ApiHelper.cs:493-502`. No application fix has been made.

Recorded command and result:

```sh
work/upstream-bugs/unit-tests/batch-002/run-refresh-cookie-dispatch.sh
```

```text
Observed dispatch: Get https://www.bilibili.com/correspond/1/fixture-path
FAIL: confirm-refresh POST was not dispatched by the extracted RefreshCookie source
```

The failed assertion is the expected result for the defect: the probe expects a POST to `/x/passport-login/web/confirm/refresh` and sees the previous GET instead. The stub records requests only; there is no live HTTP, Bilibili API, app run, or end-to-end playback check.
