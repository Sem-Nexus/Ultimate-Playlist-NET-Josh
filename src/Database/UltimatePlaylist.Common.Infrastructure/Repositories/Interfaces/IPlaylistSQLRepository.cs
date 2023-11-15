using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltimatePlaylist.Database.Infrastructure.Repositories.Interfaces
{
    public interface IPlaylistSQLRepository
    {
        Task UpdatePlaylistState(string playlistState, long playlistId);
        Task UpdatePlaylistStateAndCurrentSong(
               string playlistState,
               string userExternalId,
               string currentSongExternalId,
               string playlistExternalId,
               string actualListeningSecond
          );
    }
}
