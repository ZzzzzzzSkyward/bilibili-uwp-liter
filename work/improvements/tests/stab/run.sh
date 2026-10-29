#!/bin/sh
# Stability report item 3: when the player closes, the separate audio element must be
# detached from its own MediaPlayer. The routine used to detach mediaPlayerVideo twice,
# leaving mediaPlayerAudio bound to a player that the same method then disposes.
set -eu
repo="$(cd -- "$(dirname -- "$0")/../../../.." && pwd)"
player="$repo/src/BiliLite.UWP/Controls/Player.xaml.cs"

fail() {
	printf 'FAIL %s\n' "$1" >&2
	exit 1
}

[ -f "$player" ] || fail "source file missing: src/BiliLite.UWP/Controls/Player.xaml.cs"

# Body of ClosePlay() only: from the signature to its 8-space-indented closing brace.
body="$(awk '
	{ sub(/\r$/, "") }
	/public void ClosePlay\(\)/ { inside = 1; next }
	inside && $0 == "        {" { next }
		inside && /^        \}$/ { exit }
	inside { print }
' "$player")"

[ -n "$body" ] || fail "could not locate the ClosePlay() body in Player.xaml.cs"

audio_guard="$(printf '%s\n' "$body" | grep -n 'if (mediaPlayerAudio\.MediaPlayer != null)' | cut -d: -f1 || true)"
[ -n "$audio_guard" ] || fail "ClosePlay() no longer guards the audio element: if (mediaPlayerAudio.MediaPlayer != null)"

audio_detach="$(printf '%s\n' "$body" | grep -n 'mediaPlayerAudio\.SetMediaPlayer(null);' | cut -d: -f1 || true)"

video_detach="$(printf '%s\n' "$body" | grep -n 'mediaPlayerVideo\.SetMediaPlayer(null);' | cut -d: -f1 || true)"
[ -n "$video_detach" ] || fail "ClosePlay() no longer detaches the video element: mediaPlayerVideo.SetMediaPlayer(null)"

# 1. The audio branch detaches the audio element, exactly once, inside that branch.
[ "$(printf '%s\n' "$audio_detach" | grep -c . || true)" -eq 1 ] \
	|| fail "ClosePlay() must call mediaPlayerAudio.SetMediaPlayer(null) exactly once (found $(printf '%s\n' "$audio_detach" | grep -c . || true))"
[ "$audio_detach" -gt "$audio_guard" ] \
	|| fail "mediaPlayerAudio.SetMediaPlayer(null) is not inside the audio branch (line $audio_detach vs guard $audio_guard)"
[ "$audio_detach" -le "$((audio_guard + 3))" ] \
	|| fail "mediaPlayerAudio.SetMediaPlayer(null) is not the body of the audio branch (line $audio_detach, guard $audio_guard)"

# 2. No second video detach: every mediaPlayerVideo.SetMediaPlayer(null) precedes the audio guard.
for line in $video_detach; do
	[ "$line" -lt "$audio_guard" ] \
		|| fail "a second mediaPlayerVideo.SetMediaPlayer(null) remains in the audio branch (line $line, audio guard $audio_guard)"
done

# 3. The video detach before the audio branch is still present.
first_video="$(printf '%s\n' "$video_detach" | sort -n | sed -n 1p)"
[ -n "$first_video" ] && [ "$first_video" -lt "$audio_guard" ] \
	|| fail "the video detach is no longer present before the audio branch (video lines: $(printf '%s' "$video_detach" | tr '\n' ' '), audio guard $audio_guard)"

# --- Stability report item 4: closing the web page must not race WebView2 init. ---
page="$repo/src/BiliLite.UWP/Pages/WebPage.xaml.cs"
page_xaml="$repo/src/BiliLite.UWP/Pages/WebPage.xaml"
[ -f "$page" ] || fail "source file missing: src/BiliLite.UWP/Pages/WebPage.xaml.cs"
[ -f "$page_xaml" ] || fail "source file missing: src/BiliLite.UWP/Pages/WebPage.xaml"

# Body of a method: from its signature to the next 8-space-indented closing brace.
method_body() {
	awk -v sig="$1" '
		{ sub(/\r$/, "") }
		!inside && index($0, sig) { inside = 1; next }
		inside && $0 == "        {" { next }
		inside && /^        \}$/ { exit }
		inside { print }
	' "$2"
}

closed_body="$(method_body 'private void WebPage_ClosedPage' "$page")"
[ -n "$closed_body" ] || fail "could not locate the WebPage_ClosedPage() body in WebPage.xaml.cs"
n=3

# 4. The closed marker is assigned in WebPage_ClosedPage.
marker="$(printf '%s\n' "$closed_body" | grep -n '^            _isClosed = true;$' | cut -d: -f1 || true)"
[ -n "$marker" ] || fail "WebPage_ClosedPage() no longer sets _isClosed = true"
n=$((n + 1))

# 5. Everything teardown-related is located after the marker: it is set first.
unsub="$(printf '%s\n' "$closed_body" | grep -n 'UnsubscribeClosedPage();' | cut -d: -f1 || true)"
core_guard="$(printf '%s\n' "$closed_body" | grep -n 'if (view.CoreWebView2 != null)' | cut -d: -f1 || true)"
navigate="$(printf '%s\n' "$closed_body" | grep -n 'view.NavigateToString("");' | cut -d: -f1 || true)"
close_call="$(printf '%s\n' "$closed_body" | grep -n 'view.Close();' | cut -d: -f1 || true)"
remove="$(printf '%s\n' "$closed_body" | grep -nF '(this.Content as Grid)?.Children.Remove(view);' | cut -d: -f1 || true)"
null_line="$(printf '%s\n' "$closed_body" | grep -n '^            webView = null;$' | cut -d: -f1 || true)"
for spec in "unsubscribe:$unsub" "navigate:$navigate" "close:$close_call" "remove:$remove" "null:$null_line"; do
	name=${spec%%:*}
	line=${spec#*:}
	[ -n "$line" ] && [ "$line" -gt "$marker" ] \
		|| fail "WebPage_ClosedPage() must set _isClosed before the $name step (marker $marker, $name ${line:-missing})"
done
n=$((n + 1))

# 6. A second ClosedPage delivery is a no-op (early return on the marker).
guard="$(printf '%s\n' "$closed_body" | grep -n 'if (_isClosed)' | cut -d: -f1 || true)"
[ -n "$guard" ] && [ "$guard" -lt "$marker" ] \
	|| fail "WebPage_ClosedPage() must return early when _isClosed is already set (guard: ${guard:-none}, marker: $marker)"
n=$((n + 1))

# 7. Navigation/clearing only happens behind a CoreWebView2 null check.
[ -n "$core_guard" ] && [ "$navigate" -gt "$core_guard" ] \
	|| fail "WebPage_ClosedPage() must only navigate/clear an initialised control (guard: ${core_guard:-none}, navigate: ${navigate:-none})"
n=$((n + 1))

# 8. The control is explicitly closed, after the clear and before removal.
[ "$close_call" -gt "$navigate" ] \
	|| fail "WebPage_ClosedPage() must call view.Close() after clearing (close: ${close_call:-none}, navigate: ${navigate:-none})"
n=$((n + 1))

# 9. Removal from the grid happens after Close(), and the field is nulled last.
[ "$remove" -gt "$close_call" ] \
	|| fail "WebPage_ClosedPage() must remove the control from its grid after Close() (remove: ${remove:-none}, close: ${close_call:-none})"
[ "$null_line" -gt "$remove" ] \
	|| fail "WebPage_ClosedPage() must null webView only after the control is closed and removed (null: ${null_line:-none}, remove: ${remove:-none})"
n=$((n + 1))

# 10. No forced collection was added to the close path.
if printf '%s\n' "$closed_body" | grep -q 'GC\.Collect()'; then
	fail "WebPage_ClosedPage() must not force GC.Collect(); explicit Close() releases the control"
fi
n=$((n + 1))

# 11. Each WebView2 initialisation re-checks the marker as its first post-await action.
awaits="$(grep -n 'await webView\.EnsureCoreWebView2Async();' "$page" | cut -d: -f1 || true)"
[ "$(printf '%s\n' "$awaits" | grep -c . || true)" -eq 2 ] \
	|| fail "expected exactly two awaits of EnsureCoreWebView2Async (found: $(printf '%s' "$awaits" | tr '\n' ' '))"
for line in $awaits; do
	after="$(awk -v a="$line" 'NR > a { sub(/\r$/, ""); if ($0 ~ /^[ \t]*$/) next; print; exit }' "$page")"
	case "$after" in
		*"if (_isClosed"*) ;;
		*) fail "the continuation after EnsureCoreWebView2Async at line $line must re-check _isClosed before touching webView (next line: $after)" ;;
	esac
done
n=$((n + 1))

# 12. Exactly one forced collection remains in the file, in the navigation path it always had.
[ "$(grep -c 'GC\.Collect()' "$page" || true)" -eq 1 ] \
	|| fail "WebPage.xaml.cs must keep a single GC.Collect() (the OnNavigatingFrom teardown) and gain none"
n=$((n + 1))

# 13. The control really is Microsoft.UI.Xaml.Controls.WebView2, the type that exposes Close().
grep -q 'xmlns:controls="using:Microsoft.UI.Xaml.Controls"' "$page_xaml" \
	|| fail "WebPage.xaml no longer maps controls: to Microsoft.UI.Xaml.Controls"
grep -q '<controls:WebView2' "$page_xaml" \
	|| fail "WebPage.xaml no longer hosts a controls:WebView2 control"
n=$((n + 1))

printf 'PASS %d assertions\n' "$n"