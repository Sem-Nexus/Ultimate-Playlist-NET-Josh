#region Usings

using CSharpFunctionalExtensions;
using UltimatePlaylist.Common.Models;
using UltimatePlaylist.Database.Infrastructure.Entities.Song;
using UltimatePlaylist.Database.Infrastructure.Views;
using UltimatePlaylist.Services.Common.Models;
using UltimatePlaylist.Services.Common.Models.Song;

#endregion

namespace UltimatePlaylist.Services.Common.Interfaces.Song
{
    public interface ISongStatisticsService
    {
        Task<Result<PaginatedReadServiceModel<GeneralSongDataListItemReadServiceModel>>> SongsListAsync(Pagination pagination, SongsAnalyticsFilterServiceModel filterServiceModel);

        Task<Result<PaginatedReadServiceModel<GeneralMusicDataListItemReadServiceModel>>> MusicListAsync(Pagination pagination, SongsAnalyticsFilterServiceModel filterServiceModel);

        Task<Result<IReadOnlyList<SongsAnalyticsFileServiceReadModel>>> GetDataForFile(Pagination pagination, SongsAnalyticsFilterServiceModel filterServiceModel);

        Task<Result<SongEntity>> GetSongData(string externalID);

        Task<Result<IReadOnlyList<SongSocialMediaEntity>>> GetSongSocialMedia(string songId);

        Task<Result<IReadOnlyList<SongDSPEntity>>> GetSongDPS(string songId);

        Task<Result<SongStatics>> GetSongStatics();
    }
}
