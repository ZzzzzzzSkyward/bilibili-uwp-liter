        public async void Dispose()
        {
            if (CurrentPlayItem != null)
            {
                SettingHelper.SetValue<double>(CurrentPlayItem.season_id != 0 ? "ep" + CurrentPlayItem.ep_id : CurrentPlayItem.cid, Player.Position);
                //当视频播放结束的话，Position为0
                if (Player.PlayState != PlayState.End)
                    await playerHelper.ReportHistory(CurrentPlayItem, Player.Position);
            }

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
        }
