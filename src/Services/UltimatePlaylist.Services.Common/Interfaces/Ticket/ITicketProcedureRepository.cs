#region Usings

using CSharpFunctionalExtensions;
using UltimatePlaylist.Database.Infrastructure.Entities.Identity;
using UltimatePlaylist.Database.Infrastructure.Entities.Ticket;
using UltimatePlaylist.Database.Infrastructure.Views;
using UltimatePlaylist.Services.Common.Models.Games;

#endregion

namespace UltimatePlaylist.Services.Common.Interfaces.Ticket
{
    public interface ITicketProcedureRepository
    {
        Task MarkDailyTicketsAsUsed(long start, long end);

        Task<List<TicketEntity>> GetDailyTicketsForRaffle();

        Task RemoveInternalUserTickets();

        Task<List<WinnersInformationEntity>> GetWinnersData();

        Task<List<WinnersAlternateInformationEntity>> GetWinnersAlternateData();

        Task<UserCountView> GetTotalUsers();

        Task<List<DailyUsersView>> GetDailyUsersAdded();

        Task<List<DeactivatedUsers>> GetDeactivateUsersAdded();        

        Task<Result<ActiveUsers>> GetActiveUserTokensCount();

        Task<TicketCount> TicketCount(Guid userExternalId);

        Task<List<LeaderboardRankingTicket>> LeaderboardRankingTicket(Guid userExternalId);

    }
}
