#region Usings
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using System;
using UltimatePlaylist.Common.Config;
using UltimatePlaylist.Services.Games.Jobs;

#endregion

namespace UltimatePlaylist.Services.Games
{
    public static class DailyCashTicketsServiceAndRaffleScheduler
    {
        public static void SchedulealiCashDrawingJob(IRecurringJobManager recurringJobManager, IBackgroundJobClient backgroundJobClient, GamesConfig gamesConfig, PlaylistConfig playlistConfig)
        {
            playlistConfig.TimeZone = "US Eastern Standard Time";
            recurringJobManager.AddOrUpdate<DailyCashTicketsServiceAndRaffleJob>("Remove Internal Users Tickets", (p) => p.RemoveInternalUsersTickets(),
              Cron.Daily(playlistConfig.StartDateOffSet.Hours, playlistConfig.StartDateOffSet.Minutes),
              timeZone: TimeZoneInfo.FindSystemTimeZoneById(playlistConfig.TimeZone));


            recurringJobManager.AddOrUpdate<DailyCashTicketsServiceAndRaffleJob>("Get Tickets and Winners", (p) => p.RunDailyCashGame(),
              Cron.Daily(playlistConfig.StartDateOffSet.Hours, playlistConfig.StartDateOffSet.Minutes + 1),
              timeZone: TimeZoneInfo.FindSystemTimeZoneById(playlistConfig.TimeZone));

            recurringJobManager.AddOrUpdate<DailyCashTicketsServiceAndRaffleJob>("Email Winners", (p) => p.CreateExcelAndSendEmail(),
              Cron.Daily(playlistConfig.StartDateOffSet.Hours, playlistConfig.StartDateOffSet.Minutes + 17),
              timeZone: TimeZoneInfo.FindSystemTimeZoneById(playlistConfig.TimeZone));

        }

        public static void RemoveDaliCashDrawingJobs(IRecurringJobManager recurringJobManager)
        {
            recurringJobManager.RemoveIfExists(nameof(DailyCashTicketsServiceAndRaffleJob));
        }
    }
}
