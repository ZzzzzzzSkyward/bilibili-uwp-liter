using System;

namespace BiliLite.Services.LocalMedia
{
    public sealed class LocalMediaTrackLoadException : Exception
    {
        public LocalMediaTrackLoadException(string stage, string trackId, Exception inner)
            : base("A local media track could not be loaded; stage=" + stage + "; trackId=" + trackId + ".", inner)
        {
            Stage = stage;
            TrackId = trackId;
        }

        public string Stage { get; private set; }
        public string TrackId { get; private set; }
    }
}
