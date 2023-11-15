#region Usings

using System;

#endregion

namespace UltimatePlaylist.MobileApi.Models.Song
{
    public class UserSongResponseModel
    {
        public Guid ExternalId { get; set; }

        public string Artist { get; set; }

        public string Title { get; set; }

        public string OwnerLabel { get; set; }

        public string FeaturedArtist { get; set; }

        public string AudioFileStreamingUrl { get; set; }

        public string CoverFileUrl { get; set; }

        public int UserRating { get; set; }

        public bool IsAddedToSpotify { get; set; }

        public bool IsAddedToAppleMusic { get; set; }

        public bool IsSkipped { get; set; }

        public TimeSpan Duration { get; set; }

        public int? SecondsListened { get; set; }
    }
}
