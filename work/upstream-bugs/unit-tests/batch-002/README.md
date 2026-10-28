# Isolated `RefreshCookie` dispatch unit

Run from the repository checkout with:

```sh
work/upstream-bugs/unit-tests/batch-002/run-refresh-cookie-dispatch.sh
```

The runner reads the checkout's `src/BiliLite.UWP/Api/ApiHelper.cs`, extracts lines 493–502 verbatim into the adjacent fragment file, then executes that source fragment with `dotnet-script`. The extraction was checked with `diff` against the source line range. The runner expects the provisioned .NET 8 runtime/SDK and `dotnet-script` package under `/workspace/work/upstream-unit-tools`; set `UPSTREAM_UNIT_TOOLS` to that tool directory if it is elsewhere. All SDK, NuGet, and dotnet-script caches are directed under that tool directory.

The fixture seeds `api` as the earlier correspond-path GET and supplies an `ApiModel.Request` stub that only records method and URL. It makes no HTTP requests. The extracted tail creates `api3` as a POST to `https://passport.bilibili.com/x/passport-login/web/confirm/refresh`, then calls `api.Request()` instead. The assertion requires a POST to the confirm-refresh URL.

Expected current result: the unit assertion **fails** until the wrong callee is corrected. The failure confirms only that the extracted tail dispatches the previous GET instead of the confirmation POST; it does not reproduce or establish the reported 480 symptom, nor does it test end-to-end HTTP behavior.

```text
Observed dispatch: Get https://www.bilibili.com/correspond/1/fixture-path
FAIL: confirm-refresh POST was not dispatched by the extracted RefreshCookie source
```

The test does not execute all of `RefreshCookie`, use live account data, or exercise Bilibili APIs. The sibling implementation corroborates the two-step contract: refresh followed by confirm-refresh.

Runtime setup used .NET runtime 8.0.33 and SDK 8.0.427, with `dotnet-script` 1.6.0. The only dependency restore was for dotnet-script's generated script dependency project. No application/project/solution build or app restore was run.
