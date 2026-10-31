using System;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Net;
using BiliLite.Modules;
using Newtonsoft.Json;

namespace BiliLite.Services.LocalMedia
{
    /// <summary>Adapts the manifest subtitle formats to the player's existing cue model.</summary>
    public static class SubtitleTrackParser
    {
        private static readonly Regex Timestamp = new Regex(@"^(?:(\d+):)?(\d{2}):(\d{2})[,.](\d{1,3})$", RegexOptions.Compiled);
        private static readonly Regex VttMarkup = new Regex(@"</?(?:c(?:\.[\w-]+)?|i|b|u|ruby|rt|v(?:\s+[^>]*)?|lang(?:\s+[^>]*)?|br)\s*/?>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static SubtitleModel Parse(string text, string format)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new FormatException("Subtitle file is empty.");
            text = text.TrimStart('\uFEFF');
            if (string.Equals(format, "bilibili-json", StringComparison.OrdinalIgnoreCase))
            {
                var parsed = JsonConvert.DeserializeObject<SubtitleModel>(text);
                if (parsed?.body == null) throw new FormatException("Bilibili subtitle JSON has no body cue list.");
                return Normalize(parsed, stripMarkup: false);
            }
            var isVtt = string.Equals(format, "vtt", StringComparison.OrdinalIgnoreCase);
            if (!string.Equals(format, "srt", StringComparison.OrdinalIgnoreCase) && !isVtt)
                throw new NotSupportedException("Unsupported local subtitle format: " + format);

            var model = new SubtitleModel { body = new System.Collections.Generic.List<SubtitleItemModel>() };
            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith("WEBVTT", StringComparison.OrdinalIgnoreCase)) continue;
                if (isVtt && (line.StartsWith("NOTE", StringComparison.OrdinalIgnoreCase) || line == "STYLE" || line == "REGION"))
                {
                    while (++i < lines.Length && !string.IsNullOrWhiteSpace(lines[i])) { }
                    continue;
                }
                var arrow = line.IndexOf("-->", StringComparison.Ordinal);
                if (arrow < 0) continue;
                var startText = line.Substring(0, arrow).Trim();
                var endText = line.Substring(arrow + 3).Trim().Split(' ')[0];
                double from, to;
                if (!TryTime(startText, out from) || !TryTime(endText, out to) || to < from) throw new FormatException("Invalid subtitle cue timestamp: " + line);
                var content = new List<string>();
                while (++i < lines.Length && !string.IsNullOrWhiteSpace(lines[i])) content.Add(lines[i].TrimEnd());
                var value = string.Join("\n", content).Trim();
                if (value.Length > 0) model.body.Add(new SubtitleItemModel { from = from, to = to, content = value, location = 2 });
            }
            if (model.body.Count == 0) throw new FormatException("Subtitle file contains no valid cues.");
            return Normalize(model, stripMarkup: true);
        }

        private static bool TryTime(string value, out double seconds)
        {
            seconds = 0;
            var match = Timestamp.Match(value);
            if (!match.Success) return false;
            var hours = match.Groups[1].Success ? double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
            var minutes = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            var wholeSeconds = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
            if (minutes >= 60 || wholeSeconds >= 60) return false;
            var fraction = match.Groups[4].Value.PadRight(3, '0');
            seconds = hours * 3600 + minutes * 60 + wholeSeconds + double.Parse(fraction, CultureInfo.InvariantCulture) / 1000;
            return !double.IsNaN(seconds) && !double.IsInfinity(seconds);
        }

        private static SubtitleModel Normalize(SubtitleModel model, bool stripMarkup)
        {
            if (model.body == null || model.body.Any(cue => cue == null || cue.from < 0 || cue.to < cue.from ||
                double.IsNaN(cue.from) || double.IsInfinity(cue.from) || double.IsNaN(cue.to) || double.IsInfinity(cue.to) || string.IsNullOrWhiteSpace(cue.content)))
                throw new FormatException("Subtitle cue list contains invalid entries.");
            if (stripMarkup)
                foreach (var cue in model.body)
                    cue.content = WebUtility.HtmlDecode(VttMarkup.Replace(cue.content, ""));
            model.body = model.body.OrderBy(cue => cue.from).ToList();
            return model;
        }

        public static SubtitleItemModel FindCue(SubtitleModel model, double playbackSeconds)
        {
            return model?.body?.FirstOrDefault(cue => cue.from <= playbackSeconds && cue.to >= playbackSeconds);
        }
    }
}
