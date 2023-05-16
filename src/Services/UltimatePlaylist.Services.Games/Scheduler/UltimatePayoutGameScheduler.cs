#region Usings

using Hangfire;
using UltimatePlaylist.Common.Config;
using UltimatePlaylist.Services.Games.Jobs;

#endregion

namespace UltimatePlaylist.Services.Games
{
    public class UltimatePayoutGameScheduler
    {
        public static void ScheduleUltimatePayoutJob(IRecurringJobManager recurringJobManager, GamesConfig gamesConfig, PlaylistConfig playlistConfig)
        {
            recurringJobManager.AddOrUpdate<UltimatePayoutGameJob>(
                nameof(UltimatePayoutGameJob),
                p => p.RunUltimatePayoutGame(),
                Cron.Daily(playlistConfig.StartDateOffSet.Hours, playlistConfig.StartDateOffSet.Minutes + 12),
                timeZone: TimeZoneInfo.FindSystemTimeZoneById(playlistConfig.TimeZone));
        }

        public static void RemoveUltimatePayoutJob(IRecurringJobManager recurringJobManager)
        {
            recurringJobManager.RemoveIfExists(nameof(UltimatePayoutGameJob));
        }
    }
}
