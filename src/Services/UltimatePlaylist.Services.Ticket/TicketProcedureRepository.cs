#region Usings

using System.Text;
using Microsoft.EntityFrameworkCore;
using UltimatePlaylist.Database.Infrastructure.Context;
using UltimatePlaylist.Database.Infrastructure.Entities.Ticket;
using UltimatePlaylist.Services.Common.Interfaces.Ticket;

#endregion

namespace UltimatePlaylist.Services.Song
{
    public class TicketProcedureRepository : ITicketProcedureRepository
    {
        private readonly EFContext Context;

        public TicketProcedureRepository(EFContext context)
        {
            Context = context;
        }

        public async Task<List<TicketEntity>> GetDailyTicketsForRaffle()
        {
            DateTime startDay = DateTime.Now;
            DateTime endDay = startDay.AddDays(1);
            var builder = new StringBuilder();
            builder.Append("[dbo].[GetDailyTickets]");
            builder.Append($"@Start = '{startDay.ToString("yyyy-MM-dd") + " 03:59:59"}',");
            builder.Append($"@End = '{endDay.ToString("yyyy-MM-dd") + " 04:00:00"}'");

            var data = await Context
                .GetDailyTickets
                .FromSqlRaw(builder.ToString())
                .ToListAsync();

            return data;
        }

        public async Task MarkDailyTicketsAsUsed(long start, long end)
        {

            var builder = new StringBuilder();
            builder.Append("[dbo].[MarkDailyTicketsAsUsed]");
            builder.Append($"@Start = '{end}',");
            builder.Append($"@End = '{start}'");

            await Context.Database.ExecuteSqlRawAsync(builder.ToString());
                
            return ;
        }
    }
}
