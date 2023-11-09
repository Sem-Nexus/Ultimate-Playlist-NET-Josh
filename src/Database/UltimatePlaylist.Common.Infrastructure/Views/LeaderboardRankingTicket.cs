using UltimatePlaylist.Database.Infrastructure.Entities.Base;

namespace UltimatePlaylist.Database.Infrastructure.Views
{
    public class LeaderboardRankingTicket
    {
        public long? RankingPosition { get; set; }
        public string AvatarUrl { get; set; }
        public string FullName { get; set; }
        public string UserName { get; set; }
        public long TicketCount { get; set; }
        public Guid ExternalId { get; set; }
    }
}