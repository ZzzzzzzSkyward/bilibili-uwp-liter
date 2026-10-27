# User profile loading fix

## Source finding

The target profile page called `UserDetailVM.GetUserInfo`, which loaded the WBI account-info API and then made a second `/x/v2/space` request for counts. Its `UserInfoWbi` fallback was an alias of `UserInfo`, so all retries repeated the same endpoint. In the pinned sibling, the profile VM loads `/x/v2/space` once and maps the response's `card`, `images`, `live`, and count fields into the view model. The target already exposes the same `Space(mid)` request API.

## Change

`Modules/User/UserDetail/UserDetailVM.cs` now uses the sibling's `/x/v2/space` profile flow and maps its response onto the existing `UserCenterInfoModel`, preserving current page bindings and the followed state from `card.relation.is_follow`. It uses that response for profile and stats together, removing the duplicate request and no-op retry. Failure logs now identify request, response, or mapping stage and include response code/message when available; exceptions retain their stack trace.

## Constraints

No API request, build, restore, app run, test, or runtime/correctness validation was performed. The permitted `git diff --check` formatting check is the only check for this change. The orchestrator owns the shared `LOGGING_PROGRESS.md` and Git staging/commit schedule.
