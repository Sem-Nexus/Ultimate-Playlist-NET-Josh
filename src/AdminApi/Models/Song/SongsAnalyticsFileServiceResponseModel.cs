#region Usings

using UltimatePlaylist.Common.Mvc.Attributes;

#endregion

namespace UltimatePlaylist.AdminApi.Models.Song
{
    public class SongsAnalyticsFileServiceResponseModel
    {
        public Guid ExternalId { get; set; }

        [UltimateColumn(nameof(Artist))]
        public string Artist { get; set; }

        [UltimateColumn(nameof(Title))]
        public string Title { get; set; }

        [UltimateColumn(nameof(Licensor))]
        public string Licensor { get; set; }

        [UltimateColumn(nameof(Album))]
        public string Album { get; set; }

        [UltimateColumn(nameof(Genre))]
        public string Genre { get; set; }

        [UltimateColumn("Most Times Added")]
        public int NumberOfTimesAddedToDSP { get; set; }

        [UltimateColumn("Ranked")]
        public int NumbersOfRate { get; set; }

        [UltimateColumn("Avarage Rank")]
        public decimal? AverageRating { get; set; }

        [UltimateColumn("Plays")]
        public int UniquePlays { get; set; }

        [UltimateColumn("Minutes listened")]
        public long? TotalTimeListened { get; set; }

        [UltimateColumn(nameof(OwnerLabel))]
        public string OwnerLabel { get; set; }

        [UltimateColumn(nameof(Songwriter))]
        public string Songwriter { get; set; }

        [UltimateColumn(nameof(Producer))]
        public string Producer { get; set; }

        [UltimateColumn(nameof(FirstReleaseDate))]
        public DateTime FirstReleaseDate { get; set; }

        [UltimateColumn(nameof(IsNewRelease))]
        public bool IsNewRelease { get; set; }

        [UltimateColumn(nameof(IsAudioOriginal))]
        public bool IsAudioOriginal { get; set; }

        [UltimateColumn(nameof(IsArtWorkOriginal))]
        public bool IsArtWorkOriginal { get; set; }

        [UltimateColumn(nameof(HasExplicitContent))]
        public bool HasExplicitContent { get; set; }

        [UltimateColumn(nameof(HasSample))]
        public bool HasSample { get; set; }

        [UltimateColumn(nameof(HasLegalClearance))]
        public bool? HasLegalClearance { get; set; }

        [UltimateColumn(nameof(IsConfirmed))]
        public bool IsConfirmed { get; set; }

        [UltimateColumn(nameof(Duration))]
        public TimeSpan Duration { get; set; }

        [UltimateColumn(nameof(LinkToAppleMusic))]
        public string LinkToAppleMusic { get; set; }

        [UltimateColumn(nameof(LinkToSpotify))]
        public string LinkToSpotify { get; set; }
    }
}
