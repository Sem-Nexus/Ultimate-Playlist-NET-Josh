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
    public class DeactivatedUsers : BaseEntity
    {

        #region Service        
        public string UserName { get; set; }

        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        public string Name { get; set; }

        public long? GenderId { get; set; }

        public string LastName { get; set; }
     
        public string ZipCode { get; set; }

        public DateTime? LastActive { get; set; }

        public string Device { get; set; }

        #endregion

    }
}
