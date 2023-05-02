#region Usings

using CSharpFunctionalExtensions;
using DocumentFormat.OpenXml.VariantTypes;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UltimatePlaylist.Common.Config;
using UltimatePlaylist.Common.Enums;
using UltimatePlaylist.Common.Mvc.Helpers;
using UltimatePlaylist.Database.Infrastructure.Entities.Games;
using UltimatePlaylist.Database.Infrastructure.Repositories.Interfaces;
using UltimatePlaylist.Games.Interfaces;
using UltimatePlaylist.Games.Models.Raffle;
using UltimatePlaylist.Services.Common.Interfaces.Games;
using UltimatePlaylist.Services.Common.Interfaces.Ticket;
using System;
using System.Linq;
using StackExchange.Redis;
using UltimatePlaylist.Games.Models.Lottery;
#endregion

namespace UltimatePlaylist.Services.Games.Jobs
{
    public class DailyCashTicketsServiceAndRaffleJob
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

        public DailyCashTicketsServiceAndRaffleJob(
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

        public static DailyCashDrawingEntity Game;

        #endregion

        #region Public methods

        public async Task<LotteryWinnersReadServiceModel> RunDailyCashGame()
        {
            var result = await GetTicketsAndWinners();

            var jobId = BackgroundJob.Schedule(() => CreateGame(), TimeSpan.FromMinutes(2));

            BackgroundJob.ContinueJobWith(jobId, () => AddWinnersAndUseTickets(result), JobContinuationOptions.OnlyOnSucceededState);

            return result;
        }

        public async Task<LotteryWinnersReadServiceModel> GetTicketsAndWinners()
        {
            List<RaffleUserTicketReadServiceModel> dailyCashTickets = default;

            try
            {
                var tickets = await DailyCashTicketsService.GetTicketsForDailyCashAsync();

                if (tickets.Value.Any())
                {
                    dailyCashTickets = tickets.Value;
                    var winners = RaffleService.GetRaffleWinners(dailyCashTickets, Selections).Value;

                    if (winners.Count() == 0)
                    {
                        Logger.LogError($"Error getting raffle winners");
                        throw new Exception("Error in GetRaffleWinners");
                    }
                    if (winners.Count() > 18)
                    {
                        winners = winners.Take(18);
                    }

                    var response = new LotteryWinnersReadServiceModel();
                    response.RaffleUserTicketReadServiceModel = winners;
                    response.LotteryWinnersReadServiceModels = tickets.Value;
                    response.Counter = winners.Count();

                    return response;
                }
                else
                {
                    throw new Exception("Error in GetTicketsForDailyCashAsync");
                }
            }
            catch (Exception ex)
            {
                return null;
            }


        }

        public async Task<DailyCashDrawingEntity> CreateGame()
        {
            DailyCashDrawingEntity game = default;

            var todayDate = DateTimeHelper.ToTodayUTCTimeForTimeZoneRelativeTime("US Eastern Standard Time");
            TimeSpan dateOffSet = TimeSpan.Parse("0:00:00");
            var currentDate = todayDate.Add(PlaylistConfig.StartDateOffSet);

            try
            {
                var result = await Result.Success()
                .Tap(async () => game = await DailyCashDrawingRepository.AddAsync(new DailyCashDrawingEntity()
                {
                    GameDate = currentDate,
                }));
            }
            catch
            {
                throw new Exception("Error in CreateGame");
            }
            Game = game;
            return game;

        }

        public async Task<DailyCashDrawingEntity> AddWinnersAndUseTickets(LotteryWinnersReadServiceModel result)
        {

            if (Game != null)
            {
                try
                {
                    await WinningsService.AddWinnersForDailyCashAsync(result.RaffleUserTicketReadServiceModel.Select(i => i.UserExternalId).ToList(), Game.Id);
                    await DailyCashTicketsService.UseTickets(result.LotteryWinnersReadServiceModels.Select(t => t.UserTicketExternalId));
                    await GamesWinningCollectionService.RemoveArray(result.LotteryWinnersReadServiceModels.Select(t => t.UserExternalId));

                    Game.IsFinished = true;
                    await DailyCashDrawingRepository.UpdateAndSaveAsync(Game);
                }
                catch
                {
                    throw new Exception("Error in AddWinnersAndUseTickets");
                }


            }

            return Game;


        }
        #endregion
    }
}
