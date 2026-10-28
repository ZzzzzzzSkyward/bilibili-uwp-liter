        private async void Player_PlayMediaEnded(object sender, EventArgs e)
        {
            if (CurrentPlayItem.is_interaction)
            {
                if (interactionVideoVM.Info.is_leaf == 1)
                {
                    Utils.ShowMessageToast("播放完毕，请点击右下角节点，重新开始");
                    return;
                }
                DanmuControl.PauseDanmaku();
                InteractionChoices.Visibility = Visibility.Visible;
                return;
            }
            //await playerHelper.ReportHistory(CurrentPlayItem, Player.Duration);
            //列表顺序播放
            if (PlayerSettingPlayMode.SelectedIndex == 0)
            {
                if (CurrentPlayIndex == PlayInfos.Count - 1)
                {
                    if (AllMediaEndEvent != null)
                    {
                        AllMediaEndEvent?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        Utils.ShowMessageToast("播放完毕");
                    }
                }
                else
                {
                    if (PlayerSettingAutoNext.IsOn)
                    {
                        _autoPlay = true;
                        ChangePlayIndex(CurrentPlayIndex + 1);
                    }
                    else
                    {
                        Utils.ShowMessageToast("本P播放完成");
                    }
                }
                return;
            }
            //单P循环
            if (PlayerSettingPlayMode.SelectedIndex == 1)
            {
                ClearSubTitle();
                DanmuControl.ClearAll();
                Player.Play();
                return;
            }
            //列表循环播放
            if (PlayerSettingPlayMode.SelectedIndex == 2)
            {
                if (!PlayerSettingAutoNext.IsOn)
                {
                    Utils.ShowMessageToast("本P播放完成");
                    return;
                }
                //只有一P,重新播放
                if (PlayInfos.Count == 1)
                {
                    ClearSubTitle();
                    DanmuControl.ClearAll();
                    Player.Play();
                    return;
                }
                _autoPlay = true;
                if (CurrentPlayIndex == PlayInfos.Count - 1)
                {
                    ChangePlayIndex(0);
                }
                else
                {
                    ChangePlayIndex(CurrentPlayIndex + 1);
                }
                return;
            }
        }
