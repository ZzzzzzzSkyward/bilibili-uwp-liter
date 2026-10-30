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

# --- Stability report item 1: a closed media tab must release the window Share handler. ---
# MainPage.ClosePage() removes the tab without navigating, so OnNavigatedFrom never runs.
# Each media page therefore owns one SubscribeShare()/ReleaseShare() pair; the frame's
# ClosedPage handler reaches the page's close routine and that routine calls ReleaseShare().
check_media_page() {
	file="$repo/src/BiliLite.UWP/Pages/$1"
	closer="$2"
	[ -f "$file" ] || fail "source file missing: src/BiliLite.UWP/Pages/$1"

	sub_body="$(method_body 'private void SubscribeShare()' "$file")"
	[ -n "$sub_body" ] || fail "$1 has no SubscribeShare() routine"

	rel_body="$(method_body 'private void ReleaseShare()' "$file")"
	[ -n "$rel_body" ] || fail "$1 has no ReleaseShare() routine"

	# 1. SubscribeShare() both removes and adds, so a repeated subscribe cannot double-add.
	printf '%s\n' "$sub_body" | grep -Fq 'DataRequested -= DataTransferManager_DataRequested;' \
		|| fail "$1 SubscribeShare() must remove the Share handler before adding it"
	printf '%s\n' "$sub_body" | grep -Fq 'DataRequested += DataTransferManager_DataRequested;' \
		|| fail "$1 SubscribeShare() no longer adds the Share handler"

	# 2. ReleaseShare() removes the window-level subscription (idempotent: only a -=).
	printf '%s\n' "$rel_body" | grep -Fq 'DataRequested -= DataTransferManager_DataRequested;' \
		|| fail "$1 ReleaseShare() no longer removes the Share handler"

	# 3. Exactly one bare 'DataRequested +=' remains, and it is the one in SubscribeShare().
	[ "$(grep -c 'DataRequested += DataTransferManager_DataRequested;' "$file" || true)" -eq 1 ] \
		|| fail "$1 must add the Share handler exactly once, inside SubscribeShare()"
	printf '%s\n' "$sub_body" | grep -Fq 'DataRequested += DataTransferManager_DataRequested;' \
		|| fail "$1 adds the Share handler outside SubscribeShare()"

	# 4. The closed path (frame ClosedPage) reaches the page's close routine.
	closed="$(method_body "private void ${1%.xaml.cs}_ClosedPage" "$file")"
	[ -n "$closed" ] || fail "$1 has no frame ClosedPage handler"
	printf '%s\n' "$closed" | grep -Fq "$closer();" \
		|| fail "$1 ClosedPage handler never calls $closer()"

	# 5. That close routine releases Share and the ClosedPage subscription; safe twice.
	close_body="$(method_body "private void $closer()" "$file")"
	[ -n "$close_body" ] || fail "could not locate $1 $closer() body"
	printf '%s\n' "$close_body" | grep -Fq 'ReleaseShare();' \
		|| fail "$1 $closer() must call ReleaseShare() so a closed tab stops answering Share"
	printf '%s\n' "$close_body" | grep -Fq 'UnsubscribeClosedPage();' \
		|| fail "$1 $closer() must drop its ClosedPage subscription"

	# 6. Navigation departure uses the same routine, not its own inline removal.
	nav_body="$(method_body 'protected override void OnNavigatedFrom' "$file")"
	[ -n "$nav_body" ] || fail "$1 has no OnNavigatedFrom override"
	printf '%s\n' "$nav_body" | grep -Fq 'ReleaseShare();' \
		|| fail "$1 OnNavigatedFrom() must release Share through ReleaseShare()"

	n=$((n + 6))
}

check_media_page 'VideoDetailPage.xaml.cs' 'ClosePage'
check_media_page 'SeasonDetailPage.xaml.cs' 'ClosePage'
check_media_page 'LiveDetailPage.xaml.cs' 'StopPlay'

# --- Review fix (465aa55): a closed detail tab must not resume playlist continuations. ---
# MainPage.ClosePage() disposes the player but does not navigate, so a page fetch or a
# video-detail request that lands after the tab closed used to select the next queue row
# and initialize playback on a torn-down page (null dispRequest through InitializePlayInfo).
# The page now owns one latch, set first in ClosePage() and re-checked by every continuation.
detail="$repo/src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs"
[ -f "$detail" ] || fail "source file missing: src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs"

# A latch check must appear in the body before the first occurrence of an anchor.
guarded_before() {
	gbody="$1"; gend="$2"; gwhat="$3"
	eline="$(printf '%s\n' "$gbody" | grep -nF "$gend" | cut -d: -f1 | sed -n 1p || true)"
	[ -n "$eline" ] || { fail "$gwhat: '$gend' not found in the method body"; }
	printf '%s\n' "$gbody" | grep -n 'if (_isClosed)' | cut -d: -f1 | awk -v e="$eline" '
		$1 < e { found = 1 }
		END { exit found ? 0 : 1 }
	' || fail "$gwhat: no 'if (_isClosed)' guard before '$gend' (anchor line $eline)"
	n=$((n + 1))
}

# A latch check must appear between two anchors, i.e. after that await and before the change.
guarded_between() {
	gbody="$1"; gstart="$2"; gend="$3"; gwhat="$4"
	sline="$(printf '%s\n' "$gbody" | grep -nF "$gstart" | cut -d: -f1 | sed -n 1p || true)"
	eline="$(printf '%s\n' "$gbody" | grep -nF "$gend" | cut -d: -f1 | sed -n 1p || true)"
	[ -n "$sline" ] || { fail "$gwhat: '$gstart' not found in the method body"; }
	[ -n "$eline" ] || { fail "$gwhat: '$gend' not found in the method body"; }
	[ "$eline" -gt "$sline" ] \
		|| fail "$gwhat: '$gend' must follow '$gstart' (start: $sline, end: $eline)"
	printf '%s\n' "$gbody" | grep -n 'if (_isClosed)' | cut -d: -f1 | awk -v s="$sline" -v e="$eline" '
		$1 > s && $1 < e { found = 1 }
		END { exit found ? 0 : 1 }
	' || fail "$gwhat: no 'if (_isClosed)' guard after '$gstart' and before '$gend' (start: $sline, end: $eline)"
	n=$((n + 1))
}

# The first statement after a block must be the latch check: used for the initial fetch,
# where any guard earlier inside the block would otherwise satisfy a line-range search.
guarded_after_block() {
	gbody="$1"; gstart="$2"; gwhat="$3"
	printf '%s\n' "$gbody" | awk -v sig="$gstart" '
		{ sub(/\r$/, "") }
		state == 0 { if (index($0, sig)) state = 1; next }
		state == 1 {
			o = gsub(/\{/, "{"); c = gsub(/\}/, "}")
			depth += o - c
			if (depth > 0) state = 2
			next
		}
		state == 2 {
			o = gsub(/\{/, "{"); c = gsub(/\}/, "}")
			depth += o - c
			if (depth == 0) state = 3
			next
		}
		state == 3 {
			if ($0 ~ /^[[:space:]]*$/) next
			if ($0 ~ /^[[:space:]]*\/\//) next
			print
			exit
		}
	' | sed 's/^[[:space:]]*//' | grep -qx 'if (_isClosed)' \
		|| fail "$gwhat: the page fetch must be followed by 'if (_isClosed)' before selecting a row or starting playback"
	n=$((n + 1))
}

# 1. Exactly one closed latch on the page.
[ "$(grep -c '^        bool _isClosed;$' "$detail" || true)" -eq 1 ] \
	|| fail "VideoDetailPage must own exactly one closed latch field: bool _isClosed"
n=$((n + 1))

# 2. ClosePage() runs SubscribeClosedPage()/ReleaseShare()/player?.Dispose() as before.
close_body="$(method_body 'private void ClosePage()' "$detail")"
[ -n "$close_body" ] || fail "could not locate the VideoDetailPage ClosePage() body"
printf '%s\n' "$close_body" | grep -Fq 'UnsubscribeClosedPage();' \
	|| fail "VideoDetailPage ClosePage() must still drop its ClosedPage subscription"
printf '%s\n' "$close_body" | grep -Fq 'ReleaseShare();' \
	|| fail "VideoDetailPage ClosePage() must still call ReleaseShare()"
n=$((n + 1))

# 3. The latch is assigned in ClosePage().
latch_set="$(printf '%s\n' "$close_body" | grep -n '^            _isClosed = true;$' | cut -d: -f1 || true)"
[ -n "$latch_set" ] || fail "VideoDetailPage ClosePage() no longer sets _isClosed = true"
n=$((n + 1))

# 4. It is set before every teardown step of that routine, not after.
for spec in "unsubscribe:$(printf '%s\n' "$close_body" | grep -n 'UnsubscribeClosedPage();' | cut -d: -f1)" \
	"share:$(printf '%s\n' "$close_body" | grep -n 'ReleaseShare();' | cut -d: -f1)" \
	"dispose:$(printf '%s\n' "$close_body" | grep -n 'player?\.Dispose();' | cut -d: -f1)"; do
	what="${spec%%:*}"
	line="${spec##*:}"
	[ -n "$line" ] && [ "$line" -gt "$latch_set" ] \
		|| fail "ClosePage() must set the closed latch before $what (latch: $latch_set, $what: ${line:-none})"
done
n=$((n + 1))

# 5. Re-entering the page clears the latch: a stale one must not block the next visit.
nav_body="$(method_body 'protected async override void OnNavigatedTo' "$detail")"
[ -n "$nav_body" ] || fail "could not locate the VideoDetailPage OnNavigatedTo() body"
guarded_reset="$(printf '%s\n' "$nav_body" | grep -n '^            _isClosed = false;$' | cut -d: -f1 || true)"
[ -n "$guarded_reset" ] || fail "VideoDetailPage OnNavigatedTo() no longer clears the closed latch: _isClosed = false"
sub_line="$(printf '%s\n' "$nav_body" | grep -n 'SubscribeClosedPage();' | cut -d: -f1 | sed -n 1p || true)"
[ -n "$sub_line" ] && [ "$guarded_reset" -lt "$sub_line" ] \
	|| fail "VideoDetailPage OnNavigatedTo() must clear the latch before subscribing (reset: ${guarded_reset:-none}, subscribe: ${sub_line:-none})"
n=$((n + 1))

# 6. The page fetch drops a page that lands after the tab closed, for both callers.
load_body="$(method_body 'private async Task<bool> LoadMediaListPage()' "$detail")"
[ -n "$load_body" ] || fail "could not locate the LoadMediaListPage() body"
guarded_between "$load_body" 'await mediaListService.LoadMoreMediaList(mediaListState);' 'if (page == null)' 'LoadMediaListPage()'

# 7. The initial remote-playlist fetch is guarded before it selects or starts playback.
init_body="$(method_body 'private async Task InitializeMediaListPlaylist()' "$detail")"
[ -n "$init_body" ] || fail "could not locate the InitializeMediaListPlaylist() body"
guarded_after_block "$init_body" 'if (mediaListState.LoadedCount == 0 && !await LoadMediaListPage())' 'InitializeMediaListPlaylist()'

# 8. The automatic next-page continuation is guarded after its fetch, before the next row.
advance_body="$(method_body 'private async Task AdvancePlaylistAsync()' "$detail")"
[ -n "$advance_body" ] || fail "could not locate the AdvancePlaylistAsync() body"
guarded_before "$advance_body" 'var action = MediaListQueue.NextAction(' 'AdvancePlaylistAsync()'
guarded_between "$advance_body" 'if (await LoadMediaListPage())' 'continue;' 'AdvancePlaylistAsync()'

# 9. The Load more button refuses a closed page and re-checks after its own fetch.
more_body="$(method_body 'private async void MediaListLoadMore_Click' "$detail")"
[ -n "$more_body" ] || fail "could not locate the MediaListLoadMore_Click() body"
guarded_before "$more_body" 'mediaListLoadMoreButton.IsEnabled = false;' 'MediaListLoadMore_Click()'
guarded_between "$more_body" 'await LoadMediaListPage();' 'RefreshMediaListChrome();' 'MediaListLoadMore_Click()'

# 10. Row selection refuses a closed page, before the selection and before playback.
select_body="$(method_body 'private async Task SelectPlaylistRowAsync' "$detail")"
[ -n "$select_body" ] || fail "could not locate the SelectPlaylistRowAsync() body"
guarded_before "$select_body" 'listView.SelectedIndex = index;' 'SelectPlaylistRowAsync()'
guarded_before "$select_body" 'await InitializeVideo(playlist.Playlist[index].ID);' 'SelectPlaylistRowAsync()'

# 11. The UGC season continuation refuses a closed page and re-checks after its await.
season_body="$(method_body 'private async Task<bool> AdvanceUgcSeasonAsync()' "$detail")"
[ -n "$season_body" ] || fail "could not locate the AdvanceUgcSeasonAsync() body"
guarded_before "$season_body" 'listView.SelectedItem = row;' 'AdvanceUgcSeasonAsync()'
printf '%s\n' "$season_body" | grep -Fq 'return !_isClosed;' \
	|| fail "AdvanceUgcSeasonAsync() must report the closed page after its await: return !_isClosed"
recheck="$(printf '%s\n' "$season_body" | grep -n 'return !_isClosed;' | cut -d: -f1 || true)"
init_video="$(printf '%s\n' "$season_body" | grep -n 'await InitializeVideo(next);' | cut -d: -f1 || true)"
[ -n "$init_video" ] && [ "$recheck" -gt "$init_video" ] \
	|| fail "AdvanceUgcSeasonAsync() must re-check the latch after 'await InitializeVideo(next);' (await: ${init_video:-none}, re-check: ${recheck:-none})"
n=$((n + 1))

# 12. Video detail itself stops on both sides of its request.
video_body="$(method_body 'private async Task InitializeVideo' "$detail")"
[ -n "$video_body" ] || fail "could not locate the InitializeVideo() body"
guarded_before "$video_body" '_id = id;' 'InitializeVideo()'
guarded_between "$video_body" 'await videoDetailVM.LoadVideoDetail(id, is_bvid);' 'player.InitializePlayInfo(playInfos, index);' 'InitializeVideo()'

# 13. Every event that can start a continuation checks the latch first.
end_body="$(method_body 'private async void player_AllMediaEndEvent' "$detail")"
[ -n "$end_body" ] || fail "could not locate the player_AllMediaEndEvent() body"
guarded_before "$end_body" 'if (playFromUgcSeason)' 'player_AllMediaEndEvent()'

ugc_sel="$(method_body 'private async void UgcSeasonEpisode_SelectionChanged' "$detail")"
[ -n "$ugc_sel" ] || fail "could not locate the UgcSeasonEpisode_SelectionChanged() body"
guarded_before "$ugc_sel" 'await InitializeVideo(item.Aid);' 'UgcSeasonEpisode_SelectionChanged()'

queue_sel="$(method_body 'private async void ListView_SelectionChanged' "$detail")"
[ -n "$queue_sel" ] || fail "could not locate the ListView_SelectionChanged() body"
guarded_before "$queue_sel" 'await InitializeVideo(item.ID);' 'ListView_SelectionChanged()'

printf 'PASS %d assertions\n' "$n"