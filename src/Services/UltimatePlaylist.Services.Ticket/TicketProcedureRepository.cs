﻿#region Usings

using System.Text;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using UltimatePlaylist.Database.Infrastructure.Context;
using UltimatePlaylist.Database.Infrastructure.Entities.Identity;
using UltimatePlaylist.Database.Infrastructure.Entities.Ticket;
using UltimatePlaylist.Database.Infrastructure.Views;
using UltimatePlaylist.Services.Common.Interfaces.Ticket;
using UltimatePlaylist.Services.Common.Models.Games;
using UltimatePlaylist.Services.Common.Models.Ticket;

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

        public async Task<List<DeactivatedUsers>> GetDeactivateUsersAdded()
        {
            var data = await Context
                .DeactivatedUsers.ToListAsync();

            return data.OrderByDescending(_ => _.Updated).ToList();
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

        public async Task<List<WinnersAlternateInformationEntity>> GetWinnersAlternateData()
        {
            var data = await Context
                .GetWinnersAlternateInformation
                .ToListAsync();

            return data.OrderByDescending(item => item.PrizeTier).ToList();
        }

        public async Task<TicketCount> TicketCount(Guid userExternalId)
        {

            string timezoneId = "US Eastern Standard Time";
            var now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, timezoneId); // converted utc timezone to EST timezone
            TimeZoneInfo targetTimezone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            double offsetHours = targetTimezone.GetUtcOffset(DateTime.UtcNow).TotalHours;
            string ticketType = "Daily";

            var builder = new StringBuilder();
            builder.Append("[dbo].[TicketCountBySongHistory]");
            builder.Append($"@offsetHours = '{offsetHours}',");
            builder.Append($"@DateNow = '{now.Date}',");
            builder.Append($"@ExternalId = '{userExternalId}',");
            builder.Append($"@TicketType = '{ticketType}'");

            var result = await Context
                .TicketCount
                .FromSqlRaw(builder.ToString()).ToListAsync();

            return result.FirstOrDefault();

        }

        public async Task<List<LeaderboardRankingTicket>> LeaderboardRankingTicket(Guid userExternalId)
        {
            var builder = new StringBuilder();
            builder.Append("[dbo].[LeaderboardTicketCountRankingSP]");
            builder.Append($"@ExternalId = '{userExternalId}'");

            var result = await Context
                .LeaderboardTicket
                .FromSqlRaw(builder.ToString()).ToListAsync();

            return result;

        }

        public async Task BackupTicketsAndUpdateLeaderboard()
        {

            await BackupTickets();
            await UpdateLeaderboard();

            return;
        }

        public async Task BackupTickets()
        {
            var builder = new StringBuilder();
            builder.Append("[dbo].[BackupDailyUserTickets]");

            await Context.Database.ExecuteSqlRawAsync(builder.ToString());

            return;
        }

        public async Task UpdateLeaderboard()
        {
            var builder = new StringBuilder();
            builder.Append("[dbo].[UserTicketsCount]");

            await Context.Database.ExecuteSqlRawAsync(builder.ToString());

            return;
        }

        public async Task AddTicketForPlaylistActionAsync(
            AddTicketForPlaylistActionServiceModel addTickets
            )
        {
    
            var builder = new StringBuilder();
            builder.Append("[dbo].[AddTicketForPlaylistActionAsync]");
            builder.Append($"@UserExternalId = '{addTickets.UserExternalId.ToString()}',");
            builder.Append($"@PlaylistExternalId = '{addTickets.PlaylistExternalId.ToString()}',");
            builder.Append($"@SongExternalId = '{addTickets.SongExternalId.ToString()}',");
            builder.Append($"@TicketType = '{addTickets.TicketType.ToString()}',");
            builder.Append($"@EarnedType = '{addTickets.EarnedType.ToString()}',");
            builder.Append($"@TicketQty = {addTickets.TicketQty.ToString()}");

            await Context
                .Database
                .ExecuteSqlRawAsync(builder.ToString());

        }
    }
}
