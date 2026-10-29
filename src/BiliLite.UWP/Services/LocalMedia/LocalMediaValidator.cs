using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BiliLite.Models.LocalMedia;

namespace BiliLite.Services.LocalMedia
{
    public class LocalMediaManifestException : Exception
    {
        public LocalMediaManifestException(string message) : base(message) { }
        public LocalMediaManifestException(string message, Exception innerException) : base(message, innerException) { }
    }

    public static class LocalMediaValidator
    {
        private static readonly HashSet<string> VideoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".mp4", ".mkv", ".mov", ".webm", ".avi" };
        private static readonly HashSet<string> AudioExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".aac", ".m4a", ".mp3", ".flac", ".wav", ".ogg" };

        public static void Validate(LocalMediaManifest manifest, string rootDirectory, Func<string, bool> fileExists)
        {
            if (manifest == null) throw new LocalMediaManifestException("Manifest is empty or invalid JSON.");
            if (manifest.SchemaVersion != 1) throw new LocalMediaManifestException("schemaVersion must be 1.");
            Require(manifest.Id, "id");
            Require(manifest.Title, "title");
            if (manifest.Items == null || manifest.Items.Count == 0)
                throw new LocalMediaManifestException("items must contain at least one playable item.");
            if (string.IsNullOrWhiteSpace(rootDirectory))
                throw new LocalMediaManifestException("A collection root directory is required.");
            if (fileExists == null) throw new ArgumentNullException(nameof(fileExists));

            var root = Path.GetFullPath(rootDirectory);
            if (!string.IsNullOrEmpty(manifest.Poster)) ValidateAsset(manifest.Poster, "poster", root, fileExists, null);

            var itemIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in manifest.Items)
            {
                if (item == null) throw new LocalMediaManifestException("items cannot contain null entries.");
                Require(item.Id, "item id");
                Require(item.Title, "item title");
                if (!itemIds.Add(item.Id)) throw new LocalMediaManifestException("Duplicate item id '" + item.Id + "'.");

                item.VideoTracks = item.VideoTracks ?? new List<LocalMediaTrack>();
                item.AudioTracks = item.AudioTracks ?? new List<LocalMediaTrack>();
                item.SubtitleTracks = item.SubtitleTracks ?? new List<LocalMediaTrack>();
                item.DanmakuTracks = item.DanmakuTracks ?? new List<LocalMediaTrack>();
                if (item.VideoTracks.Count == 0)
                    throw new LocalMediaManifestException("Item '" + item.Id + "' must have at least one video track.");

                ValidateTrackGroup(item, item.VideoTracks, "video", VideoExtensions, null, true, root, fileExists);
                ValidateTrackGroup(item, item.AudioTracks, "audio", AudioExtensions, null, true, root, fileExists);
                ValidateTrackGroup(item, item.SubtitleTracks, "subtitle", null,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "srt", ".srt" }, { "vtt", ".vtt" }, { "bilibili-json", ".json" }
                    }, true, root, fileExists);
                ValidateTrackGroup(item, item.DanmakuTracks, "danmaku", null,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "bilibili-xml", ".xml" }
                    }, false, root, fileExists);
            }
        }

        private static void ValidateTrackGroup(LocalMediaItem item, IList<LocalMediaTrack> tracks, string category,
            HashSet<string> extensions, IDictionary<string, string> formats, bool hasDefault,
            string root, Func<string, bool> fileExists)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var defaults = 0;
            for (var i = 0; i < tracks.Count; i++)
            {
                var track = tracks[i];
                if (track == null) throw new LocalMediaManifestException("Item '" + item.Id + "' has a null " + category + " track.");
                Require(track.Id, category + " track id");
                if (!ids.Add(track.Id))
                    throw new LocalMediaManifestException("Item '" + item.Id + "' has duplicate " + category + " track id '" + track.Id + "'.");

                if (formats != null)
                {
                    string extension;
                    if (string.IsNullOrWhiteSpace(track.Format) || !formats.TryGetValue(track.Format, out extension))
                        throw new LocalMediaManifestException("Item '" + item.Id + "' has unsupported " + category + " format '" + (track.Format ?? "") + "'.");
                    ValidateAsset(track.Path, category + " track '" + track.Id + "'", root, fileExists, extension);
                }
                else
                {
                    ValidateAsset(track.Path, category + " track '" + track.Id + "'", root, fileExists, null);
                    if (!extensions.Contains(Path.GetExtension(track.Path)))
                        throw new LocalMediaManifestException("Item '" + item.Id + "' has unsupported " + category + " file extension for '" + track.Path + "'.");
                }

                if (hasDefault && track.IsDefault) defaults++;
            }
            if (defaults > 1)
                throw new LocalMediaManifestException("Item '" + item.Id + "' can have at most one default " + category + " track.");
        }

        private static void ValidateAsset(string relativePath, string description, string root, Func<string, bool> fileExists, string expectedExtension)
        {
            if (string.IsNullOrWhiteSpace(relativePath) || relativePath.IndexOf('\\') >= 0 || relativePath.StartsWith("/", StringComparison.Ordinal))
                throw new LocalMediaManifestException(description + " path must be a non-empty relative path using '/' separators.");

            var segments = relativePath.Split('/');
            if (segments.Any(segment => segment.Length == 0 || segment == "." || segment == ".." || segment.IndexOf(':') >= 0))
                throw new LocalMediaManifestException(description + " path must stay beneath the collection root: '" + relativePath + "'.");

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            }
            catch (Exception exception) when (exception is ArgumentException || exception is NotSupportedException || exception is PathTooLongException)
            {
                throw new LocalMediaManifestException(description + " path is invalid: '" + relativePath + "'.", exception);
            }
            var rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var comparison = Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (!fullPath.StartsWith(rootPrefix, comparison))
                throw new LocalMediaManifestException(description + " path must stay beneath the collection root: '" + relativePath + "'.");
            if (expectedExtension != null && !string.Equals(Path.GetExtension(fullPath), expectedExtension, StringComparison.OrdinalIgnoreCase))
                throw new LocalMediaManifestException(description + " format does not match file extension '" + relativePath + "'.");
            if (!fileExists(fullPath))
                throw new LocalMediaManifestException(description + " file was not found: '" + relativePath + "'.");
        }

        private static void Require(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new LocalMediaManifestException(field + " is required.");
        }
    }
}
