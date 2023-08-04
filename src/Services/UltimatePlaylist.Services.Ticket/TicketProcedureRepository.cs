#region Usings

using System.Text;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using UltimatePlaylist.Database.Infrastructure.Context;
using UltimatePlaylist.Database.Infrastructure.Entities.Ticket;
using UltimatePlaylist.Database.Infrastructure.Views;
using UltimatePlaylist.Services.Common.Interfaces.Ticket;
using UltimatePlaylist.Services.Common.Models.Games;

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
            DateTime endDay = DateTime.Now.AddDays(-1);
            var builder = new StringBuilder();
            builder.Append("[dbo].[GetDailyTickets]");
            builder.Append($"@Start = '{endDay.ToString("yyyy-MM-dd") + " 03:59:59"}',");
            builder.Append($"@End = '{startDay.ToString("yyyy-MM-dd") + " 04:00:00"}'");

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

            return;
        }

        public async Task RemoveInternalUserTickets()
        {
            var builder = new StringBuilder();
            builder.Append("[dbo].[RemoveInternalUsersDailyTickets]");

            await Context.Database.ExecuteSqlRawAsync(builder.ToString());

            return;
        }
        
        public async Task<List<WinnersInformationEntity>> GetWinnersData()
        {
            var data = await Context
                .GetWinnersInformation                
                .ToListAsync();

            return data.OrderByDescending(item => item.PrizeTier).ToList();
        }

        public async Task<UserCountView> GetTotalUsers()
        {
            var builder = new StringBuilder();
            builder.Append("[dbo].[GetTotalUsers]");

            var data = await Context
                .SoungCount
                .FromSqlRaw(builder.ToString())
                .ToListAsync();

            return data?.FirstOrDefault();

        }

        public async Task<List<DailyUsersView>> GetDailyUsersAdded()
        {
            var builder = new StringBuilder();
            builder.Append("[dbo].[GetAllUsers]");

            var data = await Context
                .DailyUsers
                .FromSqlRaw(builder.ToString())
                .ToListAsync();

            return data;

        }

        public async Task<Result<ActiveUsers>> GetActiveUserTokensCount()
        {

            var birthDateMax = DateTime.UtcNow;
            var birthDateMin = DateTime.Parse("1800-01-01");
            var genders = string.Empty;
            var zipCode = string.Empty;

            var builder = new StringBuilder();
            builder.Append("[dbo].[ActiveUsers]");
            builder.Append($"@BirthDateMax = '{GetDate(birthDateMin)}',");
            builder.Append($"@BirthDateMin = '{GetDate(birthDateMax)}',");
            builder.Append($"@Gender = '{genders}',");            
            builder.Append($"@ZipCode = '{zipCode}'");

            var result = await Context
                .ActiveUsers
                .FromSqlRaw(builder.ToString()).ToListAsync();

            return Result.Success(result.FirstOrDefault());

        }

        private string GetDate(DateTime date)
        {
            return date.ToString("yyyy'-'MM'-'dd");
        }

    }
}
