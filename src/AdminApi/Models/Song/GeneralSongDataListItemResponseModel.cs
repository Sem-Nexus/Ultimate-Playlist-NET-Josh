namespace UltimatePlaylist.AdminApi.Models.Song
{
    public class GeneralSongDataListItemResponseModel
    {
        public Guid ExternalId { get; set; }

        public string Artist { get; set; }

        public string Title { get; set; }

        public string Licensor { get; set; }

        public string Album { get; set; }

        public string Genre { get; set; }

        public string GenreSecondary { get; set; }
        
        public int NumberOfTimesAddedToDSP { get; set; }

        public int NumbersOfRate { get; set; }

        public decimal? AverageRating { get; set; }

        public int UniquePlays { get; set; }

        public int? TotalTimeListened { get; set; }

        public string CoverUrl { get; set; }
    }
}
