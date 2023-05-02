#region Usings

using Hangfire;
using UltimatePlaylist.Common.Config;
using UltimatePlaylist.Services.Games.Jobs;

#endregion

namespace UltimatePlaylist.Services.Games
{
    public static class UltimatePayoutGameRewardScheduler
    {
        public static void ScheduleUltimatePayoutGameRewardJob(IRecurringJobManager recurringJobManager, string timeZone)
        {
            recurringJobManager.AddOrUpdate<UltimatePayoutGameRewardJob>(
                nameof(UltimatePayoutGameRewardJob),
                p => p.RunUltimatePayoutGameReward(),
                Cron.Yearly(),
                timeZone: TimeZoneInfo.FindSystemTimeZoneById(timeZone));
        }

        public static void RemoveUltimatePayoutGameRewardJob(IRecurringJobManager recurringJobManager)
        {
            recurringJobManager.RemoveIfExists(nameof(UltimatePayoutGameRewardJob));
        }
    }
}
