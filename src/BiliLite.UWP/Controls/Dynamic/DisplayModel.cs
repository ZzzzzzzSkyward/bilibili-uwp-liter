using BiliLite.Helpers;
using BiliLite.Modules;
using BiliLite.Parsing.Dynamic;
using System.Collections.Generic;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace BiliLite.Controls.Dynamic
{
    /// <summary>
    /// 正文之外的附件种类；未知种类不丢弃，按 Unsupported 保留原类型名（只用于诊断，不上屏），
    /// 标题/说明/目的地全空的那种整行不渲染。
    /// </summary>
    public enum DynamicAttachmentKind
    {
        Reserve,
        Vote,
        CommonLink,
        Unsupported,
    }

    /// <summary>
    /// 附件摘要：正文之外另挂的一张卡。与主载荷分开保存 —— 图片/文字/视频动态都可能同时挂着
    /// 一张投票或一个预约，附件不是"主载荷的替代品"。
    /// </summary>
    public sealed class DynamicAttachmentSummary
    {
        public DynamicAttachmentKind Kind { get; set; }
        /// <summary>来源给的附件类型名，原样保留（未知类型也能被诊断）。</summary>
        public string RawType { get; set; } = "";
        /// <summary>附件自己的对象 ID（预约 rid / 投票 id），没有就是空串。</summary>
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        /// <summary>可读的一行说明，不参与正文。</summary>
        public string Desc { get; set; } = "";
        /// <summary>归一化后的目的地；空串表示没有目的地，只能当文本读。</summary>
        public string Destination { get; set; } = "";
        /// <summary>预约按钮的文案（check/uncheck 由 status 选出）；其它种类与没有按钮时都是空串。</summary>
        public string ButtonText { get; set; } = "";
        /// <summary>预约按钮的图标地址；只有真能画按钮的附件才有值。</summary>
        public string ButtonIcon { get; set; } = "";
        public bool HasDestination { get { return !string.IsNullOrEmpty(Destination); } }
    }

    /// <summary>
    /// 附件摘要的固定顺序：预约、投票、通用链接，最后才是原样保留的其它类型。
    /// 卡片模型的 additional 是"每种一个槽位"，这里把槽位按顺序摊成列表。
    /// </summary>
    public static class DynamicAttachmentSummaries
    {
        public static List<DynamicAttachmentSummary> From(DynamicCardModel2024 card)
        {
            var result = new List<DynamicAttachmentSummary>();
            var additional = card?.modules?.module_dynamic?.additional;
            if (additional == null)
            {
                return result;
            }

            var rawType = additional.type ?? "";

            if (additional.reserve != null)
            {
                var reserve = additional.reserve;
                var button = reserve.button;
                result.Add(new DynamicAttachmentSummary
                {
                    Kind = DynamicAttachmentKind.Reserve,
                    RawType = rawType,
                    Id = reserve.rid > 0 ? reserve.rid.ToString() : "",
                    Title = reserve.title ?? "",
                    Desc = ReserveCardCopy.Format(reserve),
                    // 直播预约的跳转在 button.jump_url；reserve.jump_url 是卡片自己的地址，按钮优先
                    Destination = WireScalars.NormalizeWebUrl(FirstPresent(button?.jump_url, reserve.jump_url)),
                    ButtonText = button?.text ?? "",
                    // web 来源填 button.icon_url，protobuf 来源只填 check/uncheck 的图标；两处都空就没有图标
                    ButtonIcon = FirstPresent(button?.icon_url, button?.icon),
                });
            }

            if (additional.vote != null)
            {
                var vote = additional.vote;
                result.Add(new DynamicAttachmentSummary
                {
                    Kind = DynamicAttachmentKind.Vote,
                    RawType = rawType,
                    Id = vote.id_str ?? "",
                    Title = vote.title ?? "",
                    Desc = WireScalars.JoinText(vote.desc1, vote.desc2),
                    Destination = WireScalars.NormalizeWebUrl(vote.jump_url),
                });
            }

            if (additional.common != null)
            {
                var common = additional.common;
                result.Add(new DynamicAttachmentSummary
                {
                    Kind = DynamicAttachmentKind.CommonLink,
                    RawType = rawType,
                    Id = common.id_str ?? "",
                    Title = common.title ?? "",
                    Desc = WireScalars.JoinText(common.desc1, common.desc2, common.head_text),
                    Destination = WireScalars.NormalizeWebUrl(common.jump_url),
                });
            }

            AppendUnsupported(result, additional.goods, rawType);
            AppendUnsupported(result, additional.ugc, rawType);
            if (result.Count == 0)
            {
                // 有附件块、但里面没有能认出来的子卡：保留类型名当诊断线索，页面至少知道"这里有东西"
                result.Add(new DynamicAttachmentSummary
                {
                    Kind = DynamicAttachmentKind.Unsupported,
                    RawType = rawType,
                    Desc = additional.title ?? "",
                });
            }

            // 认不出的种类如果连标题、说明、目的地都没有，就只是一行类型名，什么都不该上屏（BR-57）
            result.RemoveAll(x => x.Kind == DynamicAttachmentKind.Unsupported
                && string.IsNullOrWhiteSpace(x.Title)
                && string.IsNullOrWhiteSpace(x.Desc)
                && !x.HasDestination);

            return result;
        }

        /// <summary>
        /// 两个来源各填一处（web 给 button.icon_url，protobuf 只给 check/uncheck 的图标），
        /// 字段存在但为空串时也要继续看下一处；都空就是空串。
        /// </summary>
        private static string FirstPresent(string primary, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(primary))
            {
                return primary.Trim();
            }
            return (fallback ?? "").Trim();
        }

        private static void AppendUnsupported(
            List<DynamicAttachmentSummary> target,
            DynamicCardModel2024_addin addin,
            string rawType)
        {
            if (addin == null)
            {
                return;
            }
            target.Add(new DynamicAttachmentSummary
            {
                Kind = DynamicAttachmentKind.Unsupported,
                RawType = rawType,
                Id = addin.id_str ?? "",
                Title = addin.title ?? "",
                Desc = WireScalars.JoinText(addin.desc1, addin.desc2, addin.head_text),
                Destination = WireScalars.NormalizeWebUrl(addin.jump_url),
            });
        }
    }

    /// <summary>
    /// 附件行的一项（CARD_PLAN §7）：短标题 + 一行说明 + 一个只在真有目的地时才出现的打开动作。
    /// 没有目的地时只剩可读文本，不做成按了没反应的按钮；未知种类给「商品」/「合集」/「附件」这类中文短标签，
    /// wire 类型名（RawType）只留在摘要里做诊断、不上屏，也不生成投票/预约的写入动作。
    /// </summary>
    public sealed class DynamicAttachmentDisplay
    {
        public DynamicAttachmentDisplay(DynamicAttachmentSummary summary, ICommand openCommand)
        {
            if (summary == null)
            {
                summary = new DynamicAttachmentSummary();
            }
            KindLabel = KindLabelOf(summary.Kind, summary.RawType);
            Title = Clean(summary.Title);
            Desc = Clean(summary.Desc);
            Destination = Clean(summary.Destination);
            ButtonText = Clean(summary.ButtonText);
            ButtonIcon = Clean(summary.ButtonIcon);
            OpenCommand = openCommand;
            // 预约附件不再走安静行：它有角标、标题、说明和载荷自带的按钮，模板按这一位选面板
            ShowPanel = summary.Kind == DynamicAttachmentKind.Reserve;
            TitleVisibility = string.IsNullOrEmpty(Title) ? Visibility.Collapsed : Visibility.Visible;
            DescVisibility = string.IsNullOrEmpty(Desc) ? Visibility.Collapsed : Visibility.Visible;
            IconVisibility = string.IsNullOrEmpty(ButtonIcon) ? Visibility.Collapsed : Visibility.Visible;
            // 文案/目的地/命令缺一个就不画按钮：不生成点了没反应的按钮，也不替来源编造文案
            ButtonVisibility = Destination.Length > 0 && ButtonText.Length > 0 && openCommand != null
                ? Visibility.Visible
                : Visibility.Collapsed;
            // 有目的地也得有真正可用的路由才画按钮:没有命令时宁可不显示,也不留一个点了没反应的"打开"
            OpenVisibility = Destination.Length > 0 && openCommand != null ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>种类标签：已知种类给中文短标签，未知种类原样用来源的类型名。</summary>
        public string KindLabel { get; private set; }
        public string Title { get; private set; }
        public Visibility TitleVisibility { get; private set; }
        /// <summary>可读的一行说明；空串表示这行不占位。</summary>
        public string Desc { get; private set; }
        public Visibility DescVisibility { get; private set; }
        public string Destination { get; private set; }
        /// <summary>打开动作走既有 LaunchUrlCommand（MessageCenter.HandleUrl），不另建一套导航。</summary>
        public ICommand OpenCommand { get; private set; }
        public Visibility OpenVisibility { get; private set; }
        /// <summary>预约附件用面板渲染（BR-55 的预约语言）；其它种类仍是安静的一行。</summary>
        public bool ShowPanel { get; private set; }
        /// <summary>面板按钮的文案，取自 reserve.button.text；空串就是不画按钮。</summary>
        public string ButtonText { get; private set; }
        /// <summary>面板按钮的图标，取自 reserve.button 的 icon_url（回退 check/uncheck 图标）。</summary>
        public string ButtonIcon { get; private set; }
        public Visibility ButtonVisibility { get; private set; }
        public Visibility IconVisibility { get; private set; }

        /// <summary>
        /// 种类标签只给中文短词：wire 类型名（ADDITIONAL_TYPE_*）是诊断字段，不上屏。
        /// 认不出的种类一律是「附件」——没标题没说明没目的地的那种整行不画（见 DynamicAttachmentSummaries.From）。
        /// </summary>
        private static string KindLabelOf(DynamicAttachmentKind kind, string rawType)
        {
            switch (kind)
            {
                case DynamicAttachmentKind.Reserve:
                    return "预约";
                case DynamicAttachmentKind.Vote:
                    return "投票";
                case DynamicAttachmentKind.CommonLink:
                    return "链接";
                default:
                    switch (rawType)
                    {
                        case "ADDITIONAL_TYPE_GOODS":
                            return "商品";
                        case "ADDITIONAL_TYPE_UGC":
                            return "合集";
                        default:
                            return "附件";
                    }
            }
        }

        private static string Clean(string value)
        {
            return value == null ? "" : value.Trim();
        }
    }

    /// <summary>一项互动是否可用；未知与"明确禁止"是两件事。</summary>
    public enum DynamicActionAvailability
    {
        Unknown,
        Allowed,
        Forbidden,
    }

    /// <summary>
    /// 互动可用性：服务端报 forbidden=true 时不能把按钮当成会成功的动作展示；
    /// 字段缺席是未知，既不是"确认可用"也不是"确认不可用"。
    /// </summary>
    public sealed class DynamicCardActions
    {
        public DynamicActionAvailability Comment { get; private set; }
        public DynamicActionAvailability Repost { get; private set; }

        public bool CanComment { get { return Comment != DynamicActionAvailability.Forbidden; } }
        public bool CanRepost { get { return Repost != DynamicActionAvailability.Forbidden; } }

        public static DynamicCardActions From(DynamicCardModel2024 card)
        {
            var stat = card?.modules?.module_stat;
            return new DynamicCardActions
            {
                Comment = Availability(stat?.comment),
                Repost = Availability(stat?.forward),
            };
        }

        private static DynamicActionAvailability Availability(DynamicCardModel2024_forward counter)
        {
            if (counter == null)
            {
                return DynamicActionAvailability.Unknown;
            }
            return counter.forbidden ? DynamicActionAvailability.Forbidden : DynamicActionAvailability.Allowed;
        }
    }

    /// <summary>
    /// 卡片数据 → 显示模型的那几样归一化数据：评论目标对、附件摘要、互动可用性。
    /// 单独放这里而不是塞进 VM，是为了让这一步能被探针直接执行（VM 需要整个 WinRT 环境）。
    /// </summary>
    public static class DynamicCardProjection
    {
        public static void Apply(DynamicItemDisplayModel display, DynamicCardModel2024 card)
        {
            if (display == null || card == null)
            {
                return;
            }

            var basic = card.basic;
            display.ReplyID = basic?.comment_id_str ?? "";
            display.ReplyType = basic?.comment_type ?? 0;
            display.Attachments = DynamicAttachmentSummaries.From(card);
            display.Actions = DynamicCardActions.From(card);
        }

        /// <summary>
        /// 预约卡只有一个主人：文本动态被提升成 Reserve 之后由独立模板画预约面板，而附件投影是在提升之前
        /// 跑完的，那条预约摘要必须在这里摘掉，否则同一张预约会画两遍。只摘预约，投票/链接/未知种类照旧
        /// 留在附件行。赋值 Attachments 会清掉已经建好的 AttachmentItems 缓存。
        /// </summary>
        public static void ApplyAttachmentOwnership(DynamicItemDisplayModel display)
        {
            if (display == null
                || display.Type != DynamicDisplayType.Reserve
                || display.Attachments == null)
            {
                return;
            }

            var kept = new List<DynamicAttachmentSummary>(display.Attachments.Count);
            foreach (var summary in display.Attachments)
            {
                if (summary == null || summary.Kind != DynamicAttachmentKind.Reserve)
                {
                    kept.Add(summary);
                }
            }
            display.Attachments = kept;
        }

        /// <summary>
        /// 卡片标题槽：预览模板自己显示标题的卡片（OneRow / Reserve）留空，同一句标题只出现一次；
        /// 其余卡片把标题放进标题槽。标题从不拼进正文。
        /// </summary>
        public static void ApplyTitleSlot(DynamicItemDisplayModel display, string title, bool previewShowsTitle)
        {
            if (display == null)
            {
                return;
            }
            display.Title = previewShowsTitle ? "" : title ?? "";
            display.TitleVisibility = string.IsNullOrEmpty(display.Title) ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// 内嵌原动态那块：OriginInfo 里真有卡片才显示边框。深度到顶（OriginInfo 为空）时收起，
        /// Miss 占位卡也算一张卡，照旧显示它自己的"资源已失效"。
        /// </summary>
        public static void ApplyOriginVisibility(DynamicItemDisplayModel display)
        {
            if (display == null)
            {
                return;
            }
            display.OriginVisibility = display.OriginInfo != null && display.OriginInfo.Count > 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        /// <summary>转发链被截断时的祖先行：只有真实 ID 才显示，空 ID 不留一行点不动的行。</summary>
        public static void ApplyAncestry(DynamicItemDisplayModel display, string ancestorId)
        {
            if (display == null)
            {
                return;
            }
            display.AncestryDynamicID = ancestorId ?? "";
            display.AncestryVisibility = string.IsNullOrEmpty(display.AncestryDynamicID)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        /// <summary>
        /// 转发层级收口：原卡里被截断的祖先（A 转发 B 转发 C 的 C）搬到最外层卡片上，
        /// 祖先只显示一次，原卡自己不再留指向更深一层的行；第三层不会生成模型。
        /// </summary>
        public static void HoistAncestry(DynamicItemDisplayModel outer, DynamicItemDisplayModel original)
        {
            if (outer == null || original == null)
            {
                return;
            }

            var ancestorId = original.AncestryDynamicID;
            ApplyAncestry(original, "");
            ApplyAncestry(outer, ancestorId);
        }
    }

    public class DynamicItemDisplayModel : IModules
    {
        /// <summary>
        /// 类型
        /// </summary>
        public int IntType { get; set; }
        public DynamicDisplayType Type { get; set; }
        /// <summary>
        /// 是否显示稍后再看
        /// </summary>
        public bool ShowWatchLater { get { return Type == DynamicDisplayType.Video; } }
        /// <summary>
        /// 动态ID
        /// </summary>
        public string DynamicID { get; set; }
        /// <summary>
        /// 评论ID 
        /// </summary>
        public string ReplyID { get; set; }
        /// <summary>
        /// 评论类型
        /// </summary>
        public int ReplyType { get; set; }
        /// <summary>
        /// 适配器归一化后的评论目标：oid 与评论接口类型成对给出；没有真实一对时为 null，
        /// 调用方这时才按显示类型走既有映射。
        /// </summary>
        public DynamicCommentTarget CommentTarget
        {
            get { return DynamicCommentTarget.From(ReplyID, ReplyType); }
        }
        /// <summary>正文之外的附件摘要（预约/投票/通用链接/未知类型），与主载荷分开。</summary>
        public List<DynamicAttachmentSummary> Attachments
        {
            get { return _attachments; }
            set
            {
                _attachments = value;
                _attachmentItems = null;
            }
        }
        private List<DynamicAttachmentSummary> _attachments;
        private List<DynamicAttachmentDisplay> _attachmentItems;
        /// <summary>
        /// 附件行的显示项。首次读取时按摘要和当前 LaunchUrlCommand 生成一次；Attachments 变了就重算，
        /// 打开动作仍走既有的 LaunchUrlCommand（MessageCenter.HandleUrl），没有第二套导航。
        /// </summary>
        public List<DynamicAttachmentDisplay> AttachmentItems
        {
            get
            {
                if (_attachmentItems == null)
                {
                    _attachmentItems = BuildAttachmentItems();
                }
                return _attachmentItems;
            }
        }
        /// <summary>附件区是否占位：列表为空时整块收起，不画空行。</summary>
        public Visibility AttachmentRowVisibility
        {
            get { return AttachmentItems.Count > 0 ? Visibility.Visible : Visibility.Collapsed; }
        }
        private List<DynamicAttachmentDisplay> BuildAttachmentItems()
        {
            var result = new List<DynamicAttachmentDisplay>();
            if (_attachments == null)
            {
                return result;
            }
            foreach (var summary in _attachments)
            {
                if (summary != null)
                {
                    result.Add(new DynamicAttachmentDisplay(summary, LaunchUrlCommand));
                }
            }
            return result;
        }
        /// <summary>互动可用性；服务端报禁止时不能当成会成功的动作。</summary>
        public DynamicCardActions Actions { get; set; }
        /// <summary>评论不可用时页面不该假装点了会成功；不知道不等于禁止。</summary>
        public bool CanComment
        {
            get { return Actions == null || Actions.CanComment; }
        }
        /// <summary>
        /// 用户ID
        /// </summary>
        public long Mid { get; set; }
        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; }
        public string Photo { get; set; }
        public string Verify { get; set; } = AppHelper.TRANSPARENT_IMAGE;
        public string Pendant { get; set; } = AppHelper.TRANSPARENT_IMAGE;

        /// <summary>
        /// 装扮
        /// </summary>
        public string DecorateName { get; set; }
        public string DecorateText { get; set; }
        public string DecorateColor { get; set; }
        public bool ShowDecorateText { get { return !string.IsNullOrEmpty(DecorateText); } }
        public string DecorateImage { get; set; }
        public bool ShowDecorate { get { return !string.IsNullOrEmpty(DecorateImage); } }
        /// <summary>
        /// 被转发用户名旁的文本
        /// </summary>
        public string Tooltip
        {
            get
            {
                switch (Type)
                {

                    case DynamicDisplayType.Text:
                        return "的动态";
                    case DynamicDisplayType.Photo:
                        return "的图片";
                    case DynamicDisplayType.Video:
                        return "的视频";
                    case DynamicDisplayType.ShortVideo:
                        return "的短视频";
                    case DynamicDisplayType.Music:
                        return "的音频";
                    case DynamicDisplayType.Web:
                        return "的分享";
                    case DynamicDisplayType.Article:
                        return "的专栏";
                    case DynamicDisplayType.Live:
                        return "的直播";
                    case DynamicDisplayType.LiveShare:
                        return "的直播";
                    default:
                        return "";
                }
            }
        }
        /// <summary>
        /// 发表时间
        /// </summary>
        public string Time { get; set; }
        /// <summary>
        /// 发表时间(绝对)
        /// </summary>
        public string Datetime { get; set; }
        /// <summary>
        /// 卡片标题槽：预览模板不显示标题的卡片（纯文字/图片等）由这里显示一次；
        /// 空串表示没有标题，标题不拼进正文。
        /// </summary>
        public string Title { get; set; } = "";
        /// <summary>
        /// 标题槽是否显示：Title 非空且这一类的预览模板不自己显示标题时为 Visible。
        /// </summary>
        public Visibility TitleVisibility { get; set; } = Visibility.Collapsed;
        /// <summary>
        /// 转发被截断在第二层时的祖先行：只有一个动态 ID，点击后按 ID 打开详情。
        /// </summary>
        public Visibility AncestryVisibility { get; set; } = Visibility.Collapsed;
        public string AncestryDynamicID { get; set; }
        /// <summary>
        /// 按动态 ID 打开详情（祖先行用）。参数是 string 动态 ID，不是 aid/rid。
        /// </summary>
        public ICommand DetailByIdCommand { get; set; }
        /// <summary>
        /// 内嵌原动态那块的边框是否显示：只有 OriginInfo 里真有一张卡时才 Visible。
        /// 转发深度到顶时 OriginInfo 为空，边框必须收起来，否则会画出一个空框。
        /// </summary>
        public Visibility OriginVisibility { get; set; } = Visibility.Collapsed;
        /// <summary>
        /// 内容
        /// </summary>
        public RichTextBlock Content { get; set; }
        public string ContentStr { get; set; }
        /// <summary>
        /// 显示内容
        /// </summary>
        public bool ShowContent { get; set; } = true;
        private int _shareCount;
        /// <summary>
        /// 分享数量
        /// </summary>
        public int ShareCount
        {
            get { return _shareCount; }
            set { _shareCount = value; DoPropertyChanged("ShareCount"); }
        }
        private int _commentCount;
        /// <summary>
        /// 评价数量
        /// </summary>
        public int CommentCount
        {
            get { return _commentCount; }
            set { _commentCount = value; DoPropertyChanged("CommentCount"); }
        }
        private int _likeCount;
        /// <summary>
        /// 点赞数量
        /// </summary>
        public int LikeCount
        {
            get { return _likeCount; }
            set { _likeCount = value; DoPropertyChanged("LikeCount"); }
        }
        private bool _Liked = false;

        public bool Liked
        {
            get { return _Liked; }
            set { _Liked = value; DoPropertyChanged("Liked"); }
        }

        /// <summary>
        /// 显示在昵称左侧的TAG
        /// </summary>
        public string TagName { get; set; } = "";
        public bool ShowTag { get; set; } = false;
        /// <summary>
        /// 打开用户个人中心
        /// </summary>
        public ICommand UserCommand { get; set; }
        /// <summary>
        /// 打开抽奖信息
        /// </summary>
        public ICommand LotteryCommand { get; set; }
        /// <summary>
        /// 打开链接
        /// </summary>
        public ICommand LaunchUrlCommand { get; set; }
        /// <summary>
        /// 打开网页
        /// </summary>
        public ICommand WebCommand { get; set; }
        /// <summary>
        /// 打开话题
        /// </summary>
        public ICommand TagCommand { get; set; }
        /// <summary>
        /// 打开投票信息
        /// </summary>
        public ICommand VoteCommand { get; set; }
        /// <summary>
        /// 查看图片信息
        /// </summary>
        public ICommand ImageCommand { get; set; }
        /// <summary>
        /// 保存图片
        /// </summary>
        public ICommand SaveImageCommand { get; set; }
        /// <summary>
        /// 点赞
        /// </summary>
        public ICommand LikeCommand { get; set; }
        /// <summary>
        /// 评论
        /// </summary>
        public ICommand CommentCommand { get; set; }
        /// <summary>
        /// 转发
        /// </summary>
        public ICommand RepostCommand { get; set; }
        /// <summary>
        /// 删除动态
        /// </summary>
        public ICommand DeleteCommand { get; set; }
        /// <summary>
        /// 打开详情页面
        /// </summary>
        public ICommand DetailCommand { get; set; }
        /// <summary>
        /// 添加到稍后再看
        /// </summary>
        public ICommand WatchLaterCommand { get; set; }
        /// <summary>
        /// 是否年费大会员，是的话显示粉色昵称
        /// </summary>
        public bool IsYearVip { get; set; }

        public DynamicItemDisplayOneRowInfo OneRowInfo { get; set; }
        public DyanmicItemDisplayShortVideoInfo ShortVideoInfo { get; set; }
        public List<DyanmicItemDisplayImageInfo> ImagesInfo { get; set; }
        /// <summary>
        /// 转发原动态信息
        /// </summary>
        public List<DynamicItemDisplayModel> OriginInfo { get; set; }
        /// <summary>
        /// 是否转发
        /// </summary>
        /// <summary>
        /// 模板自己把 Content(RichTextBlock) 交给 RichTextControl 渲染的类型。
        /// 一个 UIElement 只能有一个父元素，外层 ContentPresenter 若也拿到同一个实例会在布局阶段抛
        /// ArgumentException(E_INVALIDARG)，所以这里决定宿主归属，而不只是决定可见性。
        /// 图片模板全都自己渲染正文（1x1 / 2x2 / 3x3）：三张图以上的图文以前只显示纯文本 Desc，富文本链接、
        /// @ 与表情在卡片里看不到；正文只由模板这一个宿主渲染，外层不再重复挂一次。
        /// </summary>
        public bool TemplateRendersContent
        {
            get
            {
                switch (Type)
                {
                    case DynamicDisplayType.Text:
                    case DynamicDisplayType.Repost:
                    case DynamicDisplayType.Reserve:
                    case DynamicDisplayType.Photo:
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <summary>外层通用 ContentPresenter 的内容；模板渲染正文时返回 null，保证 RichTextBlock 只有一个宿主。</summary>
        public RichTextBlock PresenterContent
        {
            get
            {
                return TemplateRendersContent ? null : Content;
            }
        }

        public Visibility ContentPresenterVisibility
        {
            get
            {
                return ShowContent && PresenterContent != null ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public bool IsRepost { get; set; } = false;
        /// <summary>
        /// 是否自己动态
        /// </summary>
        public bool IsSelf { get; set; }
        /// <summary>
        /// 是否显示作者信息（关注列表的合成合集卡片没有作者）
        /// </summary>
        public bool ShowAuthorHeader { get; set; } = true;
        /// <summary>
        /// 是否显示转发、评论、点赞一行
        /// </summary>
        public bool ShowInteractionRow { get; set; } = true;
        /// <summary>
        /// 作者头像与昵称：原条件为“非转发”
        /// </summary>
        public Visibility AuthorHeaderVisibility
        {
            get { return !IsRepost && ShowAuthorHeader ? Visibility.Visible : Visibility.Collapsed; }
        }
        /// <summary>
        /// 转发来源的作者行：原条件为“转发”
        /// </summary>
        public Visibility RepostAuthorVisibility
        {
            get { return IsRepost && ShowAuthorHeader ? Visibility.Visible : Visibility.Collapsed; }
        }
        /// <summary>
        /// 互动栏：原条件为“非转发”
        /// </summary>
        public Visibility InteractionRowVisibility
        {
            get { return !IsRepost && ShowInteractionRow ? Visibility.Visible : Visibility.Collapsed; }
        }
        /// <summary>
        /// 不支持的类型没有能渲染它的页面：打开原动态要走网页。用 DetailByIdCommand 会进原生详情页，
        /// 那边同样认不出这个类型，只会再显示一张占位卡（评审 52a29f1）。
        /// 所以入口只在"有 ID 且有网页命令"时出现，否则按钮点了没反应，比没有按钮更糟。
        /// </summary>
        public bool CanOpenOriginal
        {
            get { return !string.IsNullOrEmpty(DynamicID) && WebCommand != null; }
        }
        public string DynamicUrl { get { return $"https://t.bilibili.com/{DynamicID}"; } }
    }

    public class DynamicItemDisplayOneRowInfo
    {
        public string Cover { get; set; }
        public string Url { get; set; }
        public string CoverText { get; set; }
        public string CoverParameter { get; set; } = "412w_232h_1c";
        public bool ShowCoverText
        {
            get
            {
                return !string.IsNullOrEmpty(CoverText);
            }
        }
        public string Tag { get; set; }
        public bool ShowTag
        {
            get
            {
                return !string.IsNullOrEmpty(Tag);
            }
        }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Desc { get; set; }
        public object ID { get; set; }
        public string AID { get; set; }
    }

    public class DyanmicItemDisplayShortVideoInfo
    {
        public string UploadTime { get; set; }
        public string VideoPlayurl { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
    public class DyanmicItemDisplayImageInfo
    {
        public int Index { get; set; }
        public string ImageUrl { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public List<string> AllImages { get; set; }
        public ICommand ImageCommand { get; set; }
        /// <summary>长图角标：宽高缺失时不算长图，否则每个拿不到尺寸的图都会被标成长图。</summary>
        public bool LongImage { get { return DynamicImageRules.IsLongImage(Width, Height); } }
        /// <summary>单图卡片的缩略图参数，规则集中在 Helpers/DynamicImageRules.cs。</summary>
        public string ImageUrlWithPar
        {
            get { return DynamicImageRules.WithThumbnailParams(ImageUrl, Width, Height); }
        }

        public ICommand SaveImageCommand { get; internal set; }
    }
}
