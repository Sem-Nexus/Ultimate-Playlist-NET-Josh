#region Usings

using AutoMapper;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UltimatePlaylist.Common.Config;
using UltimatePlaylist.Common.Mvc.Helpers;
using UltimatePlaylist.Database.Infrastructure.Entities.Games;
using UltimatePlaylist.Database.Infrastructure.Entities.Games.Specifications;
using UltimatePlaylist.Database.Infrastructure.Repositories.Interfaces;
using UltimatePlaylist.Games.Interfaces;
using UltimatePlaylist.Games.Models.Lottery;
using UltimatePlaylist.Services.Common.Interfaces.Games;
using UltimatePlaylist.Services.Games.Exceptions;
using UltimatePlaylist.Services.Games.Validators;

#endregion

namespace UltimatePlaylist.Services.Games.Jobs
{
    public class UltimatePayoutGameJob
    {
        #region Private members

        private readonly Lazy<IRepository<UltimatePayoutEntity>> UltimatePayoutRepositoryProvider;

        private readonly Lazy<IRepository<UserLotteryEntryEntity>> UserLotteryEntryRepositoryProvider;

        private readonly Lazy<ILogger<UltimatePayoutGameJob>> LoggerProvider;

        private readonly Lazy<IWinningsService> WinningsServiceProvider;

        private readonly Lazy<IUserTicketsLotteryService> UserTicketsLotteryServiceProvider;

        private readonly Lazy<ILotteryService> LotteryGameServiceProvider;

        private readonly GamesConfig GamesConfig;

        private readonly PlaylistConfig PlaylistConfig;

        #endregion

        #region Constructor(s)

        public UltimatePayoutGameJob(
            Lazy<IWinningsService> winningsServiceProvider,
            Lazy<ILotteryService> lotteryGameServiceProvider,
            Lazy<IUserTicketsLotteryService> userTicketsLotteryServiceProvider,
            Lazy<ILogger<UltimatePayoutGameJob>> loggerProvider,
            Lazy<IRepository<UltimatePayoutEntity>> ultimatePayoutRepositoryProvider,
            Lazy<IRepository<UserLotteryEntryEntity>> userLotteryEntryRepositoryProvider,
            IOptions<GamesConfig> gamesOptions,
            IOptions<PlaylistConfig> playlistConfig)
        {
            WinningsServiceProvider = winningsServiceProvider;
            LotteryGameServiceProvider = lotteryGameServiceProvider;
            UserTicketsLotteryServiceProvider = userTicketsLotteryServiceProvider;
            LoggerProvider = loggerProvider;
            UltimatePayoutRepositoryProvider = ultimatePayoutRepositoryProvider;
            UserLotteryEntryRepositoryProvider = userLotteryEntryRepositoryProvider;
            GamesConfig = gamesOptions.Value;
            PlaylistConfig = playlistConfig.Value;
        }

        #endregion

        #region Properties

        private IRepository<UltimatePayoutEntity> UltimatePayoutRepository => UltimatePayoutRepositoryProvider.Value;

        private IRepository<UserLotteryEntryEntity> UserLotteryEntryRepository => UserLotteryEntryRepositoryProvider.Value;

        private ILogger<UltimatePayoutGameJob> Logger => LoggerProvider.Value;

        private IWinningsService WinningsService => WinningsServiceProvider.Value;

        private IUserTicketsLotteryService UserTicketsLotteryService => UserTicketsLotteryServiceProvider.Value;

        private ILotteryService LotteryGameService => LotteryGameServiceProvider.Value;

        #endregion

        #region Public methods

        public async Task RunUltimatePayoutGame()
        {
            var todayDate = DateTimeHelper.ToTodayUTCTimeForTimeZoneRelativeTime(PlaylistConfig.TimeZone);
            var currentDate = todayDate.Add(PlaylistConfig.StartDateOffSet);

            var todaysGame = await UltimatePayoutRepository.FirstOrDefaultAsync(
                 new UltimatePayoutSpecification()
                 .ByGameDate(currentDate)
                 .ByIsFinished(false));

            if (todaysGame is null)
            {
                todaysGame = await UltimatePayoutRepository.AddAsync(new UltimatePayoutEntity()
                {
                    GameDate = currentDate,
                    Reward = GamesConfig.UltimateBaseReward,
                });
            }

            var nextGameReward = todaysGame.Reward;
            var result = LotteryGameService.GetLotteryWinningNumbers();

            if (result.IsFailure)
            {
                var message = $"Faliure durring ultimate payout game. Error: {result.Error}";
                Logger.LogError(message);
                throw new UlttimatePayoutGameJobException(message);
            }

            var numbers = result.Value;
            var validator = new UltimatePayoutGameValidator();
            var validation = validator.Validate(numbers);

            if (!validation.IsValid)
            {
                var message = $"Faliure durring ultimate payout game. Error: Numbers validation failed.";
                Logger.LogError(message);
                throw new UlttimatePayoutGameJobException(message);
            }

            await UserTicketsLotteryService.AddAllUnusedLotteryTickets(todaysGame.Id);
          
            var winners = await UserLotteryEntryRepository.ListAsync(
                    new UserLotteryEntryEntitySpecification()
                    .ByGameId(todaysGame.Id)
                    .ByGameNumbers(
                        numbers.FirstNumber,
                        numbers.SecondNumber,
                        numbers.ThirdNumber,
                        numbers.FourthNumber,
                        numbers.FifthNumber,
                        numbers.SixthNumber)
                    .WithUser());

            if (winners.Any())
            {
                await WinningsService.AddWinnersForDailyCashAsync(winners.Select(i => i.User.ExternalId).ToList(), todaysGame.Id);
                nextGameReward = GamesConfig.UltimateBaseReward;
            }

            DateTime firstDayOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
            if(currentDate.Day == firstDayOfMonth.Day)
            {
                nextGameReward += 5000;
            }

            todaysGame.FirstNumber = numbers.FirstNumber;
            todaysGame.SecondNumber = numbers.SecondNumber;
            todaysGame.ThirdNumber = numbers.ThirdNumber;
            todaysGame.FourthNumber = numbers.FourthNumber;
            todaysGame.FifthNumber = numbers.FifthNumber;
            todaysGame.SixthNumber = numbers.SixthNumber;
            todaysGame.IsFinished = true;

            await UltimatePayoutRepository.UpdateAndSaveAsync(todaysGame);
            await UltimatePayoutRepository.AddAsync(new UltimatePayoutEntity()
            {
                GameDate = currentDate.AddDays(1),
                Reward = nextGameReward,
            });
        }

        #endregion
    }
}
