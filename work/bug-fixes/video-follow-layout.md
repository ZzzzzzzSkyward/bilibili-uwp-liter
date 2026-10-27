# Video detail duplicate follow buttons

## Diagnosis

The uploader follow buttons share the same grid cell and rely on `req_user.attention` to select one state. The web detail path can publish `VideoInfo` and reveal the page before `GetAttentionUp` initializes `req_user`. When that intermediate binding path is null, compiled bindings can fail before `DisplayConvert` receives a value, leaving both buttons at their default visible state.

## Change

Initialize a missing request-user model with attention `-999` before assigning `VideoInfo` and setting `Loaded`. This gives the bindings a valid initial follow state while preserving any request-user data already returned by the API.

The follow lookup still updates signed-in users afterward: `GetAttentionUp()` queries the uploader, then writes attention `1` for followed attributes (`2` or `6`) through `VideoDetailReqUserModel.attention`, whose setter calls `DoPropertyChanged("attention")`. The page's one-way bindings therefore switch the visible button when that value changes. If the account is signed out or the lookup does not report a followed state, `-999` keeps the follow button visible.

## Source basis and confidence

The target sources are `Pages/VideoDetailPage.xaml` and `Modules/Detail/VideoDetailVM.cs`; the layout pattern is also present in the sole pinned sibling at `/workspace/biliuwp-lite` commit `ca2caddc597b3faf05b4dac6dcb226aa0694365b`. The sibling uses a dedicated attention control for the same mutually exclusive states. Confidence is based on the target's publication order, null-intermediate binding behavior, and the observed default visible state of the two colocated buttons; no runtime reproduction or test was performed.

## Files

- `src/BiliLite.UWP/Modules/Detail/VideoDetailVM.cs`

## Constraints

No build, restore, run, test, app validation, or API request was performed. Only `git diff --check` is permitted.
