#region Usings

using AutoMapper;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Options;
using UltimatePlaylist.Common.Config;
using UltimatePlaylist.Common.Const;
using UltimatePlaylist.Common.Enums;
using UltimatePlaylist.Common.Extensions;
using UltimatePlaylist.Common.Mvc.Helpers;
using UltimatePlaylist.Database.Infrastructure.Entities.Games;
using UltimatePlaylist.Database.Infrastructure.Entities.Games.Specifications;
using UltimatePlaylist.Database.Infrastructure.Entities.Identity;
using UltimatePlaylist.Database.Infrastructure.Entities.Identity.Specifications;
using UltimatePlaylist.Database.Infrastructure.Entities.Ticket;
using UltimatePlaylist.Database.Infrastructure.Entities.Ticket.Specifications;
using UltimatePlaylist.Database.Infrastructure.Repositories.Interfaces;
using UltimatePlaylist.Services.Common.Interfaces.Games;
using UltimatePlaylist.Services.Common.Models.Games;

#endregion

namespace UltimatePlaylist.Services.Games
{
    public class GamesInfoService : IGamesInfoService
    {
        #region Private members

        private readonly Lazy<IRepository<GameBaseEntity>> GameBaseEntityReposiotryProvider;

        private readonly Lazy<IRepository<TicketEntity>> TicketRepositoryProvider;

        private readonly Lazy<IMapper> MapperProvider;

        private readonly Lazy<IRepository<WinningEntity>> WinningRepositoryProvider;

        private readonly Lazy<IRepository<UltimatePayoutEntity>> UltimatePayoutEntityReposiotryProvider;

        private readonly Lazy<IGamesWinningCollectionService> GamesWinningCollectionServiceProvider;

        private readonly Lazy<IRepository<User>> UserRepositoryProvider;

        private readonly PlaylistConfig PlaylistConfig;

        #endregion

        #region Constructor(s)

        public GamesInfoService(
             Lazy<IRepository<User>> userRepositoryProvider,
            Lazy<IMapper> mapperProvider,
            Lazy<IRepository<TicketEntity>> ticketRepositoryProvider,
            Lazy<IRepository<WinningEntity>> winningRepositoryProvider,
            Lazy<IGamesWinningCollectionService> gamesWinningCollectionServiceProvider,
            Lazy<IRepository<GameBaseEntity>> gameBaseEntityReposiotryProvider,
            IOptions<PlaylistConfig> playlistConfig,
            Lazy<IRepository<UltimatePayoutEntity>> ultimatePayoutEntityReposiotryProvider)
        {
            MapperProvider = mapperProvider;
            UserRepositoryProvider = userRepositoryProvider;
            TicketRepositoryProvider = ticketRepositoryProvider;
            WinningRepositoryProvider = winningRepositoryProvider;
            GamesWinningCollectionServiceProvider = gamesWinningCollectionServiceProvider;
            GameBaseEntityReposiotryProvider = gameBaseEntityReposiotryProvider;
            PlaylistConfig = playlistConfig.Value;
            UltimatePayoutEntityReposiotryProvider = ultimatePayoutEntityReposiotryProvider;
        }

        #endregion

        #region Properties

        private IRepository<User> UserRepository => UserRepositoryProvider.Value;

        private IRepository<GameBaseEntity> GameBaseEntityReposiotry => GameBaseEntityReposiotryProvider.Value;

        private IRepository<UltimatePayoutEntity> UltimatePayoutEntityReposiotry => UltimatePayoutEntityReposiotryProvider.Value;

        private IRepository<TicketEntity> TicketRepository => TicketRepositoryProvider.Value;

        private IRepository<WinningEntity> WinningRepository => WinningRepositoryProvider.Value;

        private IMapper Mapper => MapperProvider.Value;

        private IGamesWinningCollectionService GamesWinningCollectionService => GamesWinningCollectionServiceProvider.Value;

        #endregion

        #region Public Methods
        public async Task<Result<NotificationTimeDiffModel>> GetNotificationTimeDiff()
        {
            var todayDate = DateTimeHelper.ToTodayUTCTimeForTimeZoneRelativeTime(PlaylistConfig.TimeZone);
            var now = DateTimeHelper.ToUTCTimeForTimeZoneRelativeTime(DateTime.UtcNow, PlaylistConfig.TimeZone);
            var currentDate = todayDate.Add(PlaylistConfig.StartDateOffSet);

            var nextNotificationAfterGame = todayDate;
            var nextNotificationBeforeGame = todayDate.AddMinutes(-15);
            var nextNotificationReminder = todayDate.AddHours(-4);

            var nextDate = (now < currentDate) ? currentDate : currentDate.AddDays(1);
            var timeDiff = Convert.ToInt32(Math.Floor((nextDate - now).TotalSeconds));

            var nextAfterDate = (now < nextNotificationAfterGame) ? nextNotificationAfterGame : nextNotificationAfterGame.AddDays(1);
            var timeDiffAfter = Convert.ToInt32(Math.Floor((nextAfterDate - now).TotalSeconds));

            var nextBeforeDate = (now < nextNotificationBeforeGame) ? nextNotificationBeforeGame : nextNotificationBeforeGame.AddDays(1);
            var timeDiffBefore = Convert.ToInt32(Math.Floor((nextBeforeDate - now).TotalSeconds));

            var nextReminderDate = (now < nextNotificationReminder) ? nextNotificationReminder : nextNotificationReminder.AddDays(1);
            var timeDiffReminder = Convert.ToInt32(Math.Floor((nextReminderDate - now).TotalSeconds));

            var notifiactionInfo = new NotificationTimeDiffModel()
            {
                NextDrawingDate = timeDiff,
                NextNotificationAfterGame = timeDiffAfter,
                NextNotificationBeforeGame = timeDiffBefore,
                NextNotificationReminder = timeDiffReminder,
            };

            return Result.Success(notifiactionInfo);
        }
        public async Task<Result<GamesinfoReadServiceModel>> GetGamesInfoAsync(Guid userExternalId)
        {
            var todayDate = DateTimeHelper.ToTodayUTCTimeForTimeZoneRelativeTime(PlaylistConfig.TimeZone);
            var now = DateTimeHelper.ToUTCTimeForTimeZoneRelativeTime(DateTime.UtcNow, PlaylistConfig.TimeZone);
            var currentDate = todayDate.Add(PlaylistConfig.StartDateOffSet);
            
            var nextDate = (now < currentDate) ? currentDate : currentDate.AddDays(1);
            var timeDiff = Convert.ToInt32(Math.Floor((nextDate - now).TotalSeconds));

            var avaiableTodayTicketsCount = await TicketRepository.CountAsync(new TicketSpecification()
                .WithUser()
                .BySongHistoryUserExternalIdUsingSongRelation(userExternalId)
                .ByTodaysTickets()
                .ByType(TicketType.Daily)
                .OnlyNotUsed());

            avaiableTodayTicketsCount += await TicketRepository.CountAsync(new TicketSpecification()
                .WithUserByPlaylist()
                .ByUserExternalIdUsingPlaylistRelation(userExternalId)
                .ByTodaysTickets()
                .ByType(TicketType.Daily)
                .OnlyNotUsed());

            var winnings = await WinningRepository.ListAsync(
                   new WinningSpecification()
                   .ByUserExternalId(userExternalId)
                   .ByStatus(WinningStatus.New)
                   .WithUser());

            var lastUltimateGame = await UltimatePayoutEntityReposiotry.FirstOrDefaultAsync(
                new UltimatePayoutSpecification()
                .ByIsFinished(true)
                .ByGameDate(currentDate));

            var isUnclaimed = await GamesWinningCollectionService.Get(userExternalId);
            var user = await UserRepository.FirstOrDefaultAsync(new UserSpecification()
                .ByExternalId(userExternalId)
                );
            
            string timezoneId = "US Eastern Standard Time";
            var nowTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, timezoneId);
            TimeZoneInfo targetTimezone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            double offsetHours = targetTimezone.GetUtcOffset(DateTime.UtcNow).TotalHours;
            
            var isCreatedTodayUser = user.Created.AddHours(offsetHours).Date.Equals(nowTime.Date);  
            var gamesInfo = new GamesinfoReadServiceModel()
            {
                NextDrawingDate = timeDiff,
                NextUltimateDate = timeDiff,
                IsUnclaimed = false, //isCreatedTodayUser ? false : isUnclaimed is null,
                NextUltimatePrize = lastUltimateGame is not null ? lastUltimateGame.Reward : 20000,
                TicketsCount = avaiableTodayTicketsCount,
                UnclaimedWinnings = Mapper.Map<List<UserWinningReadServicModel>>(winnings),
            };

            return Result.Success(gamesInfo);
        }

        
        public async Task<Result<GamesinfoReadServiceModel>> CheckNewestGame(Guid userExternalId)
        {
            var newestGame = await GameBaseEntityReposiotry.FirstOrDefaultAsync(new GameBaseEntitySpecification().OrderByCreated(true));
            var user = await UserRepository.FirstOrDefaultAsync(new UserSpecification()
                .ByExternalId(userExternalId)
                );

            string timezoneId = "US Eastern Standard Time";
            var nowTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, timezoneId);
            TimeZoneInfo targetTimezone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            double offsetHours = targetTimezone.GetUtcOffset(DateTime.UtcNow).TotalHours;

            var isCreatedTodayUser = user.Created.AddHours(offsetHours).Date.Equals(nowTime.Date);

            return await Result.SuccessIf(isCreatedTodayUser || (newestGame is not null && newestGame.IsFinished), ErrorMessages.GameNotYetFinished)
                .Bind(async () => await GetGamesInfoAsync(userExternalId));
        }

        public async Task<Result<GamesinfoReadServiceModel>> ClaimWinningsAsync(Guid userExternalId)
        {
            var winnings = await WinningRepository.ListAsync(
                new WinningSpecification()
                .WithUser()
                .ByStatus(WinningStatus.New)
                .ByUserExternalId(userExternalId));

            return await Result.Success(winnings)
                .TapIf(winnings is not null, async () =>
                {
                    foreach (var winning in winnings!)
                    {
                        winning.Status = WinningStatus.Pending;
                    }

                    await WinningRepository.UpdateAndSaveRangeAsync(winnings);
                })
                .Tap(async winnings => await GamesWinningCollectionService.Set(userExternalId))
                .Bind(async winnings => await GetGamesInfoAsync(userExternalId));
        }

        #endregion
    }
}
