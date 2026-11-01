using System.Collections.Generic;
using System.Collections.ObjectModel;
using BiliLite.Controls;
using BiliLite.Models.Api.Comment;
using Windows.UI.Xaml;

namespace BiliLite.Parsing.Comment
{
    /// <summary>这一页的游标属于哪一路。游标槽跟源绑定：游标源推 next_offset，pn 源由控件自己记页号。</summary>
    public enum WebCommentPageSource
    {
        /// <summary>x/v2/reply/wbi/main：游标是 cursor.pagination_reply.next_offset</summary>
        Cursor = 0,
        /// <summary>x/v2/reply：游标是 pn，响应里没有游标</summary>
        PageNumber = 1,
    }

    /// <summary>一次解析里要发布的东西：这一页的行（已按界面顺序排好）、线上 replies 的条数、下一页游标。</summary>
    public sealed class WebCommentPage
    {
        /// <summary>按界面顺序准备好的行：第一页是置顶在前，其余是这一页的 replies</summary>
        public List<CommentModel> Rows { get; set; }

        /// <summary>线上 data.replies 的条数（不是 Rows 的条数）：翻页阈值照改造前用它</summary>
        public int ReplyCount { get; set; }

        /// <summary>下一页要回传的 offset；pn 源恒为 null，服务端没给也是 null</summary>
        public string NextOffset { get; set; }
    }

    /// <summary>
    /// WebCommentPageDto → 现有页面模型（CommentModel，住 Controls/CommentControl.xaml.cs）。
    /// 置顶合并、rpid 去重、showTop 派生都只在这里做，控件拿到的是可以直接上屏的行。
    ///
    /// 合并规则（照改造前 GetComment 的 :193-236）：
    /// 1. 第一页顺序是 upper.top → top_replies（线上顺序）→ replies；三者都按 rpid 去重，先出现的胜出。
    /// 2. 第一页的置顶行 showTop = Visible（upper.top 和 top_replies 都算置顶），replies 的行不动。
    /// 3. 非第一页只有 replies，且不去重：改造前的翻页路径直接 Append，服务端翻页时也可能把同一条再发一次。
    /// 4. 游标只属于游标源：source == PageNumber 时 NextOffset 恒为 null，pn 那一页的位置由控件自己持有
    ///    （改造前 isCursorApiResponse == false 之后就不再碰 cursor）。失败的响应不会走到这里，
    ///    所以上一次成功的游标不会被清掉。
    /// 5. 模型里的 replies 集合一律给空集合：XAML 的 ListView 绑的是它，null 会在展开回复时炸。
    /// </summary>
    public static class WebCommentPageMapper
    {
        public static WebCommentPage MapWebCommentPage(WebCommentPageDto page, WebCommentPageSource source,
            bool firstPage)
        {
            var rows = new List<CommentModel>();
            if (firstPage)
            {
                if (page.Upper != null && page.Upper.Top != null)
                {
                    var top = MapWebCommentReply(page.Upper.Top);
                    top.showTop = Visibility.Visible;
                    rows.Add(top);
                }
                foreach (var reply in page.TopReplies)
                {
                    var item = MapWebCommentReply(reply);
                    item.showTop = Visibility.Visible;
                    if (!ContainsRpid(rows, item.rpid))
                    {
                        rows.Add(item);
                    }
                }
            }
            foreach (var reply in page.Replies)
            {
                var item = MapWebCommentReply(reply);
                if (!firstPage || !ContainsRpid(rows, item.rpid))
                {
                    rows.Add(item);
                }
            }

            var nextOffset = source == WebCommentPageSource.Cursor
                             && page.Cursor != null
                             && page.Cursor.PaginationReply != null
                ? page.Cursor.PaginationReply.NextOffset
                : null;
            return new WebCommentPage
            {
                Rows = rows,
                ReplyCount = page.Replies.Count,
                NextOffset = nextOffset,
            };
        }

        public static List<CommentModel> MapWebCommentReplies(WebCommentReplyListDto page)
        {
            var rows = new List<CommentModel>(page.Replies.Count);
            foreach (var reply in page.Replies)
            {
                rows.Add(MapWebCommentReply(reply));
            }
            return rows;
        }

        /// <summary>一条评论 → 页面模型。缺失的嵌套对象保持 null：界面对 null 的显隐判断是既有的。</summary>
        public static CommentModel MapWebCommentReply(WebCommentReplyDto reply)
        {
            return new CommentModel
            {
                rpid = reply.Rpid,
                rpid_str = reply.RpidStr,
                oid = reply.Oid,
                type = reply.Type,
                mid = reply.Mid,
                root = reply.Root,
                parent = reply.Parent,
                count = reply.Count,
                rcount = reply.Rcount,
                like = reply.Like,
                action = reply.Action,
                floor = reply.Floor,
                state = reply.State,
                ctime = reply.Ctime,
                member = MapMember(reply.Member),
                content = MapContent(reply.Content),
                up_action = reply.UpAction == null
                    ? null
                    : new CommentUPActionModel { like = reply.UpAction.Like, reply = reply.UpAction.Reply },
                reply_control = reply.ReplyControl == null
                    ? null
                    : new CommentReplyControlModel
                    {
                        time_desc = reply.ReplyControl.TimeDesc,
                        location = reply.ReplyControl.Location,
                    },
                replies = new ObservableCollection<CommentModel>(),
            };
        }

        private static CommentMemberModel MapMember(WebCommentMemberDto member)
        {
            if (member == null)
            {
                return null;
            }
            return new CommentMemberModel
            {
                mid = member.Mid,
                uname = member.Uname,
                sex = member.Sex,
                sign = member.Sign,
                avatar = member.Avatar,
                level_info = member.LevelInfo == null
                    ? null
                    : new CommentMemberModel { current_level = member.LevelInfo.CurrentLevel },
                fans_detail = member.FansDetail == null
                    ? null
                    : new CommentMemberFansDetailModel
                    {
                        uid = member.FansDetail.Uid,
                        medal_id = member.FansDetail.MedalId,
                        medal_name = member.FansDetail.MedalName,
                        level = member.FansDetail.Level,
                        medal_color = member.FansDetail.MedalColor,
                        medal_color_end = member.FansDetail.MedalColorEnd,
                        medal_color_border = member.FansDetail.MedalColorBorder,
                        medal_color_name = member.FansDetail.MedalColorName,
                        medal_color_level = member.FansDetail.MedalColorLevel,
                    },
                user_sailing = member.UserSailing == null
                    ? null
                    : new CommentMemberUserSailingModel
                    {
                        pendant = member.UserSailing.Pendant == null
                            ? null
                            : new CommentMemberUserSailingPendantModel
                            {
                                id = member.UserSailing.Pendant.Id,
                                name = member.UserSailing.Pendant.Name,
                                image = member.UserSailing.Pendant.Image,
                            },
                        cardbg = member.UserSailing.Cardbg == null
                            ? null
                            : new CommentMemberUserSailingCardbgModel
                            {
                                id = member.UserSailing.Cardbg.Id,
                                name = member.UserSailing.Cardbg.Name,
                                image = member.UserSailing.Cardbg.Image,
                                jump_url = member.UserSailing.Cardbg.JumpUrl,
                                fan = member.UserSailing.Cardbg.Fan == null
                                    ? null
                                    : new CommentMemberUserSailingCardbgFanModel
                                    {
                                        is_fan = member.UserSailing.Cardbg.Fan.IsFan,
                                        number = member.UserSailing.Cardbg.Fan.Number,
                                        color = member.UserSailing.Cardbg.Fan.Color,
                                        name = member.UserSailing.Cardbg.Fan.Name,
                                        num_desc = member.UserSailing.Cardbg.Fan.NumDesc,
                                    },
                            },
                    },
                pendant = member.Pendant == null
                    ? null
                    : new CommentMemberModel
                    {
                        pid = member.Pendant.Pid,
                        name = member.Pendant.Name,
                        image = member.Pendant.Image,
                    },
                official_verify = member.OfficialVerify == null
                    ? null
                    : new CommentMemberModel
                    {
                        type = member.OfficialVerify.Type,
                        desc = member.OfficialVerify.Desc,
                    },
                vip = member.Vip == null ? null : new CommentMemberModel { vipType = member.Vip.VipType },
                vipType = member.Vip == null ? 0 : member.Vip.VipType,
            };
        }

        private static CommentContentModel MapContent(WebCommentContentDto content)
        {
            if (content == null)
            {
                return null;
            }
            return new CommentContentModel
            {
                message = content.Message,
                plat = content.Plat,
                device = content.Device,
                pictures = content.Pictures,
                emote = content.Emote,
            };
        }

        private static bool ContainsRpid(List<CommentModel> rows, long rpid)
        {
            foreach (var row in rows)
            {
                if (row.rpid == rpid)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
