using System;
using System.Collections.Generic;
using BiliLite.Helpers;
using BiliLite.Modules.User.UserDetail;
using Arc = Bilibili.App.Interface.V1.Arc;
using ArchiveArc = Bilibili.App.Archive.V1.Arc;

namespace G2Probe
{
    /// <summary>
    /// Exercises SpaceArchiveGrpcMapper against representative space archive-search results.
    /// </summary>
    public static class Program
    {
        private static int _passed;

        public static int Main()
        {
            try
            {
                MapAllTenFields();
                StripKeywordHighlight();
                FormatDurations();
                HandleEmptyInputs();
                HandleNullAuthorAndStat();
                SkipNullEntries();
            }
            catch (Exception ex)
            {
                Console.WriteLine("FAIL " + ex);
                return 1;
            }

            Console.WriteLine($"PASS {_passed} assertions");
            return 0;
        }

        private static void MapAllTenFields()
        {
            var item = MapSingle(new ArchiveArc
            {
                Aid = 114514,
                Title = "标题",
                Pic = "https://example.com/pic.jpg",
                Desc = "简介",
                Duration = 61,
                Ctime = 1700000000,
                Author = new Bilibili.App.Archive.V1.Author { Mid = 42, Name = "UP主", Face = "https://example.com/face.jpg" },
                Stat = new Bilibili.App.Archive.V1.Stat { View = 12345, Danmaku = 678, Reply = 9 },
            });

            Check(item.aid == "114514", "aid = " + item.aid);
            Check(item.title == "标题", "title = " + item.title);
            Check(item.pic == "https://example.com/pic.jpg", "pic = " + item.pic);
            Check(item.description == "简介", "description = " + item.description);
            Check(item.author == "UP主", "author = " + item.author);
            Check(item.length == "01:01", "length = " + item.length);
            Check(item.created == 1700000000, "created = " + item.created);
            Check(item.play == "12345", "play = " + item.play);
            Check(item.comment == 9, "comment = " + item.comment);
            Check(item.video_review == 678, "video_review = " + item.video_review);
        }

        private static void StripKeywordHighlight()
        {
            var item = MapSingle(new ArchiveArc
            {
                Title = "<em class=\"keyword\">猫</em>和<em class=\"keyword\">狗</em>的日常",
            });

            Check(item.title == "猫和狗的日常", "stripped title = " + item.title);
        }

        private static void FormatDurations()
        {
            Check(LengthOf(59) == "00:59", "59s = " + LengthOf(59));
            Check(LengthOf(61) == "01:01", "61s = " + LengthOf(61));
            Check(LengthOf(3661) == "01:01:01", "3661s = " + LengthOf(3661));
        }

        private static void HandleEmptyInputs()
        {
            Check(SpaceArchiveGrpcMapper.MapArchiveItems(null).Count == 0, "null input is empty");
            Check(SpaceArchiveGrpcMapper.MapArchiveItems(new List<Arc>()).Count == 0, "empty input is empty");
        }

        private static void HandleNullAuthorAndStat()
        {
            var item = MapSingle(new ArchiveArc
            {
                Aid = 1,
                Title = "无作者",
                Author = null,
                Stat = null,
            });

            Check(item.author == "", "null author maps to empty string");
            Check(item.play == "0", "null stat play = " + item.play);
            Check(item.comment == 0, "null stat comment = " + item.comment);
            Check(item.video_review == 0, "null stat video_review = " + item.video_review);
        }

        private static void SkipNullEntries()
        {
            var items = SpaceArchiveGrpcMapper.MapArchiveItems(new List<Arc>
            {
                null,
                new Arc { Archive = null },
                new Arc { Archive = new ArchiveArc { Aid = 7 } },
            });

            Check(items.Count == 1, "skips null entries, count = " + items.Count);
            Check(items[0].aid == "7", "surviving aid = " + items[0].aid);
        }

        private static string LengthOf(long seconds)
        {
            return MapSingle(new ArchiveArc { Duration = seconds }).length;
        }

        private static SubmitVideoItemModel MapSingle(ArchiveArc archive)
        {
            var items = SpaceArchiveGrpcMapper.MapArchiveItems(new List<Arc>
            {
                new Arc { Archive = archive },
            });
            if (items.Count != 1)
            {
                throw new InvalidOperationException("expected exactly one mapped item, got " + items.Count);
            }
            return items[0];
        }

        private static void Check(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException("assertion failed: " + message);
            }
            _passed++;
        }
    }
}
