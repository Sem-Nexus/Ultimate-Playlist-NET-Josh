﻿#region Usings

using UltimatePlaylist.Database.Infrastructure.Entities.Base;

#endregion

namespace UltimatePlaylist.Database.Infrastructure.Views
{
    public class UserCountView
    {
        public int UserCount { get; set; }

        public int ActiveUsers { get; set; }
    }
}
