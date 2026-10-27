# Comments, emotes, and danmaku API migration progress

Current step: comments and emote read migration implemented; ready for review and commit authorization.

Reference source: `/workspace/biliuwp-lite` at `ca2caddc597b3faf05b4dac6dcb226aa0694365b`.

Reference paths and symbols:
- `src/BiliLite.UWP/Models/Requests/Api/CommentApi.cs`: `Comment`, `CommentV2`, and `Reply`.
- `src/BiliLite.UWP/Models/Requests/Api/EmoteApi.cs`: `UserEmote`.
- `src/BiliLite.UWP/Controls/CommentControl.xaml.cs`: cursor pagination with `CommentV2` and legacy `Comment` fallback.
- `src/BiliLite.UWP/ViewModels/EmoteViewModel.cs`: `GetEmote` response parsing, unchanged in target because its current response handling already matches the reference.
- `src/BiliLite.UWP/Modules/Player/PlayerVM.cs`: `GetDanmaku`, owned by playback worker and not edited here.

Target changes:
- `src/BiliLite.UWP/Api/CommentApi.cs`: migrated `Comment` and `Reply` read request contracts and added WBI-signed cursor-based `CommentV2`; post, like, and delete actions are unchanged.
- `src/BiliLite.UWP/Controls/CommentControl.xaml.cs`: reads main comments through `CommentV2`, passes the cursor offset, clears the cursor on a fresh page-one load, and falls back to `Comment` when the request fails or returns a negative API code.
- `src/BiliLite.UWP/Api/EmoteApi.cs`: applies Aurora headers and cookie requirements to the signed emote panel request.
- `src/BiliLite.UWP/Modules/EmoteVM.cs`: no change; current `GetEmote` request result parsing matches reference behavior.
- Danmaku reading is not changed in this step.

Format check: `git diff --check -- src/BiliLite.UWP/Api/CommentApi.cs src/BiliLite.UWP/Api/EmoteApi.cs src/BiliLite.UWP/Controls/CommentControl.xaml.cs src/BiliLite.UWP/Modules/EmoteVM.cs` — passed.

Remaining work: wait for review/commit slot, then commit only owned paths. Playback worker owns danmaku API/callers.
