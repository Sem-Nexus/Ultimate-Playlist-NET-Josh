#region Usings

using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using UltimatePlaylist.Common.Models;
using UltimatePlaylist.Database.Infrastructure.Entities.Song;
using UltimatePlaylist.Services.Common.Models;
using UltimatePlaylist.Services.Common.Models.Song;

#endregion

namespace UltimatePlaylist.Services.Common.Interfaces.Song
{
    public interface ISongService
    {
        Task<Result> AddSongAsync(AddSongWriteServiceModel addSongWriteServiceModel);

        Task<Result> RemoveSongAsync(RemoveSongWriteServiceModel removeSongWriteServiceModel);

        Task<Result<PaginatedReadServiceModel<SongReadServiceModel>>> SongsListAsync(
            Pagination pagination);

       Task<Result<SongEntity>> EditSongAsync(AddSongWriteServiceModel addSongWriteServiceModel);

    }
}
