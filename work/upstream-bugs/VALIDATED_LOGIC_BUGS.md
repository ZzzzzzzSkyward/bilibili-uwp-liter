# Confirmed logic defect

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
