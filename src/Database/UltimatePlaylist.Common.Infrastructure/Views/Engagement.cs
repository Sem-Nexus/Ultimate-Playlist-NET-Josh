using UltimatePlaylist.Database.Infrastructure.Entities.Base;

namespace UltimatePlaylist.Database.Infrastructure.Views
{
    public class Engagement
    {
        public int TotalUsers { get; set; }

        public int TotalActiveUsers { get; set; }

        public int UserWithAppleLink { get; set; }

        public int TimeSpentOnApp { get; set; }

        public int TotalTicketsEarned { get; set; }

    }
}