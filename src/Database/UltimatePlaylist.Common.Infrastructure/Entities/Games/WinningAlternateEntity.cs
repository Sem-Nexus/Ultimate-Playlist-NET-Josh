#region Usings

using UltimatePlaylist.Common.Enums;
using UltimatePlaylist.Database.Infrastructure.Entities.Base;
using UltimatePlaylist.Database.Infrastructure.Entities.Identity;

#endregion

namespace UltimatePlaylist.Database.Infrastructure.Entities.Games
{
    public class WinningAlternateEntity : BaseEntity
    {
        public decimal Amount { get; set; }

        public WinningStatus Status { get; set; }

        public long GameId { get; set; }

        public virtual GameBaseEntity Game { get; set; }

        public long WinnerId { get; set; }

        public virtual User Winner { get; set; }
    }
}
