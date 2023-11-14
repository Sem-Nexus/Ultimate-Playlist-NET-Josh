#region Usings

using AutoMapper;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using UltimatePlaylist.Common.Const;
using UltimatePlaylist.Common.Enums;
using UltimatePlaylist.Database.Infrastructure.Entities.Identity;
using UltimatePlaylist.Database.Infrastructure.Entities.Identity.Specifications;
using UltimatePlaylist.Database.Infrastructure.Entities.Playlist;
using UltimatePlaylist.Database.Infrastructure.Entities.Playlist.Specifications;
using UltimatePlaylist.Database.Infrastructure.Repositories.Interfaces;
using UltimatePlaylist.Services.Common.Interfaces.Playlist;
using UltimatePlaylist.Services.Common.Interfaces.Song;
using UltimatePlaylist.Services.Common.Interfaces.Ticket;
using UltimatePlaylist.Services.Common.Models.Analytics;
using UltimatePlaylist.Services.Common.Models.Playlist;
using UltimatePlaylist.Services.Common.Models.Song;
using UltimatePlaylist.Services.Common.Models.Ticket;

#endregion

namespace UltimatePlaylist.Services.Analytics
{
    public class AnalyticsService : IAnalyticsService
    {
        #region Private members

        private const int RetriesCount = 20;

        private readonly Lazy<IRepository<User>> UserRepositoryProvider;

        private readonly Lazy<ITicketStatsService> TicketStatsServiceProvider;

        private readonly Lazy<ITicketService> TicketServiceProvider;

        private readonly Lazy<IUserPlaylistService> UserPlaylistStoreProvider;

        private readonly Lazy<ISongSkippingService> SongSkippingServiceProvider;

        private readonly Lazy<IPlaylistService> PlaylistServiceProvider;

        private readonly Lazy<ISongSkippingDataService> SongSkippingDataServiceProvider;

        private readonly Lazy<ISongAntibotService> SongAntibotServiceProvider;
        private readonly Lazy<IPlaylistSQLRepository> UserPlaylistSqlRepositoryProvider;

        private readonly Lazy<IRepository<UserPlaylistEntity>> UserPlaylistRepositoryProvider;

        private readonly Lazy<IRepository<UserPlaylistSongEntity>> UserPlaylistSongRepositoryProvider;

        private readonly Lazy<IMapper> MapperProvider;

        private readonly Lazy<ILogger<AnalyticsService>> LoggerProvider;

        #endregion

        #region Constructor(s)

        public AnalyticsService(
            Lazy<IRepository<User>> userRepositoryProvider,
            Lazy<ITicketStatsService> ticketStatsServiceProvider,
            Lazy<ITicketService> ticketServiceProvider,
            Lazy<IUserPlaylistService> userPlaylistStoreProvider,
            Lazy<ISongSkippingService> songSkippingServiceProvider,
            Lazy<IPlaylistService> playlistServiceProvider,
            Lazy<IRepository<UserPlaylistEntity>> userPlaylistRepositoryProvider,
            Lazy<IRepository<UserPlaylistSongEntity>> userPlaylistSongRepositoryProvider,
            Lazy<IMapper> mapperProvider,
            Lazy<ISongSkippingDataService> songSkippingDataServiceProvider,
            Lazy<ILogger<AnalyticsService>> loggerProvider,
            Lazy<ISongAntibotService> songAntibotServiceProvider,
            Lazy<IPlaylistSQLRepository> userPlaylistSqlRepositoryProvider)
        {
            UserRepositoryProvider = userRepositoryProvider;
            TicketStatsServiceProvider = ticketStatsServiceProvider;
            TicketServiceProvider = ticketServiceProvider;
            UserPlaylistStoreProvider = userPlaylistStoreProvider;
            SongSkippingServiceProvider = songSkippingServiceProvider;
            PlaylistServiceProvider = playlistServiceProvider;
            UserPlaylistRepositoryProvider = userPlaylistRepositoryProvider;
            UserPlaylistSongRepositoryProvider = userPlaylistSongRepositoryProvider;
            MapperProvider = mapperProvider;
            SongSkippingDataServiceProvider = songSkippingDataServiceProvider;
            SongAntibotServiceProvider = songAntibotServiceProvider;
            UserPlaylistSqlRepositoryProvider = userPlaylistSqlRepositoryProvider;
            LoggerProvider = loggerProvider;
        }

        #endregion

        #region Properties

        private IRepository<User> UserRepository => UserRepositoryProvider.Value;

        private ITicketStatsService TicketStatsService => TicketStatsServiceProvider.Value;

        private ITicketService TicketService => TicketServiceProvider.Value;

        private IUserPlaylistService UserPlaylistStore => UserPlaylistStoreProvider.Value;

        private ISongSkippingService SongSkippingService => SongSkippingServiceProvider.Value;

        private IPlaylistService PlaylistService => PlaylistServiceProvider.Value;

        private ISongSkippingDataService SongSkippingDataService => SongSkippingDataServiceProvider.Value;

        private ISongAntibotService SongAntibotService => SongAntibotServiceProvider.Value;

        private IRepository<UserPlaylistEntity> UserPlaylistRepository => UserPlaylistRepositoryProvider.Value;

        private IRepository<UserPlaylistSongEntity> UserPlaylistSongRepository => UserPlaylistSongRepositoryProvider.Value;
        private IPlaylistSQLRepository UserPlaylistSqlRepository => UserPlaylistSqlRepositoryProvider.Value;

        private IMapper Mapper => MapperProvider.Value;

        private ILogger<AnalyticsService> Logger => LoggerProvider.Value;

        #endregion

        #region Public Method(s)

        public async Task<Result<AnalyticsLastEarnedTicketsReadServiceModel>> SaveAnalitycsDataAsync(
            Guid userExternalId,
            SaveAnalyticsDataWriteServiceModel saveAnalyticsDataWriteServiceModel)
        {
            return await GetUserAsync(userExternalId)
                .Check(async user => await SaveAnalyticsDataAsync(user, saveAnalyticsDataWriteServiceModel))
                .Map(async _ => await SongSkippingDataService.GetCurrentSkipDataAsync(saveAnalyticsDataWriteServiceModel.PlaylistExternalId, userExternalId))
                .Map(async skipData => Mapper.Map(skipData, await GetEarnedTicketsResponse(userExternalId)));
        }

        #endregion

        #region Private Method(s)

        private async Task<Result<User>> GetUserAsync(Guid userExternalId)
        {
            var user = await UserRepository.FirstOrDefaultAsync(new UserSpecification()
                .ByExternalId(userExternalId));

            return Result.SuccessIf(user != null, user, ErrorMessages.UserDoesNotExist);
        }

        private async Task<Result> SaveAnalyticsDataAsync(
            User user,
            SaveAnalyticsDataWriteServiceModel saveAnalyticsDataWriteServiceModel)
            => saveAnalyticsDataWriteServiceModel.EventType switch
            {
                AnalitycsEventType.StartSong => await SavePlaylisStateWithoutTickets(user, saveAnalyticsDataWriteServiceModel),
                AnalitycsEventType.ThirtySecondsOfSong => await SavePlaylisStateAndRewardTicketAsync(user, saveAnalyticsDataWriteServiceModel),
                AnalitycsEventType.SixtySecondsOfSong => await SavePlaylisStateAndRewardTicketAsync(user, saveAnalyticsDataWriteServiceModel),
                AnalitycsEventType.EntireSong => await SavePlaylisStateAndRewardTicketAsync(user, saveAnalyticsDataWriteServiceModel)
                    .Tap(async () => await SongAntibotService.AddNoActionAsync(user.ExternalId))
                    .Bind(async () => await SetSongStatus(user.ExternalId, saveAnalyticsDataWriteServiceModel)),
                AnalitycsEventType.BreakListening => await SavePlaylisStateWithoutTickets(user, saveAnalyticsDataWriteServiceModel),
                AnalitycsEventType.SkipSong => await SaveSongSkipAsync(user, saveAnalyticsDataWriteServiceModel),
                AnalitycsEventType.ExpirationOfSkipRefresh => Result.Success(),
                _ => throw new Exception(ErrorType.NotSupportedAnalyticsEventType.ToString())
            };

        private async Task<Result> SavePlaylisStateWithoutTickets(
             User user,
             SaveAnalyticsDataWriteServiceModel saveAnalyticsDataWriteServiceModel)
        {
            return await PlaylistService.GetTodaysPlaylist(user.ExternalId)
                .Tap(async userPlaylist => await SavePlaylistStateAsync(user.ExternalId, saveAnalyticsDataWriteServiceModel, userPlaylist));
        }

        private async Task<Result> SavePlaylisStateAndRewardTicketAsync(
            User user,
            SaveAnalyticsDataWriteServiceModel saveAnalyticsDataWriteServiceModel)
        {
            PlaylistReadServiceModel userPlaylist = default;

            return await PlaylistService.GetTodaysPlaylist(user.ExternalId)
                .Tap(userPlaylistReadServiceModel => userPlaylist = userPlaylistReadServiceModel)
                .Check(async userPlaylist => await CheckIfShouldEarnUltimateTicketsAsync(user.ExternalId, saveAnalyticsDataWriteServiceModel, userPlaylist))
                .Bind(async userPlaylist => await CheckIfShouldEarnTicketsForThreeSongsWithoutSkip(user.ExternalId, saveAnalyticsDataWriteServiceModel, userPlaylist))
                .Tap(async wasAwardedForThird => await SavePlaylistStateAsync(user.ExternalId, saveAnalyticsDataWriteServiceModel, userPlaylist, wasAwardedForThird))
                .Check(async _ => await AddTickets(user.ExternalId, saveAnalyticsDataWriteServiceModel));
        }

        private async Task<Result> SaveSongSkipAsync(
          User user,
          SaveAnalyticsDataWriteServiceModel saveAnalyticsDataWriteServiceModel)
        {
            return await PlaylistService.GetTodaysPlaylist(user.ExternalId)
                .Bind(async userPlaylist => await SongSkippingService.SkipSongAsync(
                new SkipSongWriteServiceModel()
                {
                    UserExternalId = user.ExternalId,
                    PlaylistExternalId = saveAnalyticsDataWriteServiceModel.PlaylistExternalId,
                    SongExternalId = saveAnalyticsDataWriteServiceModel.SongExternalId,
                    ActualListeningSecond = saveAnalyticsDataWriteServiceModel.ActualListeningSecond,
                }));
        }

        private async Task<AnalyticsLastEarnedTicketsReadServiceModel> GetEarnedTicketsResponse(Guid userExternalId)
        {
            var lastEarnedTickets = await TicketStatsService.UserTicketStatsAsync(userExternalId);
            return new AnalyticsLastEarnedTicketsReadServiceModel()
            {
                LatestEarnedTickets = lastEarnedTickets.IsSuccess ? lastEarnedTickets.Value.TicketsAmountForTodayDrawing : 0,
                IsAntiBotSystemActive = await SongAntibotService.ShouldActivateAsync(userExternalId),
            };
        }

        private async Task SavePlaylistStateAsync(
            Guid userExternalId,
            SaveAnalyticsDataWriteServiceModel saveAnalyticsDataWriteServiceModel,
            PlaylistReadServiceModel playlistReadServiceModel,
            bool isAwardedTicketForThird = false)
        {

            playlistReadServiceModel.CurrentSongExternalId = saveAnalyticsDataWriteServiceModel.SongExternalId;

            if (playlistReadServiceModel.State == PlaylistState.NotStartedYet)
            {
                playlistReadServiceModel.State = PlaylistState.InProgress;
            }

            if (playlistReadServiceModel.State == PlaylistState.InProgress
                && saveAnalyticsDataWriteServiceModel.EventType == AnalitycsEventType.EntireSong
                && playlistReadServiceModel.ExternalId == saveAnalyticsDataWriteServiceModel.PlaylistExternalId
                && playlistReadServiceModel.Songs.Last().ExternalId == saveAnalyticsDataWriteServiceModel.SongExternalId)
            {
                playlistReadServiceModel.State = PlaylistState.Finished;
            }

            if (isAwardedTicketForThird)
            {
                playlistReadServiceModel.LastPlaylistTicketSongExternalId = saveAnalyticsDataWriteServiceModel.SongExternalId;
            }

            await UserPlaylistStore.Set(userExternalId, playlistReadServiceModel);
            await UserPlaylistSqlRepository.UpdatePlaylistStateAndCurrentSong(
                playlistReadServiceModel.State.ToString(),
                userExternalId.ToString(),
                saveAnalyticsDataWriteServiceModel.PlaylistExternalId.ToString(),
                saveAnalyticsDataWriteServiceModel.SongExternalId.ToString()
                );

            /*
              var playlist = await UserPlaylistRepository.FirstOrDefaultAsync(new UserPlaylistSpecification()
                .ByExternalId(saveAnalyticsDataWriteServiceModel.PlaylistExternalId)
                .OrderByCreatedDescending()
                .WithSongs());

            Logger.LogInformation("\n\n SavePlaylistStateAsync.while-----------------------------------------------\n\n");


            if (playlist is not null)
            {
                bool isSuccess = false;
                var retriesCount = 0;

                while ((!isSuccess) && retriesCount < RetriesCount)
                { // TODO:
                    try
                    {

                        playlist.State = playlistReadServiceModel.State;
                        foreach (var userPlaylistSong in playlist.UserPlaylistSongs)
                        {
                            userPlaylistSong.IsCurrent = userPlaylistSong.Song.ExternalId == saveAnalyticsDataWriteServiceModel.SongExternalId;
                        }
                        var playlistStateStr = playlist.State.ToString();
                        await UserPlaylistSqlRepository.UpdatePlaylistState(playlistStateStr, playlist.Id);

                        isSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        retriesCount++;
                        Logger.LogError($"Exception occurred in AnalyticsService.cs {ex.Message}");
                        Thread.Sleep(3000);
                    }
                }
            }*/
        }

        private async Task<Result<bool>> CheckIfShouldEarnTicketsForThreeSongsWithoutSkip(
             Guid userExternalId,
             SaveAnalyticsDataWriteServiceModel saveAnalyticsDataWriteServiceModel,
             PlaylistReadServiceModel playlist)
        {
            if (saveAnalyticsDataWriteServiceModel.EventType != AnalitycsEventType.EntireSong)
            {
                return Result.Success(false);
            }

            var song = playlist.Songs.FirstOrDefault(x => x.ExternalId == saveAnalyticsDataWriteServiceModel.SongExternalId);
            if (song is null)
            {
                return Result.Failure<bool>(ErrorMessages.SongDoesNotExist);
            }

            var songIndex = playlist.Songs.IndexOf(song);
            var previousSongs = playlist.Songs.Skip(songIndex - 2).Take(3);

            if (songIndex < 2 || (previousSongs.Count() == 3 && previousSongs.Any(x => x.IsSkipped || x.ExternalId == playlist.LastPlaylistTicketSongExternalId)))
            {
                return Result.Success(false);
            }

            return await AddTickets(userExternalId, new SaveAnalyticsDataWriteServiceModel()
            {
                ActualListeningSecond = saveAnalyticsDataWriteServiceModel.ActualListeningSecond,
                EventType = AnalitycsEventType.ThreeSongsWithoutSkip,
                PlaylistExternalId = saveAnalyticsDataWriteServiceModel.PlaylistExternalId,
                SongExternalId = saveAnalyticsDataWriteServiceModel.SongExternalId,
            }).Map(earnedTickets => true);
        }

        private async Task<Result<bool>> CheckIfShouldEarnUltimateTicketsAsync(
             Guid userExternalId,
             SaveAnalyticsDataWriteServiceModel saveAnalyticsDataWriteServiceModel,
             PlaylistReadServiceModel playlist)
        {
            if (saveAnalyticsDataWriteServiceModel.EventType != AnalitycsEventType.ThirtySecondsOfSong)
            {
                return Result.Success(false);
            }

            var song = playlist.Songs.FirstOrDefault(x => x.ExternalId == saveAnalyticsDataWriteServiceModel.SongExternalId);
            if (song is null)
            {
                return Result.Failure<bool>(ErrorMessages.SongDoesNotExist);
            }

            var thirtySecondTickets = await TicketService.GetThirtySecondsTickets(userExternalId);

            var playlistSize = playlist.Songs.Count;
            var songIndex = playlist.Songs.IndexOf(song) + 1;

            /*Logger.LogError("=========== thirtySecondTickets =============");
            Logger.LogError(JsonConvert.SerializeObject(saveAnalyticsDataWriteServiceModel));
            Logger.LogError($"By PlaylistSOng: {thirtySecondTickets}");
            Logger.LogError("=========== thirtySecondTickets =============");*/

            if (((thirtySecondTickets + 1) == (playlistSize / 2)) || (playlistSize == (thirtySecondTickets + 1)))
            {
                return await TicketService.AddUserTicketAsync(
                 userExternalId,
                 new AddTicketWriteServiceModel()
                 {
                     ExternalId = saveAnalyticsDataWriteServiceModel.SongExternalId,
                     PlaylistExternalId = saveAnalyticsDataWriteServiceModel.PlaylistExternalId,
                     Type = TicketType.Jackpot,
                     EarnedType = ((playlistSize / 2) == (thirtySecondTickets + 1)) ? TicketEarnedType.HalfOfPlaylist : TicketEarnedType.FullPlaylist,
                 }).Map(earnedTickets => false);
            }

            return Result.Success(false);
        }

        private async Task<Result<EarnedTicketsReadServiceModel>> AddTickets(
            Guid userExternalId,
            SaveAnalyticsDataWriteServiceModel saveAnalyticsDataWriteServiceModel)
        {
            return await TicketService.AddUserTicketAsync(
                userExternalId,
                new AddTicketWriteServiceModel()
                {
                    ExternalId = saveAnalyticsDataWriteServiceModel.SongExternalId,
                    PlaylistExternalId = saveAnalyticsDataWriteServiceModel.PlaylistExternalId,
                    Type = TicketType.Daily,
                    EarnedType = GetTicketEarnedType(saveAnalyticsDataWriteServiceModel.EventType),
                });
        }

        private async Task<Result> SetSongStatus(Guid userExternalId, SaveAnalyticsDataWriteServiceModel saveAnalyticsDataWriteServiceModel)
        {
            var userPlaylistSong = await UserPlaylistSongRepository.FirstOrDefaultAsync(
                new UserPlaylistSongSpecification()
                .ByPlaylistExternalId(saveAnalyticsDataWriteServiceModel.PlaylistExternalId)
                .BySongExternalId(saveAnalyticsDataWriteServiceModel.SongExternalId)
                .ByUserExternalId(userExternalId)
                .WithPlaylist()
                .WithSong());

            return await Result.SuccessIf(userPlaylistSong is not null, ErrorMessages.SongDoesNotExist)
                .Tap(() => userPlaylistSong!.IsFinished = true)// 2020-10-04 fixed by Mohitt
                .Tap(async () => await UserPlaylistSongRepository.UpdateAndSaveAsync(userPlaylistSong));
        }

        private TicketEarnedType GetTicketEarnedType(AnalitycsEventType analitycsEventType)
            => analitycsEventType switch
            {
                AnalitycsEventType.ThirtySecondsOfSong => TicketEarnedType.ThirtySecondsOfListenedSong,
                AnalitycsEventType.SixtySecondsOfSong => TicketEarnedType.SixtySecondsOfListenedSong,
                AnalitycsEventType.EntireSong => TicketEarnedType.EntireSong,
                AnalitycsEventType.ThreeSongsWithoutSkip => TicketEarnedType.ThreeSongsWithoutSkip,
                _ => throw new Exception(ErrorType.NotSupportedEarnedTicketType.ToString())
            };

        #endregion
    }
}
