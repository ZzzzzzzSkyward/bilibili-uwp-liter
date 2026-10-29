using System;
using System.IO;
using BiliLite.Models.LocalMedia;
using Newtonsoft.Json;

namespace BiliLite.Services.LocalMedia
{
    public static class LocalMediaManifestLoader
    {
        public const string ManifestFileName = "bili-media.json";

        public static LocalMediaManifest Load(string manifestPath)
        {
            if (string.IsNullOrWhiteSpace(manifestPath)) throw new ArgumentException("A manifest path is required.", nameof(manifestPath));
            var fullPath = Path.GetFullPath(manifestPath);
            if (!File.Exists(fullPath)) throw new LocalMediaManifestException("Manifest file was not found: '" + manifestPath + "'.");
            return Load(File.ReadAllText(fullPath), Path.GetDirectoryName(fullPath), File.Exists);
        }

        public static LocalMediaManifest Load(string json, string rootDirectory, Func<string, bool> fileExists)
        {
            LocalMediaManifest manifest;
            try
            {
                manifest = JsonConvert.DeserializeObject<LocalMediaManifest>(json);
            }
            catch (JsonException exception)
            {
                throw new LocalMediaManifestException("Manifest JSON could not be read: " + exception.Message, exception);
            }

            LocalMediaValidator.Validate(manifest, rootDirectory, fileExists);
            return manifest;
        }
    }
}
