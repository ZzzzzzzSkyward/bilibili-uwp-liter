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
        private void DynamicVM_OpenCommentEvent(object sender, Controls.Dynamic.DynamicItemDisplayModel e)
        {
           // splitView.IsPaneOpen = true;
            dynamic_id = e.DynamicID;
            pivotRight.SelectedIndex = 1;
            repostCount.Text = e.ShareCount.ToString();
            commentCount.Text = e.CommentCount.ToString();
            Api.CommentApi.CommentType commentType= Api.CommentApi.CommentType.Dynamic;
            var id = e.ReplyID;
            switch (e.Type)
            {

                case Controls.Dynamic.DynamicDisplayType.Photo:
                    commentType = Api.CommentApi.CommentType.Photo;
                    break;
                case Controls.Dynamic.DynamicDisplayType.Video:

                    commentType = Api.CommentApi.CommentType.Video;
                    break;
                case Controls.Dynamic.DynamicDisplayType.Season:
                    id = e.OneRowInfo.AID;
                    commentType = Api.CommentApi.CommentType.Video;
                    break;
                case Controls.Dynamic.DynamicDisplayType.ShortVideo:
                    commentType = Api.CommentApi.CommentType.MiniVideo;
                    break;
                case Controls.Dynamic.DynamicDisplayType.Music:
                    commentType = Api.CommentApi.CommentType.Song;
                    break;
                case Controls.Dynamic.DynamicDisplayType.Article:
                    commentType = Api.CommentApi.CommentType.Article;
                    break;
                case Controls.Dynamic.DynamicDisplayType.MediaList:
                    if (e.OneRowInfo.Tag != "收藏夹")
                    {
                        if (e.OneRowInfo.Tag == "合集")
                            id = e.OneRowInfo.AID;
                        commentType = Api.CommentApi.CommentType.Video;
                    }
                    break;
                default:
                    id = e.DynamicID;
                    break;
            }

            //comment.LoadComment(new Controls.LoadCommentInfo()
            //{
            //    commentMode = (int)commentType,
            //    commentSort = Api.CommentApi.commentSort.Hot,
            //    oid = id
            //});
            Utils.ShowComment(id, (int)commentType, Api.CommentApi.CommentSort.Hot);
        }
    public void OpenComment(Controls.Dynamic.DynamicItemDisplayModel item) => DynamicVM_OpenCommentEvent(this, item);
}

// Source-supported UGC season output: basic.rid_str is the dynamic ID, while the
// mapped `major.ugc_season.aid` retains the playable video's AID separately.
var dynamicId = "901234567890123456";
var videoAid = "987654321";
var source = new SourceDynamic { DynUgcSeason = new SourceUgcSeason { Avid = long.Parse(videoAid) } };
var mappedSeason = new DynamicCardModel2024_list { aid = source.DynUgcSeason.Avid, };
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
    AID = item.modules?.module_dynamic?.major?.ugc_season?.aid.ToString(),
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
