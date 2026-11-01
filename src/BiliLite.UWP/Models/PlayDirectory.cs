using System;
using System.Collections.Generic;
using System.Globalization;

namespace BiliLite.Models.Common
{
    /// <summary>
    /// 目录里的一段:当前视频的分P、详情里的合集、打开时的播放列表
    /// </summary>
    public enum PlayDirectorySectionKind
    {
        Parts = 0,
        UgcSeason = 1,
        Playlist = 2,
    }

    /// <summary>
    /// 目录的一行。身份是 Key(分P用 cid,合集/播放列表用 aid),Ordinal 只是展示位置;
    /// 标题重复、翻页追加、封面裁剪都不会改变 Key。
    /// </summary>
    public sealed class PlayDirectoryRow
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public string Cover { get; set; }
        /// <summary>
        /// 次要信息:合集的分区名、UP主
        /// </summary>
        public string Subtitle { get; set; }
        /// <summary>
        /// 位置文字,例如 合集 7/24、第7项
        /// </summary>
        public string PositionText { get; set; }
        public string Duration { get; set; }
        /// <summary>
        /// 1 起的序号,只是展示用
        /// </summary>
        public int Ordinal { get; set; }
        public bool IsCurrent { get; set; }
        /// <summary>
        /// 当前行的文字标记("正在播放"),不是当前行时为空;界面直接按这个字符串显隐
        /// </summary>
        public string CurrentBadge { get; set; }

        /// <summary>
        /// 正在换到这一行。等待的行不是正在播放的行:它只挂转圈,不挂"正在播放"的条和字
        /// </summary>
        public bool IsPending { get; set; }

        /// <summary>
        /// 读屏读到的这一行:标题,再加上它现在的状态。
        /// 状态只在这里出现,行上的可见文字不再自己复述一遍标题
        /// </summary>
        public string AutomationName
        {
            get
            {
                var title = Title ?? "";
                if (IsPending) return title + " 正在切换";
                if (IsCurrent) return title + " 正在播放";
                return title;
            }
        }
    }

    public sealed class PlayDirectorySection
    {
        public PlayDirectorySectionKind Kind { get; set; }
        public string Title { get; set; }
        public IList<PlayDirectoryRow> Rows { get; set; }
    }

    /// <summary>
    /// 目录里远程播放列表的翻页状态。只有背后有 MediaListPagingState 的播放列表会用到;
    /// 固定内存队列和没有播放列表时是 None,目录不摆翻页 footer。
    /// </summary>
    public enum PlayDirectoryPagingState
    {
        /// <summary>
        /// 没有可翻的页:固定内存队列,或者根本没有播放列表
        /// </summary>
        None = 0,
        /// <summary>
        /// 还有下一页,footer 上是"加载更多"
        /// </summary>
        More = 1,
        /// <summary>
        /// 有一页在路上,footer 上只有转圈
        /// </summary>
        Loading = 2,
        /// <summary>
        /// 上一次取失败了,footer 上是"重试"
        /// </summary>
        Failed = 3,
        /// <summary>
        /// 已经取到头,footer 不留东西
        /// </summary>
        Exhausted = 4,
    }

    /// <summary>
    /// 一份完整的目录。Caption 是播放器"目录"按钮上的短标签,CountText 是页头的数量说明,
    /// CurrentPositionText 是"现在播到哪"的一句话;三者都跟着活动来源。
    /// </summary>
    public sealed class PlayDirectoryModel
    {
        public string SourceTitle { get; set; }
        public string Caption { get; set; }
        public string CountText { get; set; }
        /// <summary>
        /// 活动分段里当前行的 Key,定位正在播放时用它
        /// </summary>
        public string CurrentKey { get; set; }
        public string CurrentPositionText { get; set; }
        /// <summary>
        /// 页头那行来源:合集《标题》、播放列表《标题》、分P
        /// </summary>
        public string HeaderText { get; set; }
        public PlayDirectorySectionKind ActiveKind { get; set; }
        public IList<PlayDirectorySection> Sections { get; set; }
        /// <summary>
        /// 这一次是新开目录,不是重画。界面只在为真时把浏览的分段拉回连播来源,
        /// 重画要留着用户正在看的那一段。
        /// </summary>
        public bool IsFreshOpen { get; set; }

        /// <summary>
        /// 远程播放列表的翻页状态。固定内存队列是 None:没有更多页可取,界面上什么都不摆。
        /// </summary>
        public PlayDirectoryPagingState Paging { get; set; }

        /// <summary>
        /// 正在换到的那一行(Key)。空表示没有换目标在路上;
        /// 界面上这一行挂转圈,不当成正在播放
        /// </summary>
        public string PendingKey { get; set; }

        /// <summary>
        /// 生效的搜索词,空表示没过滤。过滤出来的是另一份模型:
        /// 未过滤的那份留在抽屉里,清空搜索就原样拿回来
        /// </summary>
        public string Query { get; set; }

        public bool IsFiltered
        {
            get { return !string.IsNullOrEmpty(Query); }
        }

        /// <summary>
        /// 一段都没有时不弹空抽屉
        /// </summary>
        public bool HasSections
        {
            get { return Sections != null && Sections.Count > 0; }
        }
    }

    /// <summary>
    /// 分P的最小输入:只有目录需要的字段,构建器不碰播放器的 PlayInfo
    /// </summary>
    public sealed class PlayDirectoryPart
    {
        public string Cid { get; set; }
        public string Title { get; set; }
        /// <summary>
        /// 秒
        /// </summary>
        public long DurationSeconds { get; set; }
    }

    /// <summary>
    /// 目录的组装。纯函数:只读传进来的顺序、合集和队列,不取页、不改播放、不决定下一步播什么。
    /// </summary>
    public static class PlayDirectoryBuilder
    {
        public const string PartsSectionTitle = "分P";
        public const string CollectionSectionTitle = "合集";
        public const string PlaylistSectionTitle = "播放列表";
        public const string NowPlayingBadge = "正在播放";

        /// <summary>
        /// 当前视频的分P。只有一个分P的普通视频没有这一段;互动视频除外。
        /// </summary>
        public static PlayDirectoryModel FromParts(IList<PlayDirectoryPart> parts, int currentIndex, string sourceTitle, bool interactionVideo)
        {
            var count = parts == null ? 0 : parts.Count;
            var index = count == 0 ? -1 : Math.Min(Math.Max(0, currentIndex), count - 1);

            var rows = new List<PlayDirectoryRow>();
            var currentKey = "";
            for (var i = 0; i < count; i++)
            {
                var part = parts[i];
                var key = PartKey(part, i);
                if (i == index) currentKey = key;
                rows.Add(new PlayDirectoryRow
                {
                    Key = key,
                    Title = part == null ? "" : part.Title,
                    Duration = FormatDuration(part == null ? 0 : part.DurationSeconds),
                    Ordinal = i + 1,
                    IsCurrent = i == index,
                    CurrentBadge = i == index ? NowPlayingBadge : "",
                });
            }

            var partText = PartText(index, count);
            var model = new PlayDirectoryModel
            {
                SourceTitle = sourceTitle ?? "",
                Caption = Caption(PlayDirectorySectionKind.Parts, index + 1, count),
                CountText = count.ToString(CultureInfo.InvariantCulture),
                CurrentKey = currentKey,
                CurrentPositionText = partText,
                ActiveKind = PlayDirectorySectionKind.Parts,
                Sections = new List<PlayDirectorySection>(),
            };

            if (count > 1 || interactionVideo)
            {
                model.Sections.Add(new PlayDirectorySection()
                {
                    Kind = PlayDirectorySectionKind.Parts,
                    Title = PartsSectionTitle,
                    Rows = rows,
                });
            }
            return model;
        }

        /// <summary>
        /// 详情里的合集。行顺序和连播顺序一样(分区、分集),身份是 aid。
        /// 合集分集只带时长,没有别的视频的分P数,所以位置文字只写视频位置。
        /// </summary>
        public static PlayDirectoryModel FromUgcSeason(VideoUgcSeason season, string currentAid)
        {
            var episodes = new List<EpisodeRow>();
            if (season != null && season.Sections != null)
            {
                foreach (var section in season.Sections)
                {
                    if (section == null || section.Episodes == null) continue;
                    foreach (var episode in section.Episodes)
                    {
                        if (episode == null || string.IsNullOrEmpty(episode.Aid)) continue;
                        episodes.Add(new EpisodeRow()
                        {
                            Aid = episode.Aid,
                            SectionTitle = section.Title,
                            Title = episode.Title,
                            Author = episode.Author == null ? null : episode.Author.Name,
                            Cover = episode.Cover,
                            Duration = episode.Page == null ? 0 : episode.Page.Duration,
                        });
                    }
                }
            }

            var total = episodes.Count;
            var rows = new List<PlayDirectoryRow>();
            var currentIndex = -1;
            for (var i = 0; i < total; i++)
            {
                var episode = episodes[i];
                var isCurrent = currentIndex < 0 && !string.IsNullOrEmpty(currentAid)
                    && string.Equals(episode.Aid, currentAid, StringComparison.Ordinal);
                if (isCurrent) currentIndex = i;
                rows.Add(new PlayDirectoryRow()
                {
                    Key = episode.Aid,
                    Title = episode.Title,
                    Cover = episode.Cover,
                    Subtitle = Join(episode.SectionTitle, episode.Author),
                    PositionText = PositionOf(CollectionSectionTitle, i + 1, total),
                    Duration = FormatDuration(episode.Duration),
                    Ordinal = i + 1,
                    IsCurrent = isCurrent,
                    CurrentBadge = isCurrent ? NowPlayingBadge : "",
                });
            }

            var model = new PlayDirectoryModel()
            {
                SourceTitle = season == null ? "" : season.Title,
                Caption = Caption(PlayDirectorySectionKind.UgcSeason, currentIndex + 1, total),
                CountText = total.ToString(CultureInfo.InvariantCulture),
                CurrentKey = currentIndex < 0 ? "" : rows[currentIndex].Key,
                CurrentPositionText = currentIndex < 0 ? "" : PositionOf(CollectionSectionTitle, currentIndex + 1, total),
                ActiveKind = PlayDirectorySectionKind.UgcSeason,
                Sections = new List<PlayDirectorySection>(),
            };

            if (total > 0)
            {
                model.Sections.Add(new PlayDirectorySection()
                {
                    Kind = PlayDirectorySectionKind.UgcSeason,
                    Title = CollectionSectionTitle,
                    Rows = rows,
                });
            }
            return model;
        }

        /// <summary>
        /// 打开视频时带的队列。带了翻页状态的是远程 medialist:数量按已加载和已知总数写,
        /// 不把已加载数冒充总数;没有翻页状态的固定队列直接写可播放总量。
        /// 当前行只认正在播的 aid:队列游标(playlist.Index)是连播的续播位置,它会停在旧的一行,
        /// 拿它标"正在播放"会让用户看到一条假的高亮。
        /// </summary>
        public static PlayDirectoryModel FromPlaylist(VideoPlaylist playlist, MediaListPagingState paging, string currentAid)
        {
            var items = playlist == null ? null : playlist.Playlist;
            var total = items == null ? 0 : items.Count;

            var rows = new List<PlayDirectoryRow>();
            var index = -1;
            for (var i = 0; i < total; i++)
            {
                var item = items[i];
                var isCurrent = index < 0 && !string.IsNullOrEmpty(currentAid) && item != null
                    && string.Equals(item.ID, currentAid, StringComparison.Ordinal);
                if (isCurrent) index = i;
                rows.Add(new PlayDirectoryRow()
                {
                    Key = item == null ? "" : item.ID,
                    Title = item == null ? "" : item.Title,
                    Cover = item == null ? "" : item.Cover,
                    Subtitle = item == null ? "" : item.Author,
                    PositionText = "",
                    Duration = item == null ? "" : item.Duration,
                    Ordinal = i + 1,
                    IsCurrent = isCurrent,
                    CurrentBadge = isCurrent ? NowPlayingBadge : "",
                });
            }

            var ordinal = index + 1;
            var title = paging != null && !string.IsNullOrEmpty(paging.Title)
                ? paging.Title
                : (playlist != null && !string.IsNullOrEmpty(playlist.Title) ? playlist.Title : MediaListPagingState.DefaultTitle);

            var model = new PlayDirectoryModel()
            {
                SourceTitle = title,
                Caption = Caption(PlayDirectorySectionKind.Playlist, ordinal, total),
                CountText = paging != null
                    ? CountText(paging.LoadedCount, paging.HasKnownTotal, paging.TotalCount)
                    : FixedCountText(ordinal, total),
                CurrentKey = index < 0 ? "" : rows[index].Key,
                CurrentPositionText = ordinal > 0 ? OrdinalText(ordinal) : "",
                ActiveKind = PlayDirectorySectionKind.Playlist,
                Sections = new List<PlayDirectorySection>(),
                Paging = PagingOf(paging),
            };

            if (total > 0)
            {
                model.Sections.Add(new PlayDirectorySection()
                {
                    Kind = PlayDirectorySectionKind.Playlist,
                    Title = PlaylistSectionTitle,
                    Rows = rows,
                });
            }
            return model;
        }

        /// <summary>
        /// 把三段拼成一份目录:分段顺序固定为分P、合集、播放列表,只收有内容的分段。
        /// 活动分段优先跟随连播来源,没有外层来源时落在分P上。
        /// </summary>
        public static PlayDirectoryModel Combine(PlayDirectoryModel parts, PlayDirectoryModel collection,
            PlayDirectoryModel playlist, WatchSequenceSourceKind activeSource)
        {
            var sections = new List<PlayDirectorySection>();
            AppendSections(sections, parts);
            AppendSections(sections, collection);
            AppendSections(sections, playlist);

            var activeKind = ResolveActiveKind(sections, activeSource);
            var model = ModelOf(activeKind, parts, collection, playlist);

            var sourceLabel = SourceLabelOf(activeKind);
            var sourceTitle = model == null ? "" : model.SourceTitle;
            // 页头只写来源本身;下一集在"下一个"按钮上,分P位置在分P页签里
            var header = activeKind != PlayDirectorySectionKind.Parts && !string.IsNullOrEmpty(sourceTitle)
                ? sourceLabel + "《" + sourceTitle + "》"
                : sourceLabel;

            return new PlayDirectoryModel()
            {
                SourceTitle = sourceTitle,
                Caption = model == null ? "" : model.Caption,
                CountText = model == null ? "" : model.CountText,
                CurrentKey = model == null ? "" : model.CurrentKey,
                CurrentPositionText = model == null ? "" : model.CurrentPositionText,
                HeaderText = header,
                ActiveKind = activeKind,
                Sections = sections,
                // 翻页属于播放列表那一段,与当前跟哪个来源连播无关:合集在连载时播放列表照样还有页
                Paging = playlist == null ? PlayDirectoryPagingState.None : playlist.Paging,
            };
        }

        /// <summary>
        /// 把"正在换到这一行"标进目录:等待的行只多一个标记,
        /// 当前行(正在播放)的条和字都不动,等待的行也不会被标成当前行。
        /// </summary>
        public static PlayDirectoryModel ApplyPending(PlayDirectoryModel model, string pendingKey)
        {
            if (model == null) return null;
            var key = pendingKey ?? "";
            model.PendingKey = key;
            if (model.Sections == null) return model;
            foreach (var section in model.Sections)
            {
                if (section == null || section.Rows == null) continue;
                foreach (var row in section.Rows)
                {
                    if (row == null) continue;
                    row.IsPending = !string.IsNullOrEmpty(key) && string.Equals(row.Key, key, StringComparison.Ordinal);
                }
            }
            return model;
        }

        /// <summary>
        /// 在已经拿到手的目录里搜索。纯函数:只按标题筛手里的行,不取页、不换来源、不动播放,
        /// 也不改传进来的那份模型。
        /// 分段顺序不变,一段没命中就留一个空段(用户要看得见它还在,只是没有结果);
        /// 活下来的行还是原来那些对象,Key/Ordinal/当前/等待都跟着它们走。
        /// </summary>
        public static PlayDirectoryModel Filter(PlayDirectoryModel model, string query)
        {
            if (model == null) return null;
            var needle = (query ?? "").Trim();
            if (needle.Length == 0) return model;

            var sections = new List<PlayDirectorySection>();
            if (model.Sections != null)
            {
                foreach (var section in model.Sections)
                {
                    if (section == null) continue;
                    var rows = new List<PlayDirectoryRow>();
                    if (section.Rows != null)
                    {
                        foreach (var row in section.Rows)
                        {
                            if (row == null || !TitleMatches(row.Title, needle)) continue;
                            rows.Add(row);
                        }
                    }
                    sections.Add(new PlayDirectorySection()
                    {
                        Kind = section.Kind,
                        Title = section.Title,
                        Rows = rows,
                    });
                }
            }

            // 其余字段照抄:活动来源、翻页状态、当前行和等待行都还是这份目录的
            return new PlayDirectoryModel()
            {
                SourceTitle = model.SourceTitle,
                Caption = model.Caption,
                CountText = model.CountText,
                CurrentKey = model.CurrentKey,
                CurrentPositionText = model.CurrentPositionText,
                HeaderText = model.HeaderText,
                ActiveKind = model.ActiveKind,
                Sections = sections,
                IsFreshOpen = model.IsFreshOpen,
                Paging = model.Paging,
                PendingKey = model.PendingKey,
                Query = needle,
            };
        }

        /// <summary>
        /// 行在它自己来源里的下标。身份只认 Key:过滤之后 Ordinal 还是原来源里的位置,
        /// 行的位置却变了,拿它当下标就会播错一条。来源里已经没有这个 Key 时返回 -1(过期行)。
        /// </summary>
        public static int SourceIndexOf(string key, IList<string> sourceKeys)
        {
            if (sourceKeys == null || string.IsNullOrEmpty(key)) return -1;
            for (var i = 0; i < sourceKeys.Count; i++)
            {
                var candidate = sourceKeys[i];
                if (string.IsNullOrEmpty(candidate)) continue;
                if (string.Equals(candidate, key, StringComparison.Ordinal)) return i;
            }
            return -1;
        }

        /// <summary>
        /// 播放器按钮上的短标签:7/24、第7项、2/3。
        /// 当前位置不在这一段里(没有当前行)时不写位置。
        /// </summary>
        public static string Caption(PlayDirectorySectionKind kind, int ordinal, int count)
        {
            if (ordinal <= 0 || count <= 0) return "";
            switch (kind)
            {
                case PlayDirectorySectionKind.UgcSeason:
                    return CountPosition(ordinal, count);
                case PlayDirectorySectionKind.Playlist:
                    return OrdinalText(ordinal);
                default:
                    return CountPosition(Math.Max(1, ordinal), count);
            }
        }

        /// <summary>
        /// 远程队列的数量:已加载/已知总数,总数未知时只写已加载数
        /// </summary>
        public static string CountText(int loadedCount, bool hasKnownTotal, int totalCount)
        {
            if (loadedCount < 0) loadedCount = 0;
            var loaded = loadedCount.ToString(CultureInfo.InvariantCulture);
            return hasKnownTotal
                ? loaded + "/" + Math.Max(0, totalCount).ToString(CultureInfo.InvariantCulture)
                : loaded;
        }

        /// <summary>
        /// 远程列表的翻页状态。取页在路上时就是"加载中";失败要先于"还有下一页"判定,
        /// 否则重试入口会被 has_more 盖掉;首页还没取过时也还有东西可取。
        /// </summary>
        public static PlayDirectoryPagingState PagingOf(MediaListPagingState paging)
        {
            if (paging == null) return PlayDirectoryPagingState.None;
            if (paging.IsLoading) return PlayDirectoryPagingState.Loading;
            if (paging.LastLoadFailed) return PlayDirectoryPagingState.Failed;
            if (paging.IsFirstPage || paging.HasMore) return PlayDirectoryPagingState.More;
            return PlayDirectoryPagingState.Exhausted;
        }

        /// <summary>
        /// 固定内存队列没有翻页:写当前项/总量,没有当前项时只写总量
        /// </summary>
        public static string FixedCountText(int ordinal, int count)
        {
            if (count < 0) count = 0;
            var total = count.ToString(CultureInfo.InvariantCulture);
            return ordinal > 0 ? CountPosition(ordinal, count) : total;
        }

        /// <summary>
        /// 某一段里的当前行。只在这一段里找:合集和播放列表可以同时有当前行,
        /// 扫全部分段就会把正在浏览的段换成排在前面的分段。
        /// </summary>
        public static PlayDirectoryRow CurrentRowOf(PlayDirectoryModel model, PlayDirectorySectionKind kind)
        {
            var section = SectionOf(model == null ? null : model.Sections, kind);
            if (section == null || section.Rows == null) return null;
            foreach (var row in section.Rows)
            {
                if (row != null && row.IsCurrent) return row;
            }
            return null;
        }

        /// <summary>
        /// 定位正在播放该落在哪一段:先看用户正在浏览的段,这一段没有当前行
        /// (视频不在这一段里)才退到连播来源那一段;两段都没有当前行时不动浏览的分段。
        /// </summary>
        public static PlayDirectorySectionKind LocateSection(PlayDirectoryModel model, PlayDirectorySectionKind browsed)
        {
            if (CurrentRowOf(model, browsed) != null) return browsed;
            if (model != null && CurrentRowOf(model, model.ActiveKind) != null) return model.ActiveKind;
            return browsed;
        }

        /// <summary>
        /// 重画之后该停在哪一段:用户正在浏览的段还在就留在那里,只有新开目录
        /// (browsed 为空)或那一段消失了才跟随连播来源。
        /// </summary>
        public static PlayDirectorySectionKind ResolveBrowsedSection(PlayDirectoryModel model, PlayDirectorySectionKind? browsed)
        {
            var sections = model == null ? null : model.Sections;
            if (browsed.HasValue && SectionOf(sections, browsed.Value) != null) return browsed.Value;

            var active = model == null ? PlayDirectorySectionKind.Parts : model.ActiveKind;
            if (SectionOf(sections, active) != null) return active;
            if (sections != null)
            {
                foreach (var section in sections)
                {
                    if (section != null) return section.Kind;
                }
            }
            return PlayDirectorySectionKind.Parts;
        }

        /// <summary>
        /// 秒数写成 mm:ss,超过一小时写成 hh:mm:ss
        /// </summary>
        public static string FormatDuration(long seconds)
        {
            if (seconds < 0) seconds = 0;
            var duration = TimeSpan.FromSeconds(seconds);
            return seconds >= 3600 ? duration.ToString(@"hh\:mm\:ss") : duration.ToString(@"mm\:ss");
        }

        /// <summary>
        /// 标题命中:不分大小写,词两头的空白不算内容。空词已经在外层挡掉了
        /// </summary>
        private static bool TitleMatches(string title, string needle)
        {
            return !string.IsNullOrEmpty(title) && title.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// 分P行的身份键:有 cid 就是 cid;详情没给 cid 时退到位置,至少不会和别的行撞成同一个 Key。
        /// 解析来源下标时用同一条规则,行的 Key 才总能回到它自己的来源位置
        /// </summary>
        public static string PartKey(string cid, int index)
        {
            if (!string.IsNullOrEmpty(cid)) return cid;
            return "part-" + (index + 1).ToString(CultureInfo.InvariantCulture);
        }

        private static string PartKey(PlayDirectoryPart part, int index)
        {
            return PartKey(part == null ? null : part.Cid, index);
        }

        private static string PartText(int index, int count)
        {
            if (count <= 0 || index < 0) return "";
            return "P" + (index + 1).ToString(CultureInfo.InvariantCulture)
                + "/" + count.ToString(CultureInfo.InvariantCulture);
        }

        private static string CountPosition(int ordinal, int count)
        {
            return ordinal.ToString(CultureInfo.InvariantCulture) + "/" + count.ToString(CultureInfo.InvariantCulture);
        }

        private static string PositionOf(string label, int ordinal, int count)
        {
            if (count <= 0) return label;
            return label + " " + CountPosition(ordinal, count);
        }

        private static string OrdinalText(int ordinal)
        {
            return "第" + ordinal.ToString(CultureInfo.InvariantCulture) + "项";
        }

        private static string SourceLabelOf(PlayDirectorySectionKind kind)
        {
            switch (kind)
            {
                case PlayDirectorySectionKind.UgcSeason: return CollectionSectionTitle;
                case PlayDirectorySectionKind.Playlist: return PlaylistSectionTitle;
                default: return PartsSectionTitle;
            }
        }

        private static void AppendSections(List<PlayDirectorySection> target, PlayDirectoryModel model)
        {
            if (model == null || model.Sections == null) return;
            foreach (var section in model.Sections)
            {
                if (section == null || section.Rows == null || section.Rows.Count == 0) continue;
                target.Add(section);
            }
        }

        private static PlayDirectorySectionKind ResolveActiveKind(IList<PlayDirectorySection> sections, WatchSequenceSourceKind activeSource)
        {
            if (activeSource == WatchSequenceSourceKind.UgcSeason && SectionOf(sections, PlayDirectorySectionKind.UgcSeason) != null)
            {
                return PlayDirectorySectionKind.UgcSeason;
            }
            if (activeSource == WatchSequenceSourceKind.Playlist && SectionOf(sections, PlayDirectorySectionKind.Playlist) != null)
            {
                return PlayDirectorySectionKind.Playlist;
            }
            // 活动来源不在目录里时,先开还有什么外层列表;都没有外层列表才落在分P上
            if (SectionOf(sections, PlayDirectorySectionKind.UgcSeason) != null) return PlayDirectorySectionKind.UgcSeason;
            if (SectionOf(sections, PlayDirectorySectionKind.Playlist) != null) return PlayDirectorySectionKind.Playlist;
            return PlayDirectorySectionKind.Parts;
        }

        private static PlayDirectorySection SectionOf(IList<PlayDirectorySection> sections, PlayDirectorySectionKind kind)
        {
            if (sections == null) return null;
            foreach (var section in sections)
            {
                if (section != null && section.Kind == kind) return section;
            }
            return null;
        }

        private static PlayDirectoryModel ModelOf(PlayDirectorySectionKind kind, PlayDirectoryModel parts,
            PlayDirectoryModel collection, PlayDirectoryModel playlist)
        {
            switch (kind)
            {
                case PlayDirectorySectionKind.UgcSeason: return collection ?? parts ?? playlist;
                case PlayDirectorySectionKind.Playlist: return playlist ?? parts ?? collection;
                default: return parts ?? collection ?? playlist;
            }
        }

        /// <summary>
        /// 行副标题的两段(合集分区、UP主)。分隔只用空格:"·"是把两个事实拼成一句话的写法。
        /// </summary>
        private static string Join(string left, string right)
        {
            if (string.IsNullOrEmpty(left)) return right ?? "";
            if (string.IsNullOrEmpty(right)) return left;
            return left + " " + right;
        }

        private sealed class EpisodeRow
        {
            public string Aid;
            public string SectionTitle;
            public string Title;
            public string Author;
            public string Cover;
            public long Duration;
        }
    }
}
