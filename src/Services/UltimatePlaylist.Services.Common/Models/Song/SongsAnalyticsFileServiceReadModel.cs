namespace UltimatePlaylist.Services.Common.Models.Song
{
    public class SongsAnalyticsFileServiceReadModel
    {
        public long Id { get; set; }

        public Guid ExternalId { get; set; }

        public string Artist { get; set; }

        public string Title { get; set; }

        public string Licensor { get; set; }

        public string Album { get; set; }

        public string Genre { get; set; }

        public int NumberOfTimesAddedToDSP { get; set; }

        public int NumbersOfRate { get; set; }

        public double? AverageRating { get; set; }

        public int UniquePlays { get; set; }

        public int? TotalTimeListened { get; set; }

        public string CoverUrl { get; set; }

        public string OwnerLabel { get; set; }

        public string Songwriter { get; set; }

        public string Producer { get; set; }

        public DateTime FirstReleaseDate { get; set; }

        public bool IsNewRelease { get; set; }

        public bool IsAudioOriginal { get; set; }

        public bool IsArtWorkOriginal { get; set; }

        public bool HasExplicitContent { get; set; }

        public bool HasSample { get; set; }

        public bool? HasLegalClearance { get; set; }

        public bool IsConfirmed { get; set; }

        public TimeSpan Duration { get; set; }

        public string LinkToAppleMusic { get; set; }

        public string LinkToSpotify { get; set; }
    }
}
