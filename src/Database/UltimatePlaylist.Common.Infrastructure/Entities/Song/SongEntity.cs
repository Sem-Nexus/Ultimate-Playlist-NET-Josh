#region Usings

using System;
using System.Collections.Generic;
using UltimatePlaylist.Database.Infrastructure.Entities.Base;
using UltimatePlaylist.Database.Infrastructure.Entities.File;
using UltimatePlaylist.Database.Infrastructure.Entities.Playlist;
using UltimatePlaylist.Database.Infrastructure.Entities.UserSongHistory;

#endregion

namespace UltimatePlaylist.Database.Infrastructure.Entities.Song
{
    public class SongEntity : BaseEntity
    {
        #region Constructor(s)
        public SongEntity()
        {
            SongGenres = new List<SongGenreEntity>();
            SongSocialMedias = new List<SongSocialMediaEntity>();
            PlaylistSongs = new List<PlaylistSongEntity>();
            SongDSPs = new List<SongDSPEntity>();
            UserSongHistory = new List<UserSongHistoryEntity>();
            UserPlaylistSongs = new List<UserPlaylistSongEntity>();
        }

        #endregion

        #region Service

        public string Artist { get; set; }

        public string Title { get; set; }

        public string Album { get; set; }

        public string OwnerLabel { get; set; }

        public string FeaturedArtist { get; set; }

        public string Licensor { get; set; }

        public string Songwriter { get; set; }

        public string Producer { get; set; }

        public DateTime? FirstReleaseDate { get; set; }

        public bool? IsNewRelease { get; set; }

        public long AudioFileId { get; set; }

        public long CoverFileId { get; set; }

        public bool? IsAudioOriginal { get; set; }

        public bool? IsArtWorkOriginal { get; set; }

        public bool? HasExplicitContent { get; set; }

        public bool? HasSample { get; set; }

        public bool? HasLegalClearance { get; set; }

        public bool? IsConfirmed { get; set; }

        public TimeSpan Duration { get; set; }

        #endregion

        #region Navigation properties

        public virtual AudioFileEntity AudioFile { get; set; }

        public virtual CoverFileEntity CoverFile { get; set; }

        public virtual ICollection<SongGenreEntity> SongGenres { get; set; }

        public virtual ICollection<SongSocialMediaEntity> SongSocialMedias { get; set; }

        public virtual ICollection<PlaylistSongEntity> PlaylistSongs { get; set; }

        public virtual ICollection<SongDSPEntity> SongDSPs { get; set; }

        public virtual ICollection<UserSongHistoryEntity> UserSongHistory { get; set; }

        public virtual ICollection<UserPlaylistSongEntity> UserPlaylistSongs { get; set; }

        #endregion
    }
}
