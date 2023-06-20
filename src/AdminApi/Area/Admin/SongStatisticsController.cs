#region Usings

using AutoMapper;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;
using UltimatePlaylist.AdminApi.Models.Song;
using UltimatePlaylist.Common.Mvc.Attributes;
using UltimatePlaylist.Common.Mvc.Controllers;
using UltimatePlaylist.Common.Mvc.File;
using UltimatePlaylist.Common.Mvc.Paging;
using UltimatePlaylist.Database.Infrastructure.Views;
using UltimatePlaylist.Services.Common.Interfaces.Song;
using UltimatePlaylist.Services.Common.Models.Song;
using static UltimatePlaylist.Common.Mvc.Consts.Consts;

#endregion

namespace UltimatePlaylist.AdminApi.Area.Admin
{
    [Area("Song")]
    [Route("[controller]")]
    [ApiExplorerSettings(GroupName = AdminApiGroups.Administrator)]
    public class SongStatisticsController : BaseController
    {
        #region Private field(s)

        private readonly Lazy<IMapper> MapperProvider;
        private readonly Lazy<ISongStatisticsService> SongStatisticsServiceProvider;

        #endregion

        #region Constructor(s)

        public SongStatisticsController(Lazy<IMapper> mapperProvider, Lazy<ISongStatisticsService> songStatisticsServiceProvider)
        {
            MapperProvider = mapperProvider;
            SongStatisticsServiceProvider = songStatisticsServiceProvider;
        }

        #endregion

        #region Private propertie(s)

        private IMapper Mapper => MapperProvider.Value;

        private ISongStatisticsService SongStatisticsService => SongStatisticsServiceProvider.Value;

        #endregion

        #region GET

        [HttpGet("song-statics")]
        [ProducesEnvelope(typeof(SongStatics), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSongDPS()
        {
            return await SongStatisticsService.GetSongStatics()
               .Map(songs => Mapper.Map<SongStatics>(songs))
               .Finally(BuildEnvelopeResult);
        }

        #endregion

        #region PUT

        [HttpPut("filter")]
        [ProducesEnvelope(typeof(PaginatedResponse<GeneralSongDataListItemResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter(SongsAnalyticsFilterRequestModel filter)
        {
            var filterServiceModel = Mapper.Map<SongsAnalyticsFilterServiceModel>(filter);
            return await SongStatisticsService.SongsListAsync(XPagination, filterServiceModel)
                .Map(usersList => Mapper.Map<PaginatedResponse<GeneralSongDataListItemResponseModel>>(usersList))
                .Finally(BuildEnvelopeResult);
        }

        [HttpPut("get-File")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFile(SongsAnalyticsFilterRequestModel filter)
        {
            var filterServiceModel = Mapper.Map<SongsAnalyticsFilterServiceModel>(filter);
            var fileName = $"{filterServiceModel.Licensor ?? "file"}{(filterServiceModel.TimeRange ?? DateTime.UtcNow).ToString("yyyy-mm-dd")}.xlsx";
            return await SongStatisticsService.GetDataForFile(XPagination, filterServiceModel)
                .Map(songs => Mapper.Map<IReadOnlyCollection<SongsAnalyticsFileServiceResponseModel>>(songs))
                .Map(songs => DataExportUtil.GetExcelFile(songs))
                .Finally(result => BuildFileResult(result, fileName));
        }

        #endregion
    }
}
