using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UltimatePlaylist.Database.Infrastructure.Repositories.Interfaces;

namespace UltimatePlaylist.Database.Infrastructure.Repositories
{
    public class PlaylistSQLRepository : IPlaylistSQLRepository
    {
        private readonly string _connectionString;

        public PlaylistSQLRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task UpdatePlaylistState(string playlistState, long playlistId)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sqlQuery = "UPDATE [dbo].[UserPlaylist] SET [State] = @state WHERE Id = @id";
            var sqlCommand = new SqlCommand(sqlQuery, connection);
            sqlCommand.Parameters.Add(new SqlParameter("@id", value: playlistId));
            sqlCommand.Parameters.Add(new SqlParameter("@state", value: playlistState));

            await sqlCommand.ExecuteNonQueryAsync();
        }

        public async Task UpdatePlaylistStateAndCurrentSong(
                string playlistState,
                string userExternalId,
                string playlistExternalId,
                string currentSongExternalId,
                string actualListeningSecond,
                string playlistEvent
            )
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sqlCommand = new SqlCommand("SetPlaylistAndSongState3", connection);
            sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;

            sqlCommand.Parameters.Add(new SqlParameter("@playlistState", value: playlistState));
            sqlCommand.Parameters.Add(new SqlParameter("@userExternalId", value: userExternalId));
            sqlCommand.Parameters.Add(new SqlParameter("@playlistExternalId", value: playlistExternalId));
            sqlCommand.Parameters.Add(new SqlParameter("@currentSongExternalId", value: currentSongExternalId));
            sqlCommand.Parameters.Add(new SqlParameter("@actualListeningSecond", value: actualListeningSecond));
            sqlCommand.Parameters.Add(new SqlParameter("@playlistEvent", value: playlistEvent));


            await sqlCommand.ExecuteNonQueryAsync();
        }
    }
}
