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
    public void Apply(CommentResult m, bool legacyFallback)
    {
        var isCursorApiResponse = true;
        if (legacyFallback)
        {
            isCursorApiResponse = false;
        }
                                    cursor=m.data.cursor;
                            if (m.data.replies.Count >= 10 && (!isCursorApiResponse || cursor?.PaginationReply?.NextOffset != null))
                            {
                                btn_LoadMore.Visibility = Visibility.Visible;
                            }
    }
}
static void Check(string label, string nextOffset, bool legacyFallback, bool expectedVisible)
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
    harness.Apply(page, legacyFallback);
    var visible = harness.btn_LoadMore.Visibility == Visibility.Visible;
    Console.WriteLine($"{label}: load-more visible={visible}, nextOffset={harness.cursor?.PaginationReply?.NextOffset ?? "<null>"}");
    if (visible != expectedVisible) Environment.ExitCode = 1;
}
Check("modern terminal ten-reply page", null, false, false);
Check("modern nonterminal ten-reply page", "next-page-offset", false, true);
Check("legacy ten-reply page without cursor", null, true, true);
if (Environment.ExitCode == 0) Console.WriteLine("PASS: modern terminal cursor ends pagination; modern and legacy nonterminal pages retain load-more");
