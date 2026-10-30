# Issue 358 history cursor probe

From the repository root, run:

```sh
plan/upstream-bugs/unit-tests/fix-batch-002/issue-358/run.sh
```

The runner compiles the complete `src/BiliLite.UWP/Modules/User/HistoryVM.cs` source and extracts `AccountApi.HistoryWbi` verbatim from `src/BiliLite.UWP/Api/AccountApi.cs`. It uses the provisioned .NET 8 Roslyn compiler and reference assemblies directly; it does not build or restore an application project. Newtonsoft.Json is the SDK-cached DLL. The generated source, executable, runtime config, and copied dependency are temporary runner outputs and are not committed.

A recording `ApiModel.Request` boundary queues fixture JSON and records the constructed request. The probe asserts the initial cookie GET and zero cursor, cursor advancement and append, actual DTO JSON deserialization and item mapping, archive/PGC fields, business plus top-level `kid` deletion key (separate from nested `history.oid`), empty terminal page behavior, and refresh reset.

The harness supplies boundary stubs for `ApiModel`/request dispatch, `AccountApi` response queue, `IModules`, `RelayCommand`, logging, and toast handling. The actual `HistoryVM`, its item/response DTOs, and `HistoryWbi` method execute. It does not execute UWP navigation, real `ApiModel` transport/cookie handling, the app container, or a live API call; results are limited to the recorded fixture behavior.

Observed output:

```text
PASS: initial cookie GET with zero cursor and plain query
PASS: DTO mapping, archive/PGC navigation fields and separate nested OID
PASS: deletion retains business plus top-level kid
PASS: cursor request and page append
PASS: empty terminal page hides load more
PASS: refresh resets cursor and collection
Observed request: GET https://api.bilibili.com/x/web-interface/history/cursor?max=0&view_at=0&business= (cookie enabled)
```
