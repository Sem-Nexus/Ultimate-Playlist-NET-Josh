#region Usings

using System.ComponentModel;
using AutoMapper;
using Castle.Core.Internal;
using CSharpFunctionalExtensions;
using OfficeOpenXml;
using UltimatePlaylist.Common.Models;
using UltimatePlaylist.Common.Mvc.Attributes;
using UltimatePlaylist.Database.Infrastructure.Entities.Song;
using UltimatePlaylist.Database.Infrastructure.Views;
using UltimatePlaylist.Services.Common.Interfaces.Song;
using UltimatePlaylist.Services.Common.Models;
using UltimatePlaylist.Services.Common.Models.Song;

#endregion

namespace UltimatePlaylist.Services.Song
{
    public class SongStatisticsService : ISongStatisticsService
    {
        #region Private field(s)

        private readonly Lazy<IMapper> MapperProvider;
        private readonly Lazy<ISongStatisticsProcedureRepository> SongStatisticsProcedureRepositoryProvider;

        #endregion

        #region Constructor(s)

        public SongStatisticsService(
            Lazy<IMapper> mapperProvider,
            Lazy<ISongStatisticsProcedureRepository> songStatisticsProcedureRepositoryProvider)
        {
            MapperProvider = mapperProvider;
            SongStatisticsProcedureRepositoryProvider = songStatisticsProcedureRepositoryProvider;
        }

        #endregion

        #region Private properties

        private IMapper Mapper => MapperProvider.Value;

        private ISongStatisticsProcedureRepository SongStatisticsProcedureRepository => SongStatisticsProcedureRepositoryProvider.Value;

        #endregion

        #region Public method(s)

        public async Task<Result<PaginatedReadServiceModel<GeneralSongDataListItemReadServiceModel>>> SongsListAsync(Pagination pagination, SongsAnalyticsFilterServiceModel filterServiceModel)
        {
            Result<List<GeneralSongDataProcedureView>> songList = await SongStatisticsProcedureRepository.GetGeneralSongsData(pagination, filterServiceModel);
            int count = 0;

            if (pagination.SearchValue.IsNullOrEmpty())
            {
                count = await SongStatisticsProcedureRepository.GeneralSongsCount(filterServiceModel);
            }
            else
            {
                count = songList.Value.Count;
            }

            return songList
                   .Map(songs => Mapper.Map<IReadOnlyList<GeneralSongDataListItemReadServiceModel>>(songs))
                   .Map(songs => new PaginatedReadServiceModel<GeneralSongDataListItemReadServiceModel>(songs, pagination, count));
        }

        public async Task<Result<PaginatedReadServiceModel<GeneralMusicDataListItemReadServiceModel>>> MusicListAsync(Pagination pagination, SongsAnalyticsFilterServiceModel filterServiceModel)
        {
            Result<List<GeneralMusicDataProcedureView>> songList = await SongStatisticsProcedureRepository.GetGeneralMusicData(pagination, filterServiceModel);
            int count = 0;

            if (pagination.SearchValue.IsNullOrEmpty())
            {
                count = await SongStatisticsProcedureRepository.GeneralSongsCount(filterServiceModel);
            }
            else
            {
                count = songList.Value.Count;
            }

            return songList
                   .Map(songs => Mapper.Map<IReadOnlyList<GeneralMusicDataListItemReadServiceModel>>(songs))
                   .Map(songs => new PaginatedReadServiceModel<GeneralMusicDataListItemReadServiceModel>(songs, pagination, count));
        }

        public async Task<Result<IReadOnlyList<SongsAnalyticsFileServiceReadModel>>> GetDataForFile(Pagination pagination, SongsAnalyticsFilterServiceModel filterServiceModel)
        {
            return await SongStatisticsProcedureRepository.GetFileSongsData(pagination, filterServiceModel)
                   .Map(songs => Mapper.Map<IReadOnlyList<SongsAnalyticsFileServiceReadModel>>(songs));
        }

        public async Task<Result<SongEntity>> GetSongData(string externalID)
        {
            return await SongStatisticsProcedureRepository.GetSongData(externalID)
                   .Map(songs => Mapper.Map<SongEntity>(songs));
        }

        public async Task<Result<IReadOnlyList<SongSocialMediaEntity>>> GetSongSocialMedia(string songId)
        {
            return await SongStatisticsProcedureRepository.GetSongSocialMedia(songId)
                   .Map(songs => Mapper.Map<IReadOnlyList<SongSocialMediaEntity>> (songs));
        }

        public async Task<Result<IReadOnlyList<SongDSPEntity>>> GetSongDPS(string songId)
        {
            return await SongStatisticsProcedureRepository.GetSongDPS(songId)
                   .Map(songs => Mapper.Map<IReadOnlyList<SongDSPEntity>>(songs));
        }

        public async Task<Result<SongStatics>> GetSongStatics()
        {
            return await SongStatisticsProcedureRepository.GetSongStatics()
                   .Map(songs => Mapper.Map<SongStatics>(songs));
        }

        #endregion
    }
}
