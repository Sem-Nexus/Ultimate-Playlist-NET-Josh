#region Usings

using UltimatePlaylist.Database.Infrastructure.Entities.Ticket;

#endregion

namespace UltimatePlaylist.Services.Common.Interfaces.Ticket
{
    public interface ITicketProcedureRepository
    {
        Task MarkDailyTicketsAsUsed(long start, long end);

        Task<List<TicketEntity>> GetDailyTicketsForRaffle();

        Task RemoveInternalUserTickets();
    }
}
