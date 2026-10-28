using System;
using System.Collections.Generic;
using System.Threading.Tasks;

enum Visibility { Visible }
sealed class PlayInfo { public bool is_interaction; }
sealed class InteractionInfo { public int is_leaf; }
sealed class InteractionVm { public InteractionInfo Info = new(); }
sealed class DanmuStub { public void PauseDanmaku() { } public void ClearAll() { } }
sealed class ChoiceStub { public Visibility Visibility { get; set; } }
sealed class SelectionStub { public int SelectedIndex; }
sealed class ToggleStub { public bool IsOn; }
sealed class PlayerStub { public double Duration = 100; public void Play() { } }
sealed class HistoryStub
{
    public int Calls;
    public Task ReportHistory(PlayInfo item, double progress) { Calls++; return Task.CompletedTask; }
}
static class Utils { public static void ShowMessageToast(string message) { } }
sealed class PlayerControlHarness
{
    public PlayInfo CurrentPlayItem = new();
    public InteractionVm interactionVideoVM = new();
    public DanmuStub DanmuControl = new();
    public ChoiceStub InteractionChoices = new();
    public SelectionStub PlayerSettingPlayMode = new() { SelectedIndex = 0 };
    public ToggleStub PlayerSettingAutoNext = new();
    public int CurrentPlayIndex;
    public List<PlayInfo> PlayInfos = new() { new PlayInfo() };
    public EventHandler AllMediaEndEvent;
    public bool _autoPlay;
    public PlayerStub Player = new();
    public HistoryStub playerHelper = new();
    public void ChangePlayIndex(int index) { }
    public void ClearSubTitle() { }
    public void InvokePlayerEnd() { Player_PlayMediaEnded(null, EventArgs.Empty); }
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
}

var control = new PlayerControlHarness();
control.InvokePlayerEnd();
Console.WriteLine($"History report calls on natural end: {control.playerHelper.Calls}");
var passed = control.playerHelper.Calls > 0;
Console.WriteLine(passed
    ? "PASS: natural completion reported final progress"
    : "FAIL: natural completion did not report final progress");
Environment.ExitCode = passed ? 0 : 1;
