namespace UltimatePlaylist.Services.Common.Models.Song
{
    public class GeneralMusicDataListItemReadServiceModel
    {
        public long Id { get; set; }

        public Guid ExternalId { get; set; }

        public string Artist { get; set; }

        public string Title { get; set; }

        public string Album { get; set; }

        public string Genre { get; set; }

        public string GenreSecondary { get; set; }

        public int NumberOfTimesAddedToDSP { get; set; }

        public string CoverUrl { get; set; }
    }
}