using System;

sealed class PlayInfo
{
    public long season_id;
    public long ep_id;
    public long cid;
}

sealed class PlayerControlHarness
{
    public PlayInfo CurrentPlayItem;
    public bool _hasPlayedCurrentItem;
    void Select(PlayInfo selectedPlayItem)
    {
        __IDENTITY_RESET__
        CurrentPlayItem = selectedPlayItem;
    }
    void StateChanged(PlayState e)
    {
        __PLAYING_MARKER__
    }
    enum PlayState { Pause, Playing }
    public void SelectItem(PlayInfo item) => Select(item);
    public void SetState(bool playing) => StateChanged(playing ? PlayState.Playing : PlayState.Pause);
}

var sameItem = new PlayerControlHarness();
sameItem.SelectItem(new PlayInfo { season_id = 1, ep_id = 2, cid = 3 });
sameItem._hasPlayedCurrentItem = true;
sameItem.SelectItem(new PlayInfo { season_id = 1, ep_id = 2, cid = 3 });
var refreshPreserved = sameItem._hasPlayedCurrentItem;
var changedSeason = new PlayerControlHarness();
changedSeason.SelectItem(new PlayInfo { season_id = 1, ep_id = 2, cid = 3 });
changedSeason._hasPlayedCurrentItem = true;
changedSeason.SelectItem(new PlayInfo { season_id = 4, ep_id = 2, cid = 3 });
var seasonReset = !changedSeason._hasPlayedCurrentItem;
var changedEpisode = new PlayerControlHarness();
changedEpisode.SelectItem(new PlayInfo { season_id = 1, ep_id = 2, cid = 3 });
changedEpisode._hasPlayedCurrentItem = true;
changedEpisode.SelectItem(new PlayInfo { season_id = 1, ep_id = 5, cid = 3 });
var episodeReset = !changedEpisode._hasPlayedCurrentItem;
var changedCid = new PlayerControlHarness();
changedCid.SelectItem(new PlayInfo { season_id = 0, ep_id = 0, cid = 3 });
changedCid._hasPlayedCurrentItem = true;
changedCid.SelectItem(new PlayInfo { season_id = 0, ep_id = 0, cid = 6 });
var cidReset = !changedCid._hasPlayedCurrentItem;
var marker = new PlayerControlHarness();
marker.SetState(false);
var pauseUnchanged = !marker._hasPlayedCurrentItem;
marker.SetState(true);
var playingMarked = marker._hasPlayedCurrentItem;
Console.WriteLine($"Same-item refresh preserves played marker: {refreshPreserved}");
Console.WriteLine($"Season, episode, and CID changes reset marker: {seasonReset && episodeReset && cidReset}");
Console.WriteLine($"Playing event marks item while pause does not: {playingMarked && pauseUnchanged}");
var passed = refreshPreserved && seasonReset && episodeReset && cidReset && playingMarked && pauseUnchanged;
Console.WriteLine(passed ? "PASS: extracted selection and state blocks track actual play items" : "FAIL: played marker lifecycle diverged");
Environment.ExitCode = passed ? 0 : 1;
