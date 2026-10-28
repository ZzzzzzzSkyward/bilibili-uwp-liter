using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using BiliLite.Models.LocalMedia;
using BiliLite.Modules.Player;
using BiliLite.Services.LocalMedia;
using NSDanmaku.Model;
using Windows.Storage;

internal static class LM05
{
    private static int assertions;

    private static void Check(bool condition, string description)
    {
        assertions++;
        if (!condition) throw new Exception("LM-05 failed: " + description);
    }

    private static LocalMediaPlaybackTrack Track(string id, string path)
    {
        return new LocalMediaPlaybackTrack { Track = new LocalMediaTrack { Id = id, Path = path }, File = new StorageFile(path) };
    }

    private static async Task Main()
    {
        var srt = SubtitleTrackParser.Parse("1\r\n00:00:01,250 --> 00:00:03,500\r\nHello <i>world</i> &amp; all\r\nsecond line\r\n\r\n2\r\n00:00:04,000 --> 00:00:05,000\r\nNext", "srt");
        Check(srt.body.Count == 2 && srt.body[0].from == 1.25 && srt.body[0].to == 3.5, "SRT timestamps and cue conversion");
        Check(srt.body[0].content == "Hello world & all\nsecond line", "SRT multiline text and inline markup");
        Check(SubtitleTrackParser.FindCue(srt, 1.25) == srt.body[0] && SubtitleTrackParser.FindCue(srt, 3.5) == srt.body[0], "player timer cue boundary selection");
        Check(SubtitleTrackParser.FindCue(srt, 3.501) == null, "player timer hides when no cue is active");

        var vtt = SubtitleTrackParser.Parse("\uFEFFWEBVTT\n\nNOTE this is not a cue\n00:00:01.000 --> 00:00:02.000\n\nSTYLE\n::cue { color: red; }\n\nREGION\nid:foo\n\ncue-id\n00:00:02.000 --> 00:00:03.125 align:start\n<v Bob>Hello &amp; goodbye</v>\n", "vtt");
        Check(vtt.body.Count == 1 && vtt.body[0].from == 2 && vtt.body[0].to == 3.125, "VTT BOM, metadata blocks, cue ids, and settings");
        Check(vtt.body[0].content == "Hello & goodbye", "VTT tags and entities render as readable text");
        var literalAngles = SubtitleTrackParser.Parse("WEBVTT\n\n00:00:00.000 --> 00:00:01.000\n2 < 4 > 1", "vtt");
        Check(literalAngles.body[0].content == "2 < 4 > 1", "VTT preserves ordinary angle bracket text outside recognized markup");

        var json = SubtitleTrackParser.Parse("{\"body\":[{\"from\":0.5,\"to\":1.5,\"content\":\"a < b &amp; c\"}]}", "bilibili-json");
        Check(json.body.Count == 1 && json.body[0].content == "a < b &amp; c", "Bilibili JSON preserves literal cue text");
        ExpectFormat(() => SubtitleTrackParser.Parse("00:00:61,000 --> 00:01:02,000\nBad", "srt"), "invalid seconds");
        ExpectFormat(() => SubtitleTrackParser.Parse("unknown nonempty data", "vtt"), "unknown nonempty subtitle input");
        ExpectFormat(() => SubtitleTrackParser.Parse("null", "bilibili-json"), "null Bilibili JSON");
        ExpectFormat(() => SubtitleTrackParser.Parse("{\"body\":[{\"from\":2,\"to\":1,\"content\":\"bad\"}]}", "bilibili-json"), "invalid JSON cue bounds");

        var tracks = new[] { Track("a", "a.xml"), Track("b", "b.xml") };
        var merged = await DanmakuTrackLoader.LoadAsync(tracks, file => Task.FromResult(file.Path == "a.xml"
            ? new List<DanmakuModel> { new DanmakuModel { time_s = 4 }, new DanmakuModel { time_s = 1 } }
            : new List<DanmakuModel> { new DanmakuModel { time_s = 2 } }));
        Check(merged.Select(item => item.time_s).SequenceEqual(new[] { 1, 2, 4 }), "enabled XML track merge sorts existing parser output by timestamp");
        var selectedOnly = await DanmakuTrackLoader.LoadAsync(new[] { tracks[1] }, file => Task.FromResult(new List<DanmakuModel> { new DanmakuModel { time_s = file.Path == "b.xml" ? 8 : 99 } }));
        Check(selectedOnly.Count == 1 && selectedOnly[0].time_s == 8, "individual danmaku toggle loads only the enabled track subset");
        Check((await DanmakuTrackLoader.LoadAsync(new LocalMediaPlaybackTrack[0], file => Task.FromResult(new List<DanmakuModel>()))).Count == 0, "disabling all danmaku tracks produces an empty pool");
        try
        {
            await DanmakuTrackLoader.LoadAsync(new[] { tracks[1] }, file => throw new FormatException("bad XML"));
            throw new Exception("Expected track parsing failure.");
        }
        catch (LocalMediaTrackLoadException exception)
        {
            Check(exception.Stage == "danmaku-parse" && exception.TrackId == "b" && exception.InnerException is FormatException, "danmaku failures retain stage, track id, and full inner exception");
        }

        var guard = new LocalMediaAsyncLoadGuard();
        var currentItem = new object();
        var firstTicket = guard.Begin(currentItem);
        var pending = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var applied = "";
        var oldSubtitleLoad = guard.LoadAndApplyAsync(firstTicket, currentItem, () => pending.Task, value => applied = value);
        var offTicket = guard.Begin(currentItem);
        pending.SetResult("subtitle from old selection");
        Check(!await oldSubtitleLoad && applied == "", "subtitle load cannot restore text after selecting off or another track");
        Check(!guard.TryApply(firstTicket, currentItem, () => applied = "stale rendered visual"), "asynchronous subtitle render cannot commit after a newer selection");

        var oldItemTicket = guard.Begin(currentItem);
        var oldItemPending = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var oldItemLoad = guard.LoadAndApplyAsync(oldItemTicket, currentItem, () => oldItemPending.Task, value => applied = value);
        var nextItem = new object();
        guard.Begin(nextItem);
        oldItemPending.SetResult("subtitle from previous episode");
        Check(!await oldItemLoad && applied == "", "subtitle load cannot apply after the player changes items");

        var disposeTicket = guard.Begin(nextItem);
        var disposePending = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var disposeLoad = guard.LoadAndApplyAsync(disposeTicket, nextItem, () => disposePending.Task, value => applied = value);
        guard.Invalidate();
        disposePending.SetResult("subtitle after dispose");
        Check(!await disposeLoad && applied == "", "subtitle load cannot apply after disposal");

        var oldDanmakuTicket = guard.Begin(nextItem);
        var danmakuPending = new TaskCompletionSource<List<DanmakuModel>>(TaskCreationOptions.RunContinuationsAsynchronously);
        List<DanmakuModel> activeDanmaku = null;
        var oldDanmakuLoad = guard.LoadAndApplyAsync(oldDanmakuTicket, nextItem, () => danmakuPending.Task, value => activeDanmaku = value);
        guard.Begin(nextItem); // a rapid toggle starts a newer selected-track load
        danmakuPending.SetResult(new List<DanmakuModel> { new DanmakuModel { time_s = 99 } });
        Check(!await oldDanmakuLoad && activeDanmaku == null, "old danmaku toggle load cannot replace the newest track selection");
        var activeTicket = guard.Begin(nextItem);
        Check(guard.TryApply(activeTicket, nextItem, () => activeDanmaku = new List<DanmakuModel>()) && activeDanmaku.Count == 0,
            "newest danmaku selection can commit an empty disabled-track pool");

        Console.WriteLine("LM-05 actual-source parser, subtitle load/render guards, and danmaku merge/toggle checks passed: " + assertions);
    }

    private static void ExpectFormat(Action action, string description)
    {
        try { action(); }
        catch (FormatException) { assertions++; return; }
        throw new Exception("Expected format failure: " + description);
    }
}
