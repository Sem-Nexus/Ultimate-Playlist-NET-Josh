#region Usings

using UltimatePlaylist.Common.Enums;
using UltimatePlaylist.Services.Common.Models.Song;

#endregion

namespace UltimatePlaylist.Services.Common.Models.Playlist
{
    public class PlaylistReadServiceModel : BaseReadServiceModel
    {
        public Guid? CurrentSongExternalId { get; set; }

        public Guid? LastPlaylistTicketSongExternalId { get; set; }

        public DateTime StartDate { get; set; }

        public PlaylistState State { get; set; }

        public DateTime PlaylistExpirationTimeStamp { get; set; }

        public int PlaylistExpirationCountDown { get; set; } = 0;

        public int? ActualListeningSecond { get; set; }

        public List<UserSongReadServiceModel> Songs { get; set; }
    }
}
