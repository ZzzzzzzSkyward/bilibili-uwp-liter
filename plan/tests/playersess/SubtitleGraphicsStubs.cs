#pragma warning disable CS0067, CS0649, CS0414
namespace SubtitleGraphicsProbe
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    static class Check
    {
        public static void That(bool x, string m)
        {
            if (!x)
                throw new Exception("ASSERT " + m);
        }
    }
    class InjectedFault : Exception
    {
        public InjectedFault(string p) : base(p) { }
    }
    sealed class Run
    {
        public static Run Current;
        public string Fault; public InjectedFault LastFault;
        public List<Resource> Resources = new();
        public List<string> Trace = new();
        public List<string> Semantics = new();
        public TaskCompletionSource<bool> SaveGate, SourceGate;
        public CanvasDevice Device = new();
        public void Point(string p)
        {
            Trace.Add(p);
            if (Fault == p)
            {
                LastFault = new InjectedFault(p);
                throw LastFault;
            }
        }
        public void Event(string p)
        {
            Trace.Add(p);
            Semantics.Add(p);
        }
        public int Live(string kind) => Resources.Count(x => x.Kind == kind && !x.Closed);
        public int TotalLive => Resources.Count(x => !x.Closed && x.Kind != "device");
    }
    class Resource : IDisposable
    {
        public readonly Run Run;
        public string Kind;
        public bool Closed;
        public int Disposals;
        public Resource(string kind)
        {
            Run = global::SubtitleGraphicsProbe.Run.Current;
            Kind = kind;
            if (Run != null)
            {
                Run.Point("new:" + kind);
                Run.Resources.Add(this);
            }
        }
        public void Open()
        {
            Check.That(!Closed, "use-after-close " + Kind);
        }
        public virtual void Dispose()
        {
            Disposals++;
            Check.That(Disposals == 1, "double-close " + Kind);
            Closed = true;
            Run?.Trace.Add("close:" + Kind);
        }
    }
    class CanvasDevice : Resource
    {
        public CanvasDevice() : base("device") { }
        public static CanvasDevice GetSharedDevice()
        {
            Run.Current.Event("device:shared");
            return Run.Current.Device;
        }
        public void BorrowedConsumer()
        {
            Open();
            Run?.Trace.Add("device:borrowed-consumer");
        }
    }
    class CanvasTextFormat : Resource
    {
        float fontSize; CanvasHorizontalAlignment alignment; string fontWeight = "Normal";
        public float FontSize
        {
            get => fontSize; set
            {
                Run.Point("format:size");
                fontSize = value;
            }
        }
        public CanvasHorizontalAlignment HorizontalAlignment
        {
            get => alignment; set
            {
                Run.Point("format:align");
                alignment = value;
            }
        }
        public string FontWeight
        {
            get => fontWeight; set
            {
                Run.Point("format:bold");
                fontWeight = value;
            }
        }
        public CanvasTextFormat() : base("format") { }
    }
    class CanvasTextLayout : Resource
    {
        public string Text; public CanvasTextFormat Format; public float W, H;
        public CanvasTextLayout(CanvasDevice d, string t, CanvasTextFormat f, float w, float h) : base("layout") { d.Open(); f.Open(); Text = t; Format = f; W = w; H = h; Run.Event($"layout:{t.Replace("\n", "\\n")}|{f.FontSize}|{f.HorizontalAlignment}|{f.FontWeight}|{w}|{h}"); }
    }
    class CanvasGeometry : Resource
    {
        public CanvasTextLayout Layout;
        CanvasGeometry(CanvasTextLayout l) : base("geometry") { l.Open(); Layout = l; }
        public static CanvasGeometry CreateText(CanvasTextLayout l) => new(l);
    }
    class CanvasStrokeStyle : Resource
    {
        CanvasDashStyle dash; public CanvasDashStyle DashStyle
        {
            get => dash; set
            {
                Run.Point("style:dash");
                dash = value;
            }
        }
        public CanvasStrokeStyle() : base("style") { }
    }
    class CanvasRenderTarget : Resource
    {
        public bool DrawingOpen, Drawn; public float W, H, Dpi; public string Payload;
        public CanvasRenderTarget(CanvasDevice d, float w, float h, float dpi) : base("target") { d.Open(); W = w; H = h; Dpi = dpi; Run.Event($"target:{w}|{h}|{dpi}"); }
        public CanvasDrawingSession CreateDrawingSession()
        {
            Open();
            DrawingOpen = true;
            return new(this);
        }
        public async Task SaveAsync(InMemoryRandomAccessStream s, CanvasBitmapFileFormat f, float q)
        {
            Open();
            s.Open();
            Check.That(!DrawingOpen, "save before session close");
            Run.Point("save:start");
            Run.Event($"save:{f}|{q}");
            if (Run.SaveGate != null)
                await Run.SaveGate.Task;
            Open();
            s.Open();
            Run.Point("save:complete");
            s.Payload = Payload;
        }
    }
    class CanvasDrawingSession : Resource
    {
        CanvasRenderTarget Target;
        public CanvasDrawingSession(CanvasRenderTarget t) : base("drawing") { Target = t; }
        public void Clear(string c)
        {
            Open();
            Target.Open();
            Run.Point("draw:clear");
            Run.Event("clear:" + c);
        }
        public void DrawGeometry(CanvasGeometry g, string c, float width, CanvasStrokeStyle style)
        {
            Open();
            g.Open();
            style.Open();
            Run.Point("draw:stroke");
            Run.Event($"stroke:{c}|{width}|{style.DashStyle}");
            Target.Payload = g.Layout.Text;
        }
        public void FillGeometry(CanvasGeometry g, string c)
        {
            Open();
            g.Open();
            Run.Point("draw:fill");
            Run.Event("fill:" + c);
            Target.Drawn = true;
        }
        public override void Dispose()
        {
            base.Dispose();
            Target.DrawingOpen = false;
            Run.Event("drawing:closed");
            Run.Point("close:drawing:fault");
        }
    }
    class InMemoryRandomAccessStream : Resource
    {
        public string Payload; public InMemoryRandomAccessStream() : base("stream") { }
    }
    class BitmapImage
    {
        public string Payload; public bool Loaded;
        public async Task SetSourceAsync(InMemoryRandomAccessStream s)
        {
            s.Open();
            Run.Current.Point("source:start");
            Run.Current.Event("source:read");
            if (Run.Current.SourceGate != null)
                await Run.Current.SourceGate.Task;
            s.Open();
            Run.Current.Point("source:complete");
            Payload = s.Payload;
            Loaded = true;
        }
    }
    class TextBlock
    {
        public string Text, FontWeight = "Normal"; public float FontSize; public TextAlignment TextAlignment; public Size DesiredSize;
        public void Measure(Size available)
        {
            Run.Current.Point("measure");
            Check.That(double.IsPositiveInfinity(available.Width) && double.IsPositiveInfinity(available.Height), "measure input");
            var lines = Text.Split('\n');
            DesiredSize = new Size(lines.Max(x => x.Length) * FontSize / 2, lines.Length * (FontSize + 4));
            Run.Current.Event($"measure:{TextAlignment}|{FontWeight}|{FontSize}");
        }
    }
    record struct Size(double Width, double Height);
    enum CanvasHorizontalAlignment
    {
        Center, Left, Right
    }
    enum TextAlignment
    {
        Center, Left, Right
    }
    enum CanvasDashStyle
    {
        Solid
    }
    enum CanvasBitmapFileFormat
    {
        Png
    }
    enum Stretch
    {
        Uniform
    }
    static class FontWeights
    {
        public const string Bold = "Bold";
    }
    static class Colors
    {
        public const string Transparent = "transparent";
    }
    static class Utils
    {
        public static string ToColor(string v)
        {
            Run.Current.Point("color");
            return v;
        }
    }
    class Image
    {
        double width;
        public double Width
        {
            get => width; set
            {
                Run.Current.Point("output:width");
                width = value;
            }
        }
        public BitmapImage Source;
        public Stretch Stretch;
    }
    class ChildList : List<Image>
    {
        public new void Add(Image image)
        {
            Run.Current.Point("output:add");
            base.Add(image);
        }
    }
    class Grid
    {
        public object Tag; public ChildList Children = new();
    }
    class ComboBoxItem
    {
        public object Tag;
    }
    class Setting
    {
        public double Value; public object SelectedItem; public int SelectedIndex; public bool IsOn;
    }
    class DisplayInformation
    {
        public float LogicalDpi = 144;
    }
    class FixtureOwner
    {
        public Setting SubtitleSettingSize = new() { Value = 30 };
        public Setting SubtitleSettingColor = new() { SelectedItem = new ComboBoxItem { Tag = "#white" } };
        public Setting SubtitleSettingBorderColor = new() { SelectedItem = new ComboBoxItem { Tag = "#black" } };
        public Setting SubtitleSettingAlign = new();
        public Setting SubtitleSettingBold = new();
        public DisplayInformation displayInformation = new();
        public void Configure(int i)
        {
            SubtitleSettingSize.Value = 18 + i % 7;
            SubtitleSettingAlign.SelectedIndex = i % 3;
            SubtitleSettingBold.IsOn = i % 2 == 0;
            displayInformation.LogicalDpi = i % 2 == 0 ? 96 : 144;
        }
    }

}