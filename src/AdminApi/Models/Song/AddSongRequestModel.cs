#region Usings

using System;
using System.Collections.Generic;

#endregion

namespace UltimatePlaylist.AdminApi.Models.Song
{
    public class AddSongRequestModel
    {
        public Guid? SongFileExternalId { get; set; }

        public Guid? SongCoverExternalId { get; set; }

        public string Artist { get; set; }

        public string Title { get; set; }

        public string Album { get; set; }

        public string FeaturedArtist { get; set; }

        public string Label { get; set; }

        public string Licensor { get; set; }

        public string LinkToSpotify { get; set; }

        public string? LinkToAppleMusic { get; set; }

        public bool? IsNewRelease { get; set; }

        public DateTime? FirstPublicReleaseDate { get; set; }

        public IList<Guid> PrimaryGenres { get; set; }

        public IList<Guid> SecondaryGenres { get; set; }

        public string SongWriter { get; set; }

        public string Producer { get; set; }

        public string InstagramUrl { get; set; }

        public string FacebookUrl { get; set; }

        public string YoutubeUrl { get; set; }

        public string SnapchatUrl { get; set; }

        public bool? IsAllAudioOriginal { get; set; }

        public bool? IsAllArtworkOriginal { get; set; }

        public bool? IsSongWithExplicitContent { get; set; }

        public bool? IsSongWithSample { get; set; }

        public bool? IsLeagalClearanceObtained { get; set; }

        public bool? IsAllConfirmed { get; set; }

        public TimeSpan Duration { get; set; }
    }
}
