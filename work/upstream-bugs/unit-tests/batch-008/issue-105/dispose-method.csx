        public async void Dispose()
        {
            var historyItem = CurrentPlayItem;
            var historyPosition = Player.Position;
            var shouldReportHistory = historyItem != null && _hasPlayedCurrentItem && Player.PlayState != PlayState.End;
            if (historyItem != null)
                SettingHelper.SetValue<double>(historyItem.season_id != 0 ? "ep" + historyItem.ep_id : historyItem.cid, historyPosition);

            Player.PlayStateChanged -= Player_PlayStateChanged;
            Player.PlayMediaEnded -= Player_PlayMediaEnded;
            Player.PlayMediaError -= Player_PlayMediaError;
            Player.ChangeEngine -= Player_ChangeEngine;
            //Player.PlayBufferEnd -= Player_PlayBufferEnd;
            //Player.PlayBufferStart -= Player_PlayBufferStart;
            //Player.PlayBuffering -= Player_PlayBuffering;
            Player.Dispose();
            if (danmuTimer != null)
            {
                danmuTimer.Stop();
                danmuTimer = null;
            }
            danmakuPool = null;
            if (dispRequest != null)
            {
                dispRequest = null;
            }
            //当视频播放结束的话，Position为0
            if (shouldReportHistory)
                await playerHelper.ReportHistory(historyItem, historyPosition);
        }
