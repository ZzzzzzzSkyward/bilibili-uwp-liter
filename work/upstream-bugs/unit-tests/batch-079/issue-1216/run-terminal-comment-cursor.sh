#!/usr/bin/env bash
set -euo pipefail
script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd -- "$script_dir/../../../../../" && pwd)"
tools_root="${UPSTREAM_UNIT_TOOLS:-/workspace/work/upstream-unit-tools}"
source_file="$repo_root/src/BiliLite.UWP/Controls/CommentControl.xaml.cs"
fragment="$script_dir/next-cursor-load-more.csx"
harness="$script_dir/terminal-comment-cursor-test.csx"
python3 - "$source_file" "$fragment" <<'PY'
import sys
source = open(sys.argv[1], encoding='utf-8-sig').read()
start = source.index('                            cursor=m.data.cursor;')
end = source.index('\n                            }', source.index('if (m.data.replies.Count >= 10 && cursor?.PaginationReply?.NextOffset != null)', start)) + len('\n                            }')
fragment = source[start:end]
open(sys.argv[2], 'w').write('\n'.join(line.rstrip() for line in fragment.splitlines()) + '\n')
PY
grep -Fq 'm.data.replies.Count >= 10 && cursor?.PaginationReply?.NextOffset != null' "$fragment"
cat > "$harness" <<'CSHARP'
using System;
using System.Collections.Generic;

enum Visibility { Collapsed, Visible }
sealed class CommentPagination { public string NextOffset; }
sealed class CommentCursor { public CommentPagination PaginationReply; }
sealed class CommentData { public List<int> replies; public CommentCursor cursor; }
sealed class CommentResult { public CommentData data; }
sealed class Button { public Visibility Visibility; }
sealed class Harness
{
    public CommentCursor cursor;
    public Button btn_LoadMore = new() { Visibility = Visibility.Collapsed };
    public void Apply(CommentResult m)
    {
        __ACTUAL_CURSOR_AND_LOAD_MORE_BLOCK__
    }
}
static void Check(string label, string nextOffset, bool expectedVisible)
{
    var page = new CommentResult
    {
        data = new CommentData
        {
            replies = new List<int> { 1,2,3,4,5,6,7,8,9,10 },
            cursor = nextOffset == null ? null : new CommentCursor { PaginationReply = new CommentPagination { NextOffset = nextOffset } }
        }
    };
    var harness = new Harness();
    harness.Apply(page);
    var visible = harness.btn_LoadMore.Visibility == Visibility.Visible;
    Console.WriteLine($"{label}: load-more visible={visible}, nextOffset={harness.cursor?.PaginationReply?.NextOffset ?? "<null>"}");
    if (visible != expectedVisible) Environment.ExitCode = 1;
}
Check("terminal ten-reply page", null, false);
Check("nonterminal ten-reply page", "next-page-offset", true);
if (Environment.ExitCode == 0) Console.WriteLine("PASS: terminal cursor hides load-more; nonterminal cursor keeps next-page action");
CSHARP
python3 - "$harness" "$fragment" <<'PY'
import sys
harness = open(sys.argv[1]).read()
harness = harness.replace('__ACTUAL_CURSOR_AND_LOAD_MORE_BLOCK__', open(sys.argv[2]).read().rstrip())
open(sys.argv[1], 'w').write(harness)
PY
export DOTNET_ROOT="$tools_root/dotnet"
export DOTNET_CLI_HOME="$tools_root/cache/dotnet-cli"
export NUGET_PACKAGES="$tools_root/cache/nuget"
export XDG_CACHE_HOME="$tools_root/cache/xdg"
export XDG_DATA_HOME="$tools_root/cache/xdg-data"
exec "$DOTNET_ROOT/dotnet" "$tools_root/tool/dotnet-script-1.6.0/tools/net8.0/any/dotnet-script.dll" --no-cache "$harness"
