#region Usings

using UltimatePlaylist.Common.Mvc.Models;

#endregion

namespace UltimatePlaylist.AdminApi.Models.Song
{
    public class SongResponseModel : BaseResponseModel
    {
        public string Artist { get; set; }

        public string Title { get; set; }

        public string Album { get; set; }

        public string PrimaryGenres { get; set; }

        public string SecondaryGenres { get; set; }

        public int TotalSongPlays { get; set; }

        public int TotalAddedToDSP { get; set; }

        public string CoverUrl { get; set; }

        public int TotalTimeListened { get; set; }
    }
}
