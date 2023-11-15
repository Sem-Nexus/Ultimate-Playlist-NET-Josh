#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UltimatePlaylist.Services.Common.Models.Dsp;

#endregion

namespace UltimatePlaylist.Services.Common.Models.Song
{
    public class UserSongReadServiceModel : BaseReadServiceModel
    {
        public string Artist { get; set; }

        public string Title { get; set; }

        public string OwnerLabel { get; set; }

        public string FeaturedArtist { get; set; }

        public string AudioFileStreamingUrl { get; set; }

        public string CoverFileUrl { get; set; }

        public bool IsAddedToSpotify { get; set; }

        public bool IsAddedToAppleMusic { get; set; }

        public bool IsSkipped { get; set; }

        public int? UserRating { get; set; }

        public TimeSpan Duration { get; set; }

        public bool IsCurrent { get; set; }

        public DateTime? AddedToUserPlaylistDate { get; set; }

        public int? SecondsListened { get; set; }
    }
}
