# Review origin/dev 09ee4801aa06a2332ed2da0338217f25296be4b9

Reviewed all new source changes since `492714ddbd3db39c7518f09252c1596b688ce524`, with Luna reviews of dynamic cards and live-room loading. Static source inspection only; no tests, builds, linters, application execution, or runtime validation. The feed-window epoch fix addresses the stale-checkpoint finding.

## Live-room parse-failure guards still dereference null in their error branches

Location: `src/BiliLite.UWP/Modules/Detail/LiveRoomVM.cs:740` and `:765`; same pattern at `:812`/`:818`, `:856`/`:879`, and `:908`/`:915`.

Trigger: a successful HTTP response fails deserialization, so `GetData<T>()` returns null (`Helpers/HttpHelper.cs:619-628`). For example, `HasParsedData(data, "anchor")` logs the failure and returns false, but its `else` then evaluates `data.message`. The SuperChat, wallet, and nested bag-list branches do the same. They still throw the null-reference exception that the new guard was meant to avoid, and their local catches show `ex.Message` to the user. The outer loader's new stage catch does not receive those locally caught exceptions.

Suggested fix: separate null handling from a parsed API error: return/skip the optional step immediately when `HasParsedData` is false, then check `data.success` and read `data.message` only on a non-null model. Apply this to each listed helper.

BR-53: this leaves a concrete source path to the reported null-reference toast, despite the new diagnostics and guard. It does not establish which response caused the maintainer's actual report; the new per-step parse log can identify that response.

## Legacy unknown cards still lack the web command required by the new fallback

Location: `src/BiliLite.UWP/Controls/Dynamic/DisplayModel.cs:682`; missing assignment in `src/BiliLite.UWP/Modules/User/DynamicVM.cs:1956-1965`.

Trigger: REST feed/detail returns a type that `ConvertToDisplay()` maps to `Other` without throwing. That branch replaces the initially populated model with a fresh one containing `DetailByIdCommand` but no `WebCommand`. The new `CanOpenOriginal` predicate requires `WebCommand`, and both the fallback button and shared header's web action use it. Thus this ordinary legacy unsupported card has no working web escape; the fix only supplies the command for the modern unsupported branch and the legacy exception branch. This also affects the REST fallback reached when native detail cannot render a modern unknown type.

Suggested fix: assign `WebCommand = WebCommand` in the ordinary legacy `Other` return too, or preserve the initialized model's command fields when changing it into a placeholder. Keep the new web route and ID check.
