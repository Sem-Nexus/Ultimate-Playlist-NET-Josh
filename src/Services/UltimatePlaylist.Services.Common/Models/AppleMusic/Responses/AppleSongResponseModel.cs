#region Usings

#endregion

namespace UltimatePlaylist.Services.Common.Models.AppleMusic.Responses
{

    public class AppleSongIdResponse{
        public List<AppleSongResponse>  data { get; set; }
    }

    public class AppleSongResponse {
        public string id { get; set; }

        public string type { get; set; }

        public string href { get; set; }

        public Attributes attributes { get; set; }

    }

    public class Attributes
    {
        public string albumName { get; set; }

        public List<string> genreNames { get; set; }

        public int trackNumber { get; set; }

        public DateTime releaseDate { get; set; }

        public long durationInMillis { get; set; }

        public ArtWork artWork { get; set; }

        public string composerName { get; set; }

        public string url { get; set; }

        public string name { get; set; }

        public string artistName { get; set; }

    }

    public class ArtWork
    {
        public string url { get; set; }
    }

}
