#!/usr/bin/env bash
set -euo pipefail
script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd -- "$script_dir/../../../../../" && pwd)"
tools_root="${UPSTREAM_UNIT_TOOLS:-/workspace/work/upstream-unit-tools}"
source_file="$repo_root/src/BiliLite.UWP/Pages/Home/UserDynamicPage.xaml.cs"
parse_file="$repo_root/src/BiliLite.UWP/Controls/Dynamic/DynamicParse.cs"
mapper_file="$repo_root/src/BiliLite.UWP/Helpers/DynamicGrpcMapper.cs"
fragment="$script_dir/open-comment-handler.csx"
harness="$script_dir/ugc-collection-comment-dispatch-test.csx"
python3 - "$source_file" "$parse_file" "$mapper_file" "$fragment" "$script_dir/display-aid-fragment.csx" "$script_dir/mapper-aid-fragment.csx" <<'PY'
import sys
def method(source, signature):
    start = source.index(signature)
    brace = source.index('{', start)
    depth = 0
    for index in range(brace, len(source)):
        if source[index] == '{': depth += 1
        elif source[index] == '}':
            depth -= 1
            if depth == 0: return source[start:index + 1]
    raise ValueError(signature)
page = open(sys.argv[1], encoding='utf-8-sig').read()
parse = open(sys.argv[2], encoding='utf-8-sig').read()
mapper = open(sys.argv[3], encoding='utf-8-sig').read()
handler = method(page, '        private void DynamicVM_OpenCommentEvent(')
open(sys.argv[4], 'w').write('\n'.join(line.rstrip() for line in handler.splitlines()) + '\n')
display = next(line.strip() for line in parse.splitlines() if 'AID = item.modules?.module_dynamic?.major?.ugc_season?.aid.ToString()' in line)
map_line = next(line.strip() for line in mapper.splitlines() if 'aid = source.DynUgcSeason.Avid,' in line)
open(sys.argv[5], 'w').write(display + '\n')
open(sys.argv[6], 'w').write(map_line + '\n')
PY
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
sealed class SourceUgcSeason { public long Avid; }
sealed class SourceDynamic { public SourceUgcSeason DynUgcSeason; }
sealed class DynamicCardModel2024_list { public long aid; }
sealed class DynamicCardModel2024_major { public DynamicCardModel2024_list ugc_season; }
sealed class DynamicCardModel2024_dynamic { public DynamicCardModel2024_major major; }
sealed class DynamicCardModel2024_modules { public DynamicCardModel2024_dynamic module_dynamic; }
sealed class DynamicCardModel2024_basic { public string rid_str; }
sealed class DynamicCardModel2024 { public DynamicCardModel2024_basic basic; public DynamicCardModel2024_modules modules; }
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
var source = new SourceDynamic { DynUgcSeason = new SourceUgcSeason { Avid = long.Parse(videoAid) } };
var mappedSeason = new DynamicCardModel2024_list { __MAPPER_AID_ASSIGNMENT__ };
var item = new DynamicCardModel2024
{
    basic = new DynamicCardModel2024_basic { rid_str = dynamicId },
    modules = new DynamicCardModel2024_modules
    {
        module_dynamic = new DynamicCardModel2024_dynamic
        {
            major = new DynamicCardModel2024_major { ugc_season = mappedSeason }
        }
    }
};
var oneRowInfo = new Controls.Dynamic.DynamicItemDisplayOneRowInfo
{
    Tag = "合集",
    ID = dynamicId,
    __DISPLAY_AID_ASSIGNMENT__
};
var displayItem = new Controls.Dynamic.DynamicItemDisplayModel
{
    DynamicID = dynamicId,
    ReplyID = item.basic.rid_str,
    Type = Controls.Dynamic.DynamicDisplayType.MediaList,
    OneRowInfo = oneRowInfo
};
new UserDynamicPageHarness().OpenComment(displayItem);
Console.WriteLine($"Video comment dispatch: oid={Utils.LastOid}, type={Utils.LastType}");
var expectedType = (int)Api.CommentApi.CommentType.Video;
var passed = Utils.LastOid == videoAid && Utils.LastType == expectedType && displayItem.ReplyID == dynamicId;
Console.WriteLine(passed
    ? "PASS: UGC collection comment used the mapped video AID"
    : "FAIL: UGC collection comment used the dynamic ID instead of the mapped video AID");
Environment.ExitCode = passed ? 0 : 1;
CSHARP
python3 - "$harness" "$script_dir/mapper-aid-fragment.csx" "$script_dir/display-aid-fragment.csx" <<'PY'
import sys
text = open(sys.argv[1]).read()
for marker, path in zip(('__MAPPER_AID_ASSIGNMENT__', '__DISPLAY_AID_ASSIGNMENT__'), sys.argv[2:]):
    text = text.replace(marker, open(path).read().strip())
open(sys.argv[1], 'w').write(text)
PY
export DOTNET_ROOT="$tools_root/dotnet"
export DOTNET_CLI_HOME="$tools_root/cache/dotnet-cli"
export NUGET_PACKAGES="$tools_root/cache/nuget"
export XDG_CACHE_HOME="$tools_root/cache/xdg"
export XDG_DATA_HOME="$tools_root/cache/xdg-data"
exec "$DOTNET_ROOT/dotnet" "$tools_root/tool/dotnet-script-1.6.0/tools/net8.0/any/dotnet-script.dll" --no-cache "$harness"
