#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UltimatePlaylist.Common.Enums;
using UltimatePlaylist.Common.Mvc.Models;
using UltimatePlaylist.MobileApi.Models.Song;

#endregion

namespace UltimatePlaylist.MobileApi.Models.Playlist
{
    public class PlaylistResponseModel : BaseResponseModel
    {
        public Guid? CurrentSongExternalId { get; set; }

        public DateTime StartDate { get; set; }

        public PlaylistState State { get; set; }

        public DateTime PlaylistExpirationTimeStamp { get; set; }

        public int PlaylistExpirationCountDown { get; set; }

        public int? ActualListeningSecond { get; set; }

        public List<UserSongResponseModel> Songs { get; set; }
    }
}
