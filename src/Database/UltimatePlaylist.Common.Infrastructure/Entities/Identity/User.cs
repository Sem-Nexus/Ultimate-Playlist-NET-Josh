#region Usings

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using UltimatePlaylist.Database.Infrastructure.Entities.Base;
using UltimatePlaylist.Database.Infrastructure.Entities.Dsp;
using UltimatePlaylist.Database.Infrastructure.Entities.File;
using UltimatePlaylist.Database.Infrastructure.Entities.Games;
using UltimatePlaylist.Database.Infrastructure.Entities.Playlist;
using UltimatePlaylist.Database.Infrastructure.Entities.UserSongHistory;

#endregion

namespace UltimatePlaylist.Database.Infrastructure.Entities.Identity
{
    public class User : IdentityUser<long>, IBaseEntity
    {
        public User()
        {
            Created = DateTime.UtcNow;
            Roles = new List<UserRole>();
            Dsps = new List<UserDspEntity>();
            UserSongsHistory = new List<UserSongHistoryEntity>();
        }

        #region Service

        public string RefreshTokenHash { get; set; }

        public DateTime Created { get; set; }

        public DateTime? Updated { get; set; }

        public bool IsDeleted { get; set; }

        public Guid ExternalId { get; set; }

        public string ConfirmationCode { get; set; }

        public DateTime ConfirmationCodeExiprationDate { get; set; }

        public string ResetToken { get; set; }

        public DateTime ResetTokenCodeExiprationDate { get; set; }

        public string Name { get; set; }

        public string LastName { get; set; }

        public DateTime BirthDate { get; set; }

        public bool IsActive { get; set; }

        public string Pin { get; set; }

        public long? GenderId { get; set; }

        public long? AvatarFileId { get; set; }

        public bool IsNotificationEnabled { get; set; }

        public bool IsSmsPromotionalNotificationEnabled { get; set; }

        public string ZipCode { get; set; }

        public DateTime? LastActive { get; set; }

        public string NewNotConfirmedEmail { get; set; }

        public bool IsEmailChangeConfirmedFromWeb { get; set; }

        public string UserToeknRequiredByWebConfirmation { get; set; }

        public bool IsAgeVerified { get; set; }

        public string DeviceToken { get; set; }

        public bool ShouldNotificationBeEnabled { get; set; }

        #endregion

        #region Navigation properties

        public virtual ICollection<UserRole> Roles { get; set; }

        public virtual GenderEntity Gender { get; set; }

        public virtual AvatarFileEntity AvatarFile { get; set; }

        public virtual ICollection<UserDspEntity> Dsps { get; set; }

        public virtual ICollection<UserSongHistoryEntity> UserSongsHistory { get; set; }

        public virtual ICollection<UserPlaylistEntity> UserPlaylists { get; set; }

        public virtual ICollection<WinningEntity> Winnings { get; set; }

        public virtual ICollection<WinningAlternateEntity> WinningsAlternate { get; set; }

        public virtual ICollection<UserLotteryEntryEntity> UserLotteryEntries { get; set; }

        #endregion
    }
}
