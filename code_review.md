# Review — `6ae19816fbeda3d1e14ee01364c772799a665828`

Read the current `bug_report.md` and reviewed all source changes since completed review `d68fe0a` (source boundary `d4c63459af705eb26a063f4659cd23074960d776`), using GPT-6-luna reviewers. Source inspection only; no tests, builds, application execution, package inspection, or other validation.

## Space requests report the wrong client timezone

`src/BiliLite.UWP/Services/GrpcService.cs:112-118` initializes DynSpaceReq without LocalTime. Its protobuf default is 0, so this new request reports UTC, while GetDynAll and GetDynVideo in the same service explicitly report 8. The checked-in DynSpaceReq contract documents local_time as the client timezone and explicitly gives 8 for East Asia (`src/BiliLite.gRPC/bilibili/app/dynamic/v2/dynamic.proto:2050-2051`).

This is visible data, not just request bookkeeping: the mapper copies the server's PtimeLabelText into module_author.pub_time (`Helpers/DynamicGrpcMapper.cs:606`), and ConvertToDisplay2024 prefers that supplied label over locally computed time (`Modules/User/DynamicVM.cs:1382-1384,1424-1426`). For the app's existing East Asia timezone convention, the space tab therefore requests labels with a different day boundary from the main feed, which can mislabel dates around midnight. The server's exact formatting behavior was not exercised here.

Set LocalTime to the same intended timezone as the other dynamic requests (currently 8). This needs one request-field assignment, not a broader timezone refactor.

Both previous detail findings are addressed: Draw comment targets now use DynDraw.Id, and Other/conversion-failure placeholders fall through to REST detail. No worthwhile issue found in the danmaku JSON import/export and blocked-user setting-key correction, or in the space source-switch/reset logic.

The bug report still describes the earlier packaging/device round; its reported verification was not rerun here.
