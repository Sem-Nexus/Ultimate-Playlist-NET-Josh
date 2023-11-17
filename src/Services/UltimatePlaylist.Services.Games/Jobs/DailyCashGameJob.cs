#region Usings

using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UltimatePlaylist.Common.Config;
using UltimatePlaylist.Common.Enums;
using UltimatePlaylist.Common.Mvc.Helpers;
using UltimatePlaylist.Database.Infrastructure.Entities.Games;
using UltimatePlaylist.Database.Infrastructure.Repositories.Interfaces;
using UltimatePlaylist.Games.Interfaces;
using UltimatePlaylist.Games.Models.Raffle;
using UltimatePlaylist.Services.Common.Interfaces.Games;
using UltimatePlaylist.Services.Common.Interfaces.Ticket;

#endregion

namespace UltimatePlaylist.Services.Games.Jobs
{
    public class DailyCashGameJob
    {
        private const int Selections = 18;

        #region Private members

        private readonly Lazy<IDailyCashTicketsService> DailyCashTicketsServiceProvider;

        private readonly Lazy<IRaffleService> RaffleServiceProvider;

        private readonly Lazy<IRepository<DailyCashDrawingEntity>> DailyCashDrawingRepositoryProvider;

        private readonly Lazy<IWinningsService> WinningsServiceProvider;

        private readonly Lazy<ILogger<DailyCashGameJob>> LoggerProvider;

        private readonly Lazy<IGamesWinningCollectionService> GamesWinningCollectionServiceProvider;

        private readonly PlaylistConfig PlaylistConfig;

        #endregion

        #region Constructor(s)

        public DailyCashGameJob(
            Lazy<IDailyCashTicketsService> dailyCashTicketsServiceProvider,
            Lazy<IRaffleService> raffleServiceProvider,
            Lazy<IRepository<DailyCashDrawingEntity>> dailyCashDrawingRepositoryProvider,
            Lazy<IWinningsService> winningsServiceProvider,
            Lazy<ILogger<DailyCashGameJob>> loggerProvider,
            Lazy<IGamesWinningCollectionService> gamesWinningCollectionServiceProvider, 
            IOptions<PlaylistConfig> playlistConfig)
        {
            DailyCashTicketsServiceProvider = dailyCashTicketsServiceProvider;
            RaffleServiceProvider = raffleServiceProvider;
            DailyCashDrawingRepositoryProvider = dailyCashDrawingRepositoryProvider;
            WinningsServiceProvider = winningsServiceProvider;
            LoggerProvider = loggerProvider;
            GamesWinningCollectionServiceProvider = gamesWinningCollectionServiceProvider;
            PlaylistConfig = playlistConfig.Value;
        }

        #endregion

        #region Properties

        private IDailyCashTicketsService DailyCashTicketsService => DailyCashTicketsServiceProvider.Value;

        private IRaffleService RaffleService => RaffleServiceProvider.Value;

        private IRepository<DailyCashDrawingEntity> DailyCashDrawingRepository => DailyCashDrawingRepositoryProvider.Value;

        private IWinningsService WinningsService => WinningsServiceProvider.Value;

        private ILogger<DailyCashGameJob> Logger => LoggerProvider.Value;

        private IGamesWinningCollectionService GamesWinningCollectionService => GamesWinningCollectionServiceProvider.Value;

        #endregion

        #region Public methods

        public async Task RunDailyCashGame()
        {
            DailyCashDrawingEntity game = default;
            List<RaffleUserTicketReadServiceModel> dailyCashTickets = default;

            var todayDate = DateTimeHelper.ToTodayUTCTimeForTimeZoneRelativeTime(PlaylistConfig.TimeZone);
            var currentDate = todayDate.Add(PlaylistConfig.StartDateOffSet);

            var result = await Result.Success()
                .Tap(async () => game = await DailyCashDrawingRepository.AddAsync(new DailyCashDrawingEntity()
                {
                    GameDate = currentDate,
                }))
                .Bind(async () => await DailyCashTicketsService.GetTicketsForDailyCashAsyncSP())
                .Ensure(tickets => tickets.Any(), ErrorType.NoTicketsForGame.ToString())
                .Tap(tickets => dailyCashTickets = tickets)
                .Bind(tickets => RaffleService.GetRaffleWinners(tickets, Selections))
                .Tap(async winners => await WinningsService.AddWinnersForDailyCashAsync(winners.Select(i => i.UserExternalId).ToList(), game.Id))
                .Tap(async winners => await DailyCashTicketsService.UseTickets(dailyCashTickets.Select(t => t.UserTicketExternalId)))
                .Tap(async winners => await GamesWinningCollectionService.RemoveArray(dailyCashTickets.Select(t => t.UserExternalId)));

            if (result.IsFailure && result.Error != ErrorType.NoTicketsForGame.ToString())
            {
                Logger.LogError($"Faliure durring daily cash drawining game. Error: {result.Error}");
            }
            else
            {
                game.IsFinished = true;
                await DailyCashDrawingRepository.UpdateAndSaveAsync(game);
            }
        }

        #endregion
    }
}
