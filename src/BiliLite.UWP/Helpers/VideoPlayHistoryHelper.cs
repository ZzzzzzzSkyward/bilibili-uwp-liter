using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiliLite.Helpers
{
    public class VideoPlayHistoryHelper
    {
        private const int ABPLAY_HISTORES_MAX = 200;
        // 启动时由 App.InitBili 里的一步异步初始化赋值,播放器可能在它之前就读;先给一个空表,不让静态字段成为 null
        private static Dictionary<string, ABPlayHistoryEntry> ABPlayHistories =
            new Dictionary<string, ABPlayHistoryEntry>(ABPLAY_HISTORES_MAX);

        public class ABPlayHistoryEntry {
            public double PointA { get; set; }
            public double PointB { get; set; } = double.MaxValue;
        }

        /// <summary>
        /// 加载AB播放记录
        /// </summary>
        /// <param name="cleanup">是否顺带清理一下历史记录</param>
        public static void LoadABPlayHistories(bool cleanup)
        {
            ABPlayHistories = SettingHelper.GetValue(SettingHelper.Player.PLAYER_ABPLAY_HISTORIES, new Dictionary<string, ABPlayHistoryEntry>(ABPLAY_HISTORES_MAX))
                ?? new Dictionary<string, ABPlayHistoryEntry>(ABPLAY_HISTORES_MAX);

            if (ABPlayHistories.Count > ABPLAY_HISTORES_MAX && cleanup)
            {
                int toDelete = ABPlayHistories.Count - ABPLAY_HISTORES_MAX;

                for (int i = 0; i < toDelete; i++)
                {
                    ABPlayHistories.Remove(ABPlayHistories.Keys.First());
                }

                SettingHelper.SetValue(SettingHelper.Player.PLAYER_ABPLAY_HISTORIES, ABPlayHistories);
            }
        }

        /// <summary>
        /// 查找视频的AB播放区间
        /// </summary>
        /// <param name="videoID"></param>
        /// <returns>null 如果没有找到</returns>
        public static ABPlayHistoryEntry FindABPlayHistory(Controls.PlayInfo info)
        {
            return ABPlayHistories.GetValueOrDefault(info.season_id != 0 ? "ep" + info.ep_id : info.cid, null);
        }

        /// <summary>
        /// 设置AB播放历史
        /// </summary>
        /// <param name="info"></param>
        /// <param name="history">null移除AB播放历史</param>
        public static async void SetABPlayHistory(Controls.PlayInfo info, ABPlayHistoryEntry history)
        {
            try
            {
                var key = info.season_id != 0 ? "ep" + info.ep_id : info.cid;
                if (history == null)
                {
                    ABPlayHistories.Remove(key);
                }
                else
                {
                    ABPlayHistories[key] = history;
                }

                // 快照之后再写盘:Task.Run 里读静态字段会和下一次 Load 撞上
                var snapshot = ABPlayHistories;
                await Task.Run(() => SettingHelper.SetValue(SettingHelper.Player.PLAYER_ABPLAY_HISTORIES, snapshot));
            }
            catch (Exception ex)
            {
                // async void:写盘失败必须在这里收住,否则会走未处理异常通道把进程带崩
                LogHelper.Log("AB play history write failed", LogType.ERROR, ex);
            }
        }
    }
}
