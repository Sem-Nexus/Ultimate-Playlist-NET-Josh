#region Usings

using UltimatePlaylist.Common.Enums;
using UltimatePlaylist.Database.Infrastructure.Entities.Games;
using UltimatePlaylist.Database.Infrastructure.Entities.Identity;
using UltimatePlaylist.Database.Infrastructure.Entities.Identity.Specifications;
using UltimatePlaylist.Database.Infrastructure.Repositories.Interfaces;
using UltimatePlaylist.Services.Common.Interfaces.Games;

#endregion

namespace UltimatePlaylist.Services.Games
{
    public class WinningsService : IWinningsService
    {
        #region Private members

        private readonly Lazy<IRepository<User>> UserRepositoryProvider;

        private readonly Lazy<IRepository<WinningEntity>> WinningRepositoryProvider;

        private readonly Lazy<IRepository<WinningAlternateEntity>> WinningAlternateRepositoryProvider;

        #endregion

        #region Constructor(s)

        public WinningsService(
            Lazy<IRepository<User>> userRepositoryProvider,
            Lazy<IRepository<WinningEntity>> winningRepositoryProvider,
            Lazy<IRepository<WinningAlternateEntity>> winningAlternateRepositoryProvider)
        {
            UserRepositoryProvider = userRepositoryProvider;
            WinningRepositoryProvider = winningRepositoryProvider;
            WinningAlternateRepositoryProvider = winningAlternateRepositoryProvider;
        }

        #endregion

        #region Properties

        private IRepository<User> UserRepository => UserRepositoryProvider.Value;

        private IRepository<WinningEntity> WinningRepository => WinningRepositoryProvider.Value;

        private IRepository<WinningAlternateEntity> WinningAlternateRepository => WinningAlternateRepositoryProvider.Value;

        #endregion

        #region Public methods

        public async Task AddWinnersForDailyCashAsync(IList<Guid> winnersExternalIds, long gameId)
        {
            var winners = new List<WinningEntity>();

            var users = await UserRepository.ListAsync(new UserSpecification().ByExternalIds(winnersExternalIds.ToArray()));

            for (int i = 0; i < winnersExternalIds.Count; i++)
            {
                winners.Add(new WinningEntity()
                {
                    Amount = GetAmmountByWinnerPlace(i),
                    GameId = gameId,
                    Status = WinningStatus.New,
                    WinnerId = users.First(c => c.ExternalId == winnersExternalIds[i]).Id,
                });
            }

            await WinningRepository.AddRangeAsync(winners);

            // TODO: Send Notifications to winners
        }

        public async Task AddAlternateWinnersForDailyCashAsync(IList<Guid> winnersExternalIds, long gameId)
        {
            var winners = new List<WinningAlternateEntity>();

            var users = await UserRepository.ListAsync(new UserSpecification().ByExternalIds(winnersExternalIds.ToArray()));

            for (int i = 0; i < winnersExternalIds.Count; i++)
            {
                winners.Add(new WinningAlternateEntity()
                {
                    Amount = GetAmmountByWinnerPlace(i),
                    GameId = gameId,
                    Status = WinningStatus.New,
                    WinnerId = users.First(c => c.ExternalId == winnersExternalIds[i]).Id,
                });
            }

            await WinningAlternateRepository.AddRangeAsync(winners);
        }

        public async Task AddWinnersForUltimateAsync(IList<Guid> winnersExternalIds, decimal reward, long gameId)
        {
            var winners = new List<WinningEntity>();
            var users = await UserRepository.ListAsync(new UserSpecification().ByExternalIds(winnersExternalIds.ToArray()));

            var splitReward = reward / winners.Count;
            foreach (var user in users)
            {
                winners.Add(new WinningEntity()
                {
                    Amount = splitReward,
                    GameId = gameId,
                    WinnerId = user.Id,
                    Status = WinningStatus.New,
                });
            }

            await WinningRepository.AddRangeAsync(winners);

            // TODO: Send Notifications to winners
        }

        #endregion

        #region Private methods

        // This was supposed to be configurable from admin panel
        private static decimal GetAmmountByWinnerPlace(int place)
        {
            if (place == 0)
            {
                return 500;
            }

            if (place < 3)
            {
                return 250;
            }

            if (place < 8)
            {
                return 100;
            }

            return 50;
        }

        #endregion
    }
}
