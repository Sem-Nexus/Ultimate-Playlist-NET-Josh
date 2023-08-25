namespace UltimatePlaylist.Services.Common.Interfaces.Games
{
    public interface IWinningsService
    {
        Task AddWinnersForDailyCashAsync(IList<Guid> winnersExternalIds, long gameId);

        Task AddAlternateWinnersForDailyCashAsync(IList<Guid> winnersExternalIds, long gameId);

        Task AddWinnersForUltimateAsync(IList<Guid> winnersExternalIds, decimal reward, long gameId);
    }
}
