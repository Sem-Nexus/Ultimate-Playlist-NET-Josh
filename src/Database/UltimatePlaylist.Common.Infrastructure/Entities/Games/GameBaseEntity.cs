#region Usings

using UltimatePlaylist.Common.Enums;
using UltimatePlaylist.Database.Infrastructure.Entities.Base;

#endregion

namespace UltimatePlaylist.Database.Infrastructure.Entities.Games
{
    public abstract class GameBaseEntity : BaseEntity
    {
        public DateTime GameDate { get; set; }

        public GameType Type { get; protected set; }

        public virtual ICollection<WinningEntity> Winnings { get; set; }

        public virtual ICollection<WinningAlternateEntity> WinningsAlternate { get; set; }

        public bool IsFinished { get; set; }
    }
}
