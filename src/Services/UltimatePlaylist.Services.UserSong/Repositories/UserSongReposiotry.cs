#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using UltimatePlaylist.Database.Infrastructure.Context;
using UltimatePlaylist.Database.Infrastructure.Entities.Song;
using UltimatePlaylist.Database.Infrastructure.Entities.Song.Specifications;
using UltimatePlaylist.Database.Infrastructure.Repositories;
using UltimatePlaylist.Services.Common.Interfaces.UserSong;
using UltimatePlaylist.Services.Common.Models.Song;

#endregion

namespace UltimatePlaylist.Services.UserSong.Repositories
{
    public class UserSongReposiotry : ReadOnlyRepository<SongEntity>, IUserSongRepository
    {
        public UserSongReposiotry(EFContext context, Lazy<IMapper> mapperProvider)
            : base(context, mapperProvider)
        {
        }

        public async Task<IEnumerable<UserSongReadServiceModel>> GetUserPlaylistSongsAsync(Guid userExternalId, Guid playlistExternalId)
        {
            var selectSpec = new SongSpecification()
                .ByPlaylistExternalId(playlistExternalId)
                .WithUserPlaylistSongs()
                .WithCover()
                .WithAudioFile();

            var userPlaylistSongs = Context
                .UserPlaylistSongs
                .Include(i => i.UserPlaylist)
                .Where(s => s.UserPlaylist.ExternalId == playlistExternalId);

            var userSongHistory = Context.UserSongsHistory
                .Include(i => i.User)
                .Where(s => s.User.ExternalId == userExternalId);

            return await ApplySpecification(selectSpec)
                .Where(song => !song.UserPlaylistSongs.Single(s => s.UserPlaylist.ExternalId == playlistExternalId && s.Song.ExternalId == song.ExternalId).IsDeleted)
                .Select(song => new UserSongReadServiceModel
                {
                    Created = song.Created,
                    ExternalId = song.ExternalId,
                    Duration = song.Duration,
                    Artist = song.Artist,
                    FeaturedArtist = song.FeaturedArtist,
                    OwnerLabel = song.OwnerLabel,
                    Title = song.Title,
                    AudioFileStreamingUrl = song.AudioFile.StreamingUrl,
                    CoverFileUrl = song.CoverFile.Url,
                    Updated = song.Updated,
                    SecondsListened = userPlaylistSongs.FirstOrDefault(s => s.SongId == song.Id) != null ? userPlaylistSongs.FirstOrDefault(s => s.SongId == song.Id).SecondsListened : 0,
                    IsSkipped = userPlaylistSongs.FirstOrDefault(s => s.SongId == song.Id) != null ? userPlaylistSongs.FirstOrDefault(s => s.SongId == song.Id).IsSkipped : false,
                    IsAddedToAppleMusic = userSongHistory.FirstOrDefault(s => s.SongId == song.Id) != null && userSongHistory.FirstOrDefault(s => s.SongId == song.Id).IsAddedToAppleMusic,
                    IsAddedToSpotify = userSongHistory.FirstOrDefault(s => s.SongId == song.Id) != null && userSongHistory.FirstOrDefault(s => s.SongId == song.Id).IsAddedToSpotify,
                    UserRating = userPlaylistSongs.FirstOrDefault(s => s.SongId == song.Id) != null ? userPlaylistSongs.FirstOrDefault(s => s.SongId == song.Id).Rating : null,
                    IsCurrent = userPlaylistSongs.FirstOrDefault(s => s.SongId == song.Id) != null ? userPlaylistSongs.FirstOrDefault(s => s.SongId == song.Id).IsCurrent : false,
                    AddedToUserPlaylistDate = userPlaylistSongs.FirstOrDefault(s => s.SongId == song.Id) != null ? userPlaylistSongs.FirstOrDefault(s => s.SongId == song.Id).Created : null,
                })
                .OrderBy(s => s.AddedToUserPlaylistDate)
                .ToListAsync();
        }
}
}
