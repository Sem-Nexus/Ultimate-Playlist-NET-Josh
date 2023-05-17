#region Usings

using UltimatePlaylist.Database.Infrastructure.Entities.Ticket;
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
    }
}
