#region Usings

using AutoMapper;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Options;
using UltimatePlaylist.Common.Config;
using UltimatePlaylist.Common.Const;
using UltimatePlaylist.Common.Enums;
using UltimatePlaylist.Common.Mvc.Helpers;
using UltimatePlaylist.Database.Infrastructure.Entities.Identity;
using UltimatePlaylist.Database.Infrastructure.Entities.Identity.Specifications;
using UltimatePlaylist.Database.Infrastructure.Entities.Playlist;
using UltimatePlaylist.Database.Infrastructure.Entities.Playlist.Specifications;
using UltimatePlaylist.Database.Infrastructure.Entities.Song;
using UltimatePlaylist.Database.Infrastructure.Entities.Song.Specifications;
using UltimatePlaylist.Database.Infrastructure.Repositories.Interfaces;
using UltimatePlaylist.Services.Common.Interfaces.Playlist;
using UltimatePlaylist.Services.Common.Interfaces.UserSong;
using UltimatePlaylist.Services.Common.Models.Playlist;
using UltimatePlaylist.Services.Common.Models.Schedule;

#endregion

namespace UltimatePlaylist.Service.Playlist
{
    public class PlaylistService : IPlaylistService
    {
        #region Private members

        private readonly Lazy<IRepository<PlaylistEntity>> PlaylistRepositoryProvider;

        private readonly Lazy<IRepository<PlaylistSongEntity>> PlaylistSongRepositoryProvider;

        private readonly Lazy<IRepository<UserPlaylistEntity>> UserPlaylistRepositoryProvider;

        private readonly Lazy<IRepository<UserPlaylistSongEntity>> UserPlaylistSongRepositoryProvider;

        private readonly Lazy<IReadOnlyRepository<SongEntity>> SongRepositoryProvider;

        private readonly Lazy<IUserSongService> UserSongServiceProvider;

        private readonly Lazy<IRepository<User>> UserRepositoryProvider;

        private readonly Lazy<IUserPlaylistService> UserPlaylistStoreProvider;

        private readonly Lazy<IMapper> MapperProvider;

        private readonly PlaylistConfig PlaylistConfig;

        #endregion

        #region Constructor(s)

        public PlaylistService(
            Lazy<IRepository<PlaylistEntity>> playlistRepositoryProvider,
            Lazy<IRepository<PlaylistSongEntity>> playlistSongRepositoryProvider,
            Lazy<IRepository<UserPlaylistEntity>> userPlaylistRepositoryProvider,
            Lazy<IRepository<UserPlaylistSongEntity>> userPlaylistSongRepositoryProvider,
            Lazy<IReadOnlyRepository<SongEntity>> songRepositoryProvider,
            Lazy<IUserSongService> userSongRepositoryProvider,
            Lazy<IRepository<User>> userRepositoryProvider,
            Lazy<IUserPlaylistService> userPlaylistStoreProvider,
            Lazy<IMapper> mapperProvider,
            IOptions<PlaylistConfig> playlistConfig)
        {
            PlaylistRepositoryProvider = playlistRepositoryProvider;
            PlaylistSongRepositoryProvider = playlistSongRepositoryProvider;
            UserPlaylistRepositoryProvider = userPlaylistRepositoryProvider;
            UserPlaylistSongRepositoryProvider = userPlaylistSongRepositoryProvider;
            SongRepositoryProvider = songRepositoryProvider;
            UserSongServiceProvider = userSongRepositoryProvider;
            UserRepositoryProvider = userRepositoryProvider;
            UserPlaylistStoreProvider = userPlaylistStoreProvider;
            MapperProvider = mapperProvider;
            PlaylistConfig = playlistConfig.Value;
        }

        #endregion

        #region Properties

        private IRepository<PlaylistEntity> PlaylistRepository => PlaylistRepositoryProvider.Value;

        private IRepository<PlaylistSongEntity> PlaylistSongRepository => PlaylistSongRepositoryProvider.Value;

        private IRepository<UserPlaylistEntity> UserPlaylistRepository => UserPlaylistRepositoryProvider.Value;

        private IRepository<UserPlaylistSongEntity> UserPlaylistSongRepository => UserPlaylistSongRepositoryProvider.Value;

        private IReadOnlyRepository<SongEntity> SongRepository => SongRepositoryProvider.Value;

        private IUserSongService UserSongService => UserSongServiceProvider.Value;

        private IRepository<User> UserRepository => UserRepositoryProvider.Value;

        private IUserPlaylistService UserPlaylistStore => UserPlaylistStoreProvider.Value;

        private IMapper Mapper => MapperProvider.Value;

        #endregion

        #region Public Method(s)

        public async Task<Result<PlaylistReadServiceModel>> GetTodaysPlaylist(Guid userExternalId)
        {
            var playlistServiceModel = await UserPlaylistStore.Get(userExternalId);

            var todayDate = DateTimeHelper.ToTodayUTCTimeForTimeZoneRelativeTime(PlaylistConfig.TimeZone);
            var currentDate = todayDate.Add(PlaylistConfig.StartDateOffSet);
            var playlistDate = currentDate.Add(PlaylistConfig.StartDateOffSet);

            if (playlistServiceModel == null || playlistServiceModel.StartDate != playlistDate)
            {
                return await GetOrCreateTodaysPlaylistForUserAsync(userExternalId, playlistDate, playlistServiceModel);
            }
            else
            {
                var now = DateTimeHelper.ToUTCTimeForTimeZoneRelativeTime(DateTime.UtcNow, PlaylistConfig.TimeZone);
                var timeDiff = Convert.ToInt32(Math.Floor((playlistDate.AddDays(1) - now).TotalSeconds));
                playlistServiceModel.PlaylistExpirationCountDown = timeDiff;
                return playlistServiceModel;
            }
        }

        public async Task<Result<AdminPlaylistReadServiceModel>> GetPlaylist(Guid playlistExternalId)
        {
            return await GetPlaylistAsync(playlistExternalId)
                .Map(playlist => Mapper.Map<AdminPlaylistReadServiceModel>(playlist));
        }

        /// <summary>
        /// usage : get maximun id.
        /// 2022-10-05
        /// </summary>
        /// <returns></returns>
        public long GetMaxPlaylistIndex()
        {
            return GetMaxIdPlaylistAsync();
        }

        public async Task<Result> AddSongToPlaylistAsync(AddSongToPlaylistWriteServiceModel addSongToPlaylistWriteServiceModel)
        {
            return await GetPlaylistAsync(addSongToPlaylistWriteServiceModel.PlaylistExternalId)
                .CheckIf(playlist => playlist.PlaylistSongs.Any(s => s.ExternalId == addSongToPlaylistWriteServiceModel.SongExternalId), _ => Result.Failure(ErrorMessages.SongExistInPlaylist))
                .Bind(async playlist => await GetSongsAsync(addSongToPlaylistWriteServiceModel.SongExternalId)
                    .Check(async song => await AddSongToPlaylistRelationAsync(playlist, song)));
        }

        public async Task<Result> RemoveSongFromPlaylistAsync(RemoveSongFromPlaylistWriteServiceModel removeSongFromPlaylistWriteServiceModel)
        {
            var playlistSong = await PlaylistSongRepository.FirstOrDefaultAsync(new PlaylistSongSpecification()
                .ByPlaylistExternalId(removeSongFromPlaylistWriteServiceModel.ExternalId)
                .BySongExternalId(removeSongFromPlaylistWriteServiceModel.SongExternalId)
                .WithPlaylist());

            await PlaylistSongRepository.DeleteAsync(new PlaylistSongSpecification()
                .ByPlaylistExternalId(removeSongFromPlaylistWriteServiceModel.ExternalId)
                .BySongExternalId(removeSongFromPlaylistWriteServiceModel.SongExternalId));

            if (playlistSong != null)
            {
                 await UserPlaylistSongRepository.DeleteAsync(new UserPlaylistSongSpecification()
                    .BySongExternalId(removeSongFromPlaylistWriteServiceModel.SongExternalId)
                    .ByStartDate(playlistSong.Playlist.StartDate));
            }

            return Result.Success();
        }

        public async Task<Result> RemoveAllSongsFromPlaylistAsync(Guid playlistExternalId)
        {
            var playlistSong = await PlaylistSongRepository.ListAsync(new PlaylistSongSpecification()
                .ByPlaylistExternalId(playlistExternalId));

            await PlaylistSongRepository.DeleteAsync(new PlaylistSongSpecification()
                .ByPlaylistExternalId(playlistExternalId));

            if (playlistSong.Count > 0)
            {
                await UserPlaylistSongRepository.DeleteAsync(new UserPlaylistSongSpecification()
                   .ByStartDate(playlistSong.First().Playlist.StartDate));
            }

            return Result.Success();
        }

        #endregion

        #region Private Method(s)

        private async Task<Result<PlaylistReadServiceModel>> GetOrCreateTodaysPlaylistForUserAsync(
            Guid userExternalId,
            DateTime today,
            PlaylistReadServiceModel playlistReadServiceModel)
        {
            UserPlaylistEntity userPlaylist = await UserPlaylistRepository.FirstOrDefaultAsync(new UserPlaylistSpecification()
               .BySpecificDate(today)
               .ByNotEmptyPlaylist()
               .ByUserExternalId(userExternalId)
               .WithSongs());

            if (userPlaylist != null)
            {
                userPlaylist.UserPlaylistSongs = userPlaylist.UserPlaylistSongs.Where(s => !s.IsDeleted).ToList();
                var now = DateTimeHelper.ToUTCTimeForTimeZoneRelativeTime(DateTime.UtcNow, PlaylistConfig.TimeZone);
                var timeDiff = Convert.ToInt32(Math.Floor((today.AddDays(1) - now).TotalSeconds));
                
                return userPlaylist.UserPlaylistSongs.Any()
                    ? await Result.Success(userPlaylist)
                    .Tap(_ => playlistReadServiceModel = Mapper.Map<PlaylistReadServiceModel>(userPlaylist))
                    .Tap(_ => playlistReadServiceModel.PlaylistExpirationTimeStamp = today.AddDays(1))
                    .Tap(_ => playlistReadServiceModel.PlaylistExpirationCountDown = timeDiff)
                    .Bind(async _ => await UserSongService.GetUserSongsForPlaylistAsync(userExternalId, userPlaylist.ExternalId))
                    .Tap(userSongs => playlistReadServiceModel.Songs = userSongs.ToList())
                    .Tap(userSongs => playlistReadServiceModel.CurrentSongExternalId = userSongs.First(s => s.IsCurrent).ExternalId)
                    .Tap(_ => playlistReadServiceModel.ActualListeningSecond = playlistReadServiceModel.Songs.FirstOrDefault(s => s.IsCurrent)?.SecondsListened)
                    .Tap(_ => UserPlaylistStore.Set(userExternalId, playlistReadServiceModel))
                    .Map(userSongs => playlistReadServiceModel)
                    : await CreateTodaysPlaylistForUserAsync(userExternalId, today, playlistReadServiceModel);
            }

            return await CreateTodaysPlaylistForUserAsync(userExternalId, today, playlistReadServiceModel);
        }

        private async Task<Result<PlaylistReadServiceModel>> CreateTodaysPlaylistForUserAsync(
            Guid userExternalId,
            DateTime today,
            PlaylistReadServiceModel playlistReadServiceModel)
        {
            PlaylistEntity playlist = default;

            User user = default;
            var now = DateTimeHelper.ToUTCTimeForTimeZoneRelativeTime(DateTime.UtcNow, PlaylistConfig.TimeZone);
            var timeDiff = Convert.ToInt32(Math.Floor((today.AddDays(1) - now).TotalSeconds));

            return await Result.Success(playlistReadServiceModel)
                .Check(async _ => await GetUserAsync(userExternalId)
                    .Tap(userEntity => user = userEntity))
                .Check(async _ => await GetTodayTemplatePlaylist(today)
                    .Tap(playlistEntity => playlist = playlistEntity))
                .Bind(async _ => await CreateUserPlaylistAsync(user, playlist, today))
                .Tap(userPlaylist => playlistReadServiceModel = Mapper.Map<PlaylistReadServiceModel>(userPlaylist))
                .Tap(userPlaylist => playlistReadServiceModel.PlaylistExpirationTimeStamp = today.AddDays(1))
                .Tap(userPlaylist => playlistReadServiceModel.PlaylistExpirationCountDown = timeDiff)
                .Bind(async userPlaylist => await UserSongService.GetUserSongsForPlaylistAsync(userExternalId, userPlaylist.ExternalId))
                .Tap(userSongs => playlistReadServiceModel.Songs = userSongs.Where(i => i.AudioFileStreamingUrl != null && i.AudioFileStreamingUrl.Length > 0).ToList())
                .Tap(_ => playlistReadServiceModel.CurrentSongExternalId = playlistReadServiceModel.Songs.FirstOrDefault(s => s.IsCurrent)?.ExternalId)
                .Tap(_ => playlistReadServiceModel.ActualListeningSecond = playlistReadServiceModel.Songs.FirstOrDefault(s => s.IsCurrent)?.SecondsListened)
                .Tap(_ => UserPlaylistStore.Set(userExternalId, playlistReadServiceModel))
                .Map(userSongs => playlistReadServiceModel);
        }

        private async Task<Result<PlaylistEntity>> GetTodayTemplatePlaylist(DateTime today)
        {
            var playlist = await PlaylistRepository.FirstOrDefaultAsync(new PlaylistSpecification().ByDate(today).WithSongs());

            if (playlist != null)
            {
                playlist.PlaylistSongs = playlist.PlaylistSongs.Where(s => !s.IsDeleted).ToList();
            }

            if (playlist is null || playlist.PlaylistSongs.Count == 0)
            {
                playlist = await PlaylistRepository.FirstOrDefaultAsync(new PlaylistSpecification().ByIsFallback().WithSongs());
                playlist.PlaylistSongs = playlist.PlaylistSongs.Where(s => !s.IsDeleted).ToList();
            }

            return Result.SuccessIf(playlist != null, playlist, ErrorMessages.PlaylistDoesNotExist);
        }

        private async Task<Result<User>> GetUserAsync(Guid userExternalId)
        {
            var user = await UserRepository.FirstOrDefaultAsync(new UserSpecification()
                .ByExternalId(userExternalId));

            return Result.SuccessIf(user != null, user, ErrorMessages.UserDoesNotExist);
        }

        private async Task<Result<PlaylistEntity>> GetPlaylistAsync(Guid playlistExternalId)
        {
            var playlist = await PlaylistRepository.FirstOrDefaultAsync(new PlaylistSpecification()
              .ByExternalId(playlistExternalId)
              .WithSongs()
              .WithCover());

            playlist.PlaylistSongs = playlist.PlaylistSongs.Where(t => !t.IsDeleted).ToList();

            return Result.SuccessIf(playlist != null, playlist, ErrorType.PlaylistNotFound.ToString());
        }

        private long GetMaxIdPlaylistAsync()
        {
            return PlaylistRepository.GetPlaylistMaxId();
        }

        private async Task<Result<UserPlaylistEntity>> CreateUserPlaylistAsync(
            User user,
            PlaylistEntity playlistEntity,
            DateTime today)
        {
            var userPlaylistSongs = new List<UserPlaylistSongEntity>();

            var playlist = await UserPlaylistRepository.AddAsync(new UserPlaylistEntity()
            {
                State = PlaylistState.NotStartedYet,
                UserId = user.Id,
                StartDate = today,
            });

            playlistEntity.PlaylistSongs.OrderBy(a => Guid.NewGuid()).ToList().ForEach(s =>
            {
                userPlaylistSongs.Add(new UserPlaylistSongEntity()
                {
                    SongId = s.SongId,
                    UserPlaylistId = playlist.Id,
                    Created = DateTime.UtcNow,
                });
            });

            await UserPlaylistRepository.UpdateAndSaveAsync(playlist);

            var firstPlaylistSong = userPlaylistSongs.FirstOrDefault();
            if (firstPlaylistSong != null)
            {
                firstPlaylistSong.IsCurrent = true;
            }

            var playlistSong = await UserPlaylistSongRepository.AddRangeAsync(userPlaylistSongs);

            return Result.SuccessIf(playlist != null, playlist, ErrorMessages.PlaylistDoesNotExist);
        }

        private async Task<Result<SongEntity>> GetSongsAsync(Guid songExternalId)
        {
            var song = await SongRepository.FirstOrDefaultAsync(new SongSpecification()
                .ByExternalId(songExternalId));

            return Result.SuccessIf(song != null, song, ErrorType.CannotFindSong.ToString());
        }

        private async Task<Result> AddSongToPlaylistRelationAsync(
            PlaylistEntity playlist,
            SongEntity songEntity)
        {
            var added = await PlaylistSongRepository.AddAsync(new PlaylistSongEntity()
            {
                PlaylistId = playlist.Id,
                SongId = songEntity.Id,
            });

            return Result.SuccessIf(added != null, ErrorMessages.CannotAddSongToPlaylist);
        }

        #endregion
    }
}
