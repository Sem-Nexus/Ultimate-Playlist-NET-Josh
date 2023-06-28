#region Usings

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UltimatePlaylist.AdminApi.Models.Song;
using UltimatePlaylist.Common.Enums;
using UltimatePlaylist.Common.Mvc.Attributes;
using UltimatePlaylist.Common.Mvc.Controllers;
using UltimatePlaylist.Common.Mvc.Paging;
using UltimatePlaylist.Services.Common.Interfaces.Song;
using UltimatePlaylist.Services.Common.Models.Song;
using static UltimatePlaylist.Common.Mvc.Consts.Consts;
using UltimatePlaylist.Database.Infrastructure.Entities.Song;
using UltimatePlaylist.Services.Common.Interfaces.AppleMusic.Client;
using UltimatePlaylist.Services.Common.Models.AppleMusic.Responses;

#endregion

namespace UltimatePlaylist.AdminApi.Area.Admin
{
    [Area("Song")]
    [Route("[controller]")]
    [AuthorizeRole(UserRole.Administrator)]
    [ApiExplorerSettings(GroupName = AdminApiGroups.Administrator)]
    public class SongController : BaseControllerWithAuthentication
    {
        #region Private Members

        private readonly Lazy<IMapper> MapperProvider;

        private readonly Lazy<ISongService> SongServiceProvider;

        private readonly Lazy<ISongStatisticsService> SongStatisticsServiceProvider;

        private readonly Lazy<IAppleMusicPlaylistClientService> AppleMusicPlaylistServiceProvider;

        #endregion

        #region Constructor(s)

        public SongController(
            Lazy<IMapper> mapperProvider,
            Lazy<ISongService> songServiceProvider,
            Lazy<ISongStatisticsService> songStatisticsServiceProvider,
            Lazy<IAppleMusicPlaylistClientService> appleMusicPlaylistServiceProvider)
        {
            MapperProvider = mapperProvider;
            SongServiceProvider = songServiceProvider;
            SongStatisticsServiceProvider = songStatisticsServiceProvider;
            AppleMusicPlaylistServiceProvider = appleMusicPlaylistServiceProvider;
        }

        #endregion

        #region Private Properites

        private IMapper Mapper => MapperProvider.Value;

        private ISongService SongService => SongServiceProvider.Value;

        private ISongStatisticsService SongStatisticsService => SongStatisticsServiceProvider.Value;

        private IAppleMusicPlaylistClientService AppleMusicPlaylistService => AppleMusicPlaylistServiceProvider.Value;

        #endregion

        #region GET

        [HttpGet("songs-list")]
        [ProducesEnvelope(typeof(PaginatedResponse<SongResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SongsListAsync()
        {
            return await SongStatisticsService.SongsListAsync(XPagination, new SongsAnalyticsFilterServiceModel())
               .Map(songs => Mapper.Map<PaginatedResponse<SongResponseModel>>(songs))
               .Finally(BuildEnvelopeResult);
        }

        [HttpGet("music-list")]
        [ProducesEnvelope(typeof(PaginatedResponse<MusicResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> MusicListAsync()
        {
            return await SongStatisticsService.MusicListAsync(XPagination, new SongsAnalyticsFilterServiceModel())
               .Map(songs => Mapper.Map<PaginatedResponse<MusicResponseModel>>(songs))
               .Finally(BuildEnvelopeResult);
        }

        [HttpGet("")]
        [ProducesEnvelope(typeof(SongEntity), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSong([FromQuery] string externalId)
        {
            return await SongStatisticsService.GetSongData(externalId)
               .Map(songs => Mapper.Map<SongEntity>(songs))
               .Finally(BuildEnvelopeResult);
        }

        [HttpGet("social-media")]
        [ProducesEnvelope(typeof(SongSocialMediaEntity), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSongSocialMedia([FromQuery] string songId)
        {
            return await SongStatisticsService.GetSongSocialMedia(songId)
               .Map(songs => Mapper.Map<List<SongSocialMediaEntity>>(songs))
               .Finally(BuildEnvelopeResult);
        }

        [HttpGet("social-dps")]
        [ProducesEnvelope(typeof(SongDSPEntity), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSongDPS([FromQuery] string songId)
        {
            return await SongStatisticsService.GetSongDPS(songId)
               .Map(songs => Mapper.Map<List<SongDSPEntity>>(songs))
               .Finally(BuildEnvelopeResult);
        }

        #endregion

        #region POST

        [HttpPost("add-song")]
        [ProducesEmptyEnvelope(StatusCodes.Status200OK)]
        public async Task<IActionResult> AddSongAsync([FromBody] AddSongRequestModel addSongRequestModel)
        {
            var mapped = Mapper.Map<AddSongWriteServiceModel>(addSongRequestModel);

            return await SongService.AddSongAsync(mapped)
               .Finally(BuildEnvelopeResult);
        }

        #endregion

        #region DELETE

        [HttpDelete("remove-song")]
        [ProducesEmptyEnvelope(StatusCodes.Status200OK)]
        public async Task<IActionResult> RemoveSongAsync([FromBody] RemoveSongRequestModel removeSongRequestModel)
        {
            var mapped = Mapper.Map<RemoveSongWriteServiceModel>(removeSongRequestModel);

            return await SongService.RemoveSongAsync(mapped)
               .Finally(BuildEnvelopeResult);
        }

        #endregion

        #region PUT

        [HttpPut("edit-song")]
        [ProducesEmptyEnvelope(StatusCodes.Status200OK)]
        public async Task<IActionResult> EditSongAsync([FromBody] AddSongRequestModel editSongRequestModel)
        {
            var mapped = Mapper.Map<AddSongWriteServiceModel>(editSongRequestModel);

            return await SongService.EditSongAsync(mapped)
               .Finally(BuildEnvelopeResult);
        }

        [HttpGet("search-song-apple-music")]
        [ProducesEmptyEnvelope(StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchSongAppleMusicAsync([FromQuery] string searchParam)
        {
            return await AppleMusicPlaylistService.GetAllSongs(XUserExternalId, searchParam)
                .Map(usersList => Mapper.Map<List<AppleSongResponse>>(usersList.results.songs.data))
                .Finally(BuildEnvelopeResult);
        }

        [HttpGet("search-byID-apple-music")]
        [ProducesEmptyEnvelope(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSongByID([FromQuery] string songID)
        {
            return await AppleMusicPlaylistService.GetSongByID(XUserExternalId, songID)
                .Map(usersList => Mapper.Map<List<AppleSongResponse>>(usersList.data))
                .Finally(BuildEnvelopeResult);
        }

        #endregion
    }
}