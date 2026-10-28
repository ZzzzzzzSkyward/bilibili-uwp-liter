#!/usr/bin/env bash
set -euo pipefail
script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd -- "$script_dir/../../../../../" && pwd)"
tools_root="${UPSTREAM_UNIT_TOOLS:-/workspace/work/upstream-unit-tools}"
source_file="$repo_root/src/BiliLite.UWP/Pages/Home/UserDynamicPage.xaml.cs"
fragment="$script_dir/open-comment-handler.csx"
harness="$script_dir/ugc-collection-comment-dispatch-test.csx"
sed -n '100,148p' "$source_file" > "$fragment"
grep -Fq 'case Controls.Dynamic.DynamicDisplayType.MediaList:' "$fragment"
grep -Fq 'Utils.ShowComment(id, (int)commentType' "$fragment"
cat > "$harness" <<'CSHARP'
using System;

static class Controls
{
    public static class Dynamic
    {
        public enum DynamicDisplayType { Dynamic, Photo, Video, Season, ShortVideo, MediaList, Music, Article }
        public sealed class DynamicItemDisplayModel
        {
            public string DynamicID;
            public string ReplyID;
            public int ShareCount;
            public int CommentCount;
            public DynamicDisplayType Type;
            public DynamicItemDisplayOneRowInfo OneRowInfo = new();
        }
        public sealed class DynamicItemDisplayOneRowInfo { public string Tag; public string ID; public string AID; }
    }
}
static class Api
{
    public static class CommentApi
    {
        public enum CommentType { Dynamic, Photo, Video, MiniVideo, Song, Article }
        public enum CommentSort { Hot }
    }
}
static class Utils
{
    public static string LastOid;
    public static int LastType;
    public static void ShowComment(string oid, int commentMode, Api.CommentApi.CommentSort sort)
    {
        LastOid = oid;
        LastType = commentMode;
    }
}
sealed class TextStub { public string Text; }
sealed class SelectionStub { public int SelectedIndex; }
sealed class UserDynamicPageHarness
{
    string dynamic_id;
    SelectionStub pivotRight = new();
    TextStub repostCount = new();
    TextStub commentCount = new();
CSHARP
cat "$fragment" >> "$harness"
cat >> "$harness" <<'CSHARP'
    public void OpenComment(Controls.Dynamic.DynamicItemDisplayModel item) => DynamicVM_OpenCommentEvent(this, item);
}

// Source-supported UGC season output: basic.rid_str is the dynamic ID, while the
// mapped `major.ugc_season.aid` retains the playable video's AID separately.
var dynamicId = "901234567890123456";
var videoAid = "987654321";
var displayItem = new Controls.Dynamic.DynamicItemDisplayModel
{
    DynamicID = dynamicId,
    ReplyID = dynamicId,
    Type = Controls.Dynamic.DynamicDisplayType.MediaList,
    OneRowInfo = new Controls.Dynamic.DynamicItemDisplayOneRowInfo { Tag = "合集", ID = dynamicId }
};
new UserDynamicPageHarness().OpenComment(displayItem);
Console.WriteLine($"Video comment dispatch: oid={Utils.LastOid}, type={Utils.LastType}");
var expectedType = (int)Api.CommentApi.CommentType.Video;
var passed = Utils.LastOid == videoAid && Utils.LastType == expectedType;
Console.WriteLine(passed
    ? "PASS: UGC collection comment used the mapped video AID"
    : "FAIL: UGC collection comment used the dynamic ID instead of the mapped video AID");
Environment.ExitCode = passed ? 0 : 1;
CSHARP
export DOTNET_ROOT="$tools_root/dotnet"
export DOTNET_CLI_HOME="$tools_root/cache/dotnet-cli"
export NUGET_PACKAGES="$tools_root/cache/nuget"
export XDG_CACHE_HOME="$tools_root/cache/xdg"
export XDG_DATA_HOME="$tools_root/cache/xdg-data"
exec "$DOTNET_ROOT/dotnet" "$tools_root/tool/dotnet-script-1.6.0/tools/net8.0/any/dotnet-script.dll" --no-cache "$harness"
