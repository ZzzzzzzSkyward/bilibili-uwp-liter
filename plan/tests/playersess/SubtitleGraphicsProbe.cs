namespace SubtitleGraphicsProbe
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    static class GraphicsLifetimeProbe
    {
        static readonly string[] Owned = { "format", "target", "layout", "geometry", "style" };
        static string Text(int i) => i % 2 == 0 ? $"字幕 {i}\nsecond line" : $"cue {i}";
        static Run Start(string fault = null)
        {
            Run.Current = null;
            var r = new Run { Fault = fault };
            Run.Current = r;
            return r;
        }
        static async Task<Grid> Render(bool green, int i)
        {
            if (green)
            {
                var owner = new ActualScoped();
                owner.Configure(i);
                return await owner.Render(Text(i));
            }
            else
            {
                var owner = new ActualOriginal();
                owner.Configure(i);
                return await owner.Render(Text(i));
            }
        }
        static readonly List<string> cleanupFailures = new List<string>();
        static void CheckCleanup(Run r)
        {
            try { Clean(r); } catch (Exception error) { cleanupFailures.Add(error.Message); }
        }
        static void Clean(Run r)
        {
            var expected = r.Resources.Where(x => Owned.Contains(x.Kind)).Reverse().Select(x => "close:" + x.Kind);
            var actual = r.Trace.Where(x => x.StartsWith("close:") && Owned.Contains(x.Substring(6)));
            Check.That(expected.SequenceEqual(actual), "owned reverse release order");
            foreach (var x in r.Resources)
                Check.That(x.Closed && x.Disposals == 1, "not released " + x.Kind);
            Check.That(!r.Device.Closed && r.Device.Disposals == 0, "borrowed device closed");
            r.Device.BorrowedConsumer();
        }
        static void Output(Grid g, int i)
        {
            var expected = " " + Text(i).Replace("\n", " \n ") + " ";
            Check.That((string)g.Tag == expected, "padded tag");
            Check.That(g.Children.Count == 1, "image count");
            var im = g.Children[0];
            Check.That(im.Source.Loaded && im.Source.Payload == expected, "returned bitmap unavailable");
            Check.That(im.Width == expected.Split('\n').Max(x => x.Length) * (18 + i % 7) / 2.0, "output width");
            Check.That(im.Stretch == Stretch.Uniform, "stretch");
        }
        static async Task Fifty(bool green, bool desiredClean)
        {
            var outputs = new List<Grid>();
            var runs = new List<Run>();
            var totals = Owned.ToDictionary(x => x, _ => 0);
            for (int i = 0; i < 50; i++)
            {
                var r = Start();
                var g = await Render(green, i);
                outputs.Add(g);
                runs.Add(r);
                Output(g, i);
                foreach (var k in Owned)
                    totals[k] += r.Live(k);
                Check.That(r.Resources.Single(x => x.Kind == "drawing").Disposals == 1, "drawing close");
                Check.That(r.Resources.Single(x => x.Kind == "stream").Disposals == 1, "stream close");
                if (!green)
                {
                    foreach (var k in Owned)
                        Check.That(r.Live(k) == 1, "baseline evidence " + k);
                    r.Device.BorrowedConsumer();
                }
            }
            foreach (var pair in outputs.Select((g, i) => (g, i)))
                Output(pair.g, pair.i);
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                caseName = green ? "current-source-50" : "original-50",
                renders = 50,
                undisposed = totals,
                outputsUsable = outputs.Count,
                deviceDisposals = 0
            }));
            if (desiredClean)
                Check.That(totals.Values.All(x => x == 0), "EXPECTED_CLEAN_FAILED after50 completed renders");
            if (green) foreach (var run in runs) CheckCleanup(run);
        }
        static async Task Order(bool green)
        {
            var r = Start();
            r.SaveGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
            r.SourceGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
            var task = Render(green, 2);
            Check.That(!task.IsCompleted, "save not awaited");
            Check.That(r.Resources.Single(x => x.Kind == "drawing").Closed, "drawing must close before save");
            foreach (var k in Owned)
                Check.That(r.Live(k) == 1, "early close at save " + k);
            Check.That(r.Live("stream") == 1, "stream early close at save");
            r.Device.BorrowedConsumer();
            r.SaveGate.SetResult(true);
            for (int j = 0; j < 1000 && !r.Trace.Contains("source:start"); j++)
                await Task.Delay(1);
            Check.That(r.Trace.Contains("source:start") && !task.IsCompleted, "SetSourceAsync not awaited");
            foreach (var k in Owned)
                Check.That(r.Live(k) == 1, "early close at source " + k);
            Check.That(r.Live("stream") == 1, "stream early close at source");
            r.SourceGate.SetResult(true);
            Output(await task, 2);
            if (green)
                CheckCleanup(r);
            Check.That(r.Semantics.IndexOf("drawing:closed") < r.Semantics.IndexOf("save:Png|1"), "save order");
            Console.WriteLine($"ORDER {(green ? "current-source" : "original")} PASS");
        }
        static async Task Faults(bool green)
        {
            string[] points = { "color", "new:format", "format:size", "format:align", "format:bold", "measure", "new:target", "new:layout", "new:geometry", "new:drawing", "draw:clear", "new:style", "style:dash", "draw:stroke", "draw:fill", "close:drawing:fault", "new:stream", "save:start", "save:complete", "source:start", "source:complete", "output:width", "output:add" };
            int leaks = 0;
            foreach (var p in points)
            {
                var r = Start(p);
                try
                {
                    await Render(green, 2);
                    throw new Exception("fault not triggered " + p);
                }
                catch (InjectedFault e) { Check.That(e.Message == p && ReferenceEquals(e, r.LastFault), "wrong fault identity"); }
                leaks += r.TotalLive;
                if (green)
                    CheckCleanup(r);
                Check.That(!r.Device.Closed, "fault closed device");
                foreach (var x in r.Resources.Where(x => x.Kind == "stream"))
                    Check.That(x.Closed, "fault stream open");
                foreach (var x in r.Resources.Where(x => x.Kind == "drawing"))
                    Check.That(x.Closed, "fault drawing open");
            }
            Console.WriteLine($"FAULT {(green ? "current-source" : "original")} cases={points.Length} undisposedTotal={leaks} COMPLETE");
        }
        static async Task AsyncFaults(bool green)
        {
            foreach (var at in new[] { "save:complete", "source:complete" })
            {
                var r = Start(at);
                var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                if (at.StartsWith("save"))
                    r.SaveGate = gate;
                else
                    r.SourceGate = gate;
                var pending = Render(green, 3);
                Check.That(!pending.IsCompleted, "async fault not suspended");
                foreach (var k in Owned)
                    Check.That(r.Live(k) == 1, "async fault premature close " + k);
                Check.That(r.Live("stream") == 1, "async fault stream not alive");
                gate.SetResult(true);
                try
                {
                    await pending;
                    throw new Exception("async fault missing");
                }
                catch (InjectedFault e) { Check.That(e.Message == at && ReferenceEquals(e, r.LastFault), "async failure identity"); }
                if (green)
                    CheckCleanup(r);
                else
                    Check.That(Owned.All(k => r.Live(k) == 1), "baseline async fault evidence");
                Check.That(r.Resources.Single(x => x.Kind == "stream").Closed, "async fault stream close");
            }
            Console.WriteLine($"ASYNC_FAULT {(green ? "current-source" : "original")} cases=2 COMPLETE");
        }
        static async Task Compare()
        {
            for (int i = 0; i < 50; i++)
            {
                var before = Start();
                var old = await Render(false, i);
                var after = Start();
                var current = await Render(true, i);
                Check.That(before.Semantics.SequenceEqual(after.Semantics), "render operation/parameter change at " + i);
                Check.That(before.Trace.Where(x => !x.StartsWith("close:")).SequenceEqual(after.Trace.Where(x => !x.StartsWith("close:"))), "non-disposal allocation/render/error checkpoint order change at " + i);
                Check.That(JsonSerializer.Serialize(new
                {
                    old.Tag,
                    old.Children[0].Width,
                    old.Children[0].Source.Payload,
                    old.Children[0].Stretch
                }) == JsonSerializer.Serialize(new
                {
                    current.Tag,
                    current.Children[0].Width,
                    current.Children[0].Source.Payload,
                    current.Children[0].Stretch
                }), "output contract change at " + i);
                CheckCleanup(after);
            }
            Console.WriteLine("PARAMETER_OUTPUT_COMPARISON renders=50 COMPLETE (stub contract, not pixel output)");
        }
        public static async Task<int> RunCases()
        {
            cleanupFailures.Clear();
            int failedStages = 0;
            async Task Stage(string name, Func<Task> action)
            {
                int before = cleanupFailures.Count;
                try
                {
                    await action();
                    Check.That(cleanupFailures.Count == before, "cleanup failures=" + (cleanupFailures.Count - before));
                    Console.WriteLine("GRAPHICS_STAGE " + name + " PASS");
                }
                catch (Exception error)
                {
                    failedStages++;
                    Console.Error.WriteLine("GRAPHICS_STAGE " + name + " FAIL " + error.Message);
                }
            }
            await Stage("50-render-returned-output", () => Fifty(true, true));
            await Stage("suspended-save-source-order", () => Order(true));
            await Stage("23-fault-boundaries", () => Faults(true));
            await Stage("2-suspended-async-faults", () => AsyncFaults(true));
            await Stage("50-parameter-output-comparisons", Compare);
            if (failedStages != 0) return 1;
            Console.WriteLine("GRAPHICS OWNERSHIP MODEL PASS; native allocation/finalization/pixels unmeasured");
            return 0;
        }
    }

}