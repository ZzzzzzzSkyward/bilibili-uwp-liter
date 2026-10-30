using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BiliLite.Models.LocalMedia;
using BiliLite.Services.LocalMedia;

const string root = "/collection";
var existing = new HashSet<string>(StringComparer.Ordinal)
{
    "/collection/Episodes/01/video.mkv",
    "/collection/Episodes/01/subtitles/en.srt",
    "/collection/Episodes/01/danmaku/comments.xml",
    "/collection/poster.jpg"
};
const string validJson = @"{
  'schemaVersion': 1, 'id': 'series', 'title': 'Sample', 'poster': 'poster.jpg',
  'items': [{ 'id': 'ep1', 'title': 'Episode 1',
    'videoTracks': [{ 'id': '1080p', 'path': 'Episodes/01/video.mkv', 'label': '1080p' }],
    'audioTracks': [],
    'subtitleTracks': [{ 'id': 'en', 'path': 'Episodes/01/subtitles/en.srt', 'label': 'English', 'format': 'srt' }],
    'danmakuTracks': [{ 'id': 'bili', 'path': 'Episodes/01/danmaku/comments.xml', 'label': 'Bilibili', 'format': 'bilibili-xml', 'enabledByDefault': true }]
  }]
}";
Func<string, bool> exists = path => existing.Contains(path);
var valid = LocalMediaManifestLoader.Load(validJson, root, exists);
Assert(valid.Items.Count == 1, "valid manifest deserializes its item");
Assert(!valid.Items[0].VideoTracks[0].IsDefault, "an absent video default remains unset");
Assert(!valid.Items[0].SubtitleTracks[0].IsDefault, "an absent subtitle default remains unset");
Assert(!valid.Items[0].AudioTracks.Any(), "empty optional audio list remains valid");
Assert(valid.Items[0].DanmakuTracks[0].EnabledByDefault, "danmaku enabledByDefault is deserialized");
var unlabeled = LocalMediaManifestLoader.Load(validJson.Replace("'label': '1080p'", "'label': null"), root, exists);
Assert(unlabeled.Items[0].VideoTracks[0].Label == null, "optional track labels are accepted");

ExpectError("parent traversal", "stay beneath", validJson.Replace("Episodes/01/video.mkv", "../outside.mkv"));
ExpectError("missing asset", "not found", validJson.Replace("Episodes/01/video.mkv", "Episodes/01/missing.mkv"));
ExpectError("unsupported schema version", "schemaVersion", validJson.Replace("'schemaVersion': 1", "'schemaVersion': 2"));
ExpectError("duplicate track IDs", "duplicate video track id", validJson.Replace("'videoTracks': [{ 'id': '1080p', 'path': 'Episodes/01/video.mkv', 'label': '1080p' }]", "'videoTracks': [{ 'id': '1080p', 'path': 'Episodes/01/video.mkv', 'label': '1080p' }, { 'id': '1080p', 'path': 'Episodes/01/video.mkv', 'label': 'copy' }]"));
ExpectError("multiple defaults", "at most one default video", validJson.Replace("'label': '1080p' }", "'label': '1080p', 'default': true }, { 'id': 'other', 'path': 'Episodes/01/video.mkv', 'label': 'Other', 'default': true }"));
ExpectError("missing required title", "title is required", validJson.Replace("'title': 'Sample',", ""));
ExpectError("unsupported subtitle format", "unsupported subtitle format", validJson.Replace("'format': 'srt'", "'format': 'ass'"));
ExpectError("format and extension mismatch", "does not match", validJson.Replace("en.srt", "en.vtt"));
ExpectError("absolute path", "relative path", validJson.Replace("Episodes/01/video.mkv", "/outside/video.mkv"));
ExpectError("malformed path", "path is invalid", validJson.Replace("Episodes/01/video.mkv", "bad\u0000name.mkv"));
ExpectError("malformed JSON", "JSON could not be read", "not json");

var tempRoot = Path.Combine(Path.GetTempPath(), "lm01-loader-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(Path.Combine(tempRoot, "Episodes", "01", "subtitles"));
File.WriteAllText(Path.Combine(tempRoot, "Episodes", "01", "video.mkv"), "fixture");
File.WriteAllText(Path.Combine(tempRoot, "Episodes", "01", "subtitles", "en.srt"), "fixture");
    Directory.CreateDirectory(Path.Combine(tempRoot, "Episodes", "01", "danmaku"));
    File.WriteAllText(Path.Combine(tempRoot, "Episodes", "01", "danmaku", "comments.xml"), "fixture");
    File.WriteAllText(Path.Combine(tempRoot, "poster.jpg"), "fixture");
File.WriteAllText(Path.Combine(tempRoot, "bili-media.json"), validJson.Replace("'", "\""));
try
{
    var fromFile = LocalMediaManifestLoader.Load(Path.Combine(tempRoot, "bili-media.json"));
    Assert(fromFile.Id == "series", "filesystem loader reads manifest and checks referenced assets");
}
finally { Directory.Delete(tempRoot, true); }

Console.WriteLine("LM-01 actual-source checks passed (12 validation cases plus filesystem load).");

void ExpectError(string name, string messagePart, string json)
{
    try
    {
        LocalMediaManifestLoader.Load(json, root, exists);
        throw new Exception("Expected validation error: " + name);
    }
    catch (LocalMediaManifestException ex)
    {
        Assert(ex.Message.IndexOf(messagePart, StringComparison.OrdinalIgnoreCase) >= 0,
            name + " reports useful context; actual message: " + ex.Message);
    }
}

void Assert(bool condition, string message)
{
    if (!condition) throw new Exception("Assertion failed: " + message);
}
