#region Usings

#endregion

namespace UltimatePlaylist.Services.Common.Models.AppleMusic.Responses
{

    public class AppleSearchResponse
    {
        public Results results { get; set; }

    }

    public class Results
    {
        public Songs songs { get; set; }
    }

    public class Songs
    {
        public string href { get; set; }

        public string next { get; set; }

        public List<AppleSongResponse> data { get; set; }
    }
}
