using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiliLite.Helpers
{
    public class VideoPlayHistoryHelper
    {
        private const int ABPLAY_HISTORES_MAX = 200;
        private static readonly object ABPlayHistoryLock = new object();
        private static bool _abPlayHistoriesInitialized;
        private static Task _pendingABPlayWrite = Task.CompletedTask;
        // 首次访问先读持久化记录，启动时的延迟 Load 不再覆盖已经接受的修改。
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
            lock (ABPlayHistoryLock)
            {
                EnsureABPlayHistoriesInitialized();
                if (ABPlayHistories.Count > ABPLAY_HISTORES_MAX && cleanup)
                {
                    int toDelete = ABPlayHistories.Count - ABPLAY_HISTORES_MAX;
                    for (int i = 0; i < toDelete; i++)
                        ABPlayHistories.Remove(ABPlayHistories.Keys.First());
                    QueueABPlayHistoryWrite();
                }
            }
        }

        private static void EnsureABPlayHistoriesInitialized()
        {
            if (_abPlayHistoriesInitialized) return;
            ABPlayHistories = SettingHelper.GetValue(SettingHelper.Player.PLAYER_ABPLAY_HISTORIES,
                new Dictionary<string, ABPlayHistoryEntry>(ABPLAY_HISTORES_MAX))
                ?? new Dictionary<string, ABPlayHistoryEntry>(ABPLAY_HISTORES_MAX);
            _abPlayHistoriesInitialized = true;
        }

        // 调用方持有状态锁：保留内存条目的身份，只给写盘队列复制键和值。
        private static Task QueueABPlayHistoryWrite()
        {
            var snapshot = ABPlayHistories.ToDictionary(pair => pair.Key, pair => pair.Value == null ? null :
                new ABPlayHistoryEntry { PointA = pair.Value.PointA, PointB = pair.Value.PointB });
            _pendingABPlayWrite = SaveABPlayHistoriesAsync(_pendingABPlayWrite, snapshot);
            return _pendingABPlayWrite;
        }

        private static async Task SaveABPlayHistoriesAsync(Task previous, Dictionary<string, ABPlayHistoryEntry> snapshot)
        {
            try
            {
                await previous.ConfigureAwait(false);
                await Task.Run(() => SettingHelper.SetValue(SettingHelper.Player.PLAYER_ABPLAY_HISTORIES, snapshot)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // 每次写入自行处理失败，后续已接受的快照仍可继续写入。
                LogHelper.Log("AB play history write failed", LogType.ERROR, ex);
            }
        }

        /// <summary>
        /// 查找视频的AB播放区间
        /// </summary>
        /// <param name="videoID"></param>
        /// <returns>null 如果没有找到</returns>
        public static ABPlayHistoryEntry FindABPlayHistory(Controls.PlayInfo info)
        {
            lock (ABPlayHistoryLock)
            {
                EnsureABPlayHistoriesInitialized();
                return ABPlayHistories.GetValueOrDefault(info.season_id != 0 ? "ep" + info.ep_id : info.cid, null);
            }
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
                Task write;
                lock (ABPlayHistoryLock)
                {
                    EnsureABPlayHistoriesInitialized();
                    var key = info.season_id != 0 ? "ep" + info.ep_id : info.cid;
                    if (history == null)
                        ABPlayHistories.Remove(key);
                    else
                        ABPlayHistories[key] = history;
                    write = QueueABPlayHistoryWrite();
                }
                await write;
            }
            catch (Exception ex)
            {
                // async void:写盘失败必须在这里收住,否则会走未处理异常通道把进程带崩
                LogHelper.Log("AB play history write failed", LogType.ERROR, ex);
            }
        }
    }
}
