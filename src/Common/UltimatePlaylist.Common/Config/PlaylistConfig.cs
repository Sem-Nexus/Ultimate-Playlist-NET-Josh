#region Usings

using System;

#endregion

namespace UltimatePlaylist.Common.Config
{
    public class PlaylistConfig
    {
        public int RequiredPlaylistSongsCount { get; set; }

        public TimeSpan StartDateOffSet { get; set; }

        public int SongSkippingLimit { get; set; }

        public TimeSpan SongSkippingLimitTime { get; set; }

        public int AntibotSongsCount { get; set; }

        public string TimeZone { get; set; }

        public string ExcelSheetId { get; set; }
    }
}
