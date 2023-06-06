#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using UltimatePlaylist.Common.Enums;
using UltimatePlaylist.Common.Extensions;
using UltimatePlaylist.Common.Models;
using UltimatePlaylist.Database.Infrastructure.Entities.File;
using UltimatePlaylist.Database.Infrastructure.Entities.File.Specifications;
using UltimatePlaylist.Database.Infrastructure.Entities.Playlist;
using UltimatePlaylist.Database.Infrastructure.Entities.Playlist.Specifications;
using UltimatePlaylist.Database.Infrastructure.Entities.Song;
using UltimatePlaylist.Database.Infrastructure.Entities.Song.Specifications;
using UltimatePlaylist.Database.Infrastructure.Entities.Specifications;
using UltimatePlaylist.Database.Infrastructure.Repositories.Interfaces;
using UltimatePlaylist.Services.Common.Interfaces.Song;
using UltimatePlaylist.Services.Common.Models;
using UltimatePlaylist.Services.Common.Models.Song;

#endregion

namespace UltimatePlaylist.Services.Song
{
    public class SongService : ISongService
    {
        #region Private members

        private readonly Lazy<IRepository<SongEntity>> SongRepositoryProvider;

        private readonly Lazy<IReadOnlyRepository<CoverFileEntity>> CoverFileRepositoryProvider;

        private readonly Lazy<IReadOnlyRepository<AudioFileEntity>> AudioFileRepositoryProvider;

        private readonly Lazy<IReadOnlyRepository<GenreEntity>> GenreRepositoryProvider;

        private readonly Lazy<IRepository<SongGenreEntity>> SongGenreRepositoryProvider;

        private readonly Lazy<IRepository<SongSocialMediaEntity>> SongSocialMediaRepositoryProvider;

        private readonly Lazy<IRepository<SongDSPEntity>> SongDSPRepositoryProvider;

        private readonly Lazy<IRepository<PlaylistSongEntity>> PlaylistSongProvider;

        private readonly Lazy<ILogger<SongService>> LoggerProvider;

        private readonly Lazy<IMapper> MapperProvider;

        #endregion

        #region Constructor(s)

        public SongService(
            Lazy<IRepository<SongEntity>> songRepositoryProvider,
            Lazy<IReadOnlyRepository<CoverFileEntity>> coverFileRepositoryProvider,
            Lazy<IReadOnlyRepository<AudioFileEntity>> audioFileRepositoryProvider,
            Lazy<IReadOnlyRepository<GenreEntity>> genreRepositoryProvider,
            Lazy<IRepository<SongGenreEntity>> songGenreRepositoryProvider,
            Lazy<IRepository<SongSocialMediaEntity>> songSocialMediaRepositoryProvider,
            Lazy<IRepository<SongDSPEntity>> songDSPRepositoryProvider,
            Lazy<IRepository<PlaylistSongEntity>> playlistSongProvider,
            Lazy<ILogger<SongService>> loggerProvider,
            Lazy<IMapper> mapperProvider)
        {
            SongRepositoryProvider = songRepositoryProvider;
            CoverFileRepositoryProvider = coverFileRepositoryProvider;
            AudioFileRepositoryProvider = audioFileRepositoryProvider;
            GenreRepositoryProvider = genreRepositoryProvider;
            SongGenreRepositoryProvider = songGenreRepositoryProvider;
            SongSocialMediaRepositoryProvider = songSocialMediaRepositoryProvider;
            SongDSPRepositoryProvider = songDSPRepositoryProvider;
            PlaylistSongProvider = playlistSongProvider;
            LoggerProvider = loggerProvider;
            MapperProvider = mapperProvider;
        }

        #endregion

        #region Properties

        private IRepository<SongEntity> SongRepository => SongRepositoryProvider.Value;

        private IReadOnlyRepository<CoverFileEntity> CoverFileRepository => CoverFileRepositoryProvider.Value;

        private IReadOnlyRepository<AudioFileEntity> AudioFileRepository => AudioFileRepositoryProvider.Value;

        private IReadOnlyRepository<GenreEntity> GenreRepository => GenreRepositoryProvider.Value;

        private IRepository<SongGenreEntity> SongGenreRepository => SongGenreRepositoryProvider.Value;

        private IRepository<SongSocialMediaEntity> SongSocialMediaRepository => SongSocialMediaRepositoryProvider.Value;

        private IRepository<SongDSPEntity> SongDSPRepository => SongDSPRepositoryProvider.Value;

        private IRepository<PlaylistSongEntity> PlaylistSongRepository => PlaylistSongProvider.Value;

        private ILogger<SongService> Logger => LoggerProvider.Value;

        private IMapper Mapper => MapperProvider.Value;

        #endregion

        #region Public Method(s)

        public async Task<Result> AddSongAsync(AddSongWriteServiceModel addSongWriteServiceModel)
        {

            if (addSongWriteServiceModel.FirstPublicReleaseDate == null) { addSongWriteServiceModel.FirstPublicReleaseDate = DateTime.Now;}
            var entityToSave = Mapper.Map<SongEntity>(addSongWriteServiceModel);

            return await GetSongCoverFile(addSongWriteServiceModel.SongCoverExternalId)
                .Tap(cover => entityToSave.CoverFileId = cover.Id)
                .Bind(async _ => await GetSongAudioFile(addSongWriteServiceModel.SongFileExternalId))
                .Tap(audio => entityToSave.AudioFileId = audio.Id)
                .Bind(async _ => await AddSongToDatabase(entityToSave))
                .CheckIf(addSongWriteServiceModel.PrimaryGenres.Count > 0, async song => await GetSongGenres(addSongWriteServiceModel.PrimaryGenres)
                    .Check(async primaryGenres => await AddSongGenres(song, primaryGenres, SongGenreType.Primary)))
                .CheckIf(addSongWriteServiceModel.SecondaryGenres.Count > 0, async song => await GetSongGenres(addSongWriteServiceModel.SecondaryGenres)
                    .Check(async secondaryGenres => await AddSongGenres(song, secondaryGenres, SongGenreType.Secondary)))
                .Check(async song => await AddSongSocialMedia(song, addSongWriteServiceModel, false))
                .Check(async song => await AddSongDSPLinks(song, addSongWriteServiceModel, false));
        }

        public async Task<Result> RemoveSongAsync(RemoveSongWriteServiceModel removeSongWriteServiceModel)
        {
            return await Result.Success()
                .Tap(async () => await SongRepository.DeleteAsync(new SongSpecification()
                    .ByExternalId(removeSongWriteServiceModel.ExternalId)))
                .Tap(async () => await RemoveSongFromPlannedPlaylists(removeSongWriteServiceModel.ExternalId));
        }

        public async Task<Result<PaginatedReadServiceModel<SongReadServiceModel>>> SongsListAsync(
            Pagination pagination)
        {
            var count = await SongRepository.CountAsync(new SongSpecification());

            return await GetSongsAsync(pagination)
                .Map(songs => Mapper.Map<IReadOnlyList<SongReadServiceModel>>(songs))
                .Map(songs => new PaginatedReadServiceModel<SongReadServiceModel>(songs, pagination, count));
        }

        #endregion

        #region Private Method(s)

        private async Task<Result<SongEntity>> AddSongToDatabase(SongEntity songEntity)
        {
            SongEntity added = default;
            songEntity.FirstReleaseDate = songEntity.FirstReleaseDate == null ? DateTime.Now : songEntity.FirstReleaseDate;
            songEntity.IsNewRelease = songEntity.IsNewRelease == null ? false : songEntity.IsNewRelease;
            songEntity.IsAudioOriginal = songEntity.IsAudioOriginal == null ? false : songEntity.IsAudioOriginal;
            songEntity.IsArtWorkOriginal = songEntity.IsArtWorkOriginal == null ? false : songEntity.IsArtWorkOriginal;
            songEntity.HasExplicitContent = songEntity.HasExplicitContent == null ? false : songEntity.HasExplicitContent;
            songEntity.HasSample = songEntity.HasSample == null ? false : songEntity.HasSample;
            
            try
            {
                added = await SongRepository.AddAsync(songEntity);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return Result.SuccessIf(added != null, added, ErrorType.CannotAddSongToDatabase.ToString());
        }

        private async Task<Result<CoverFileEntity>> GetSongCoverFile(Guid coverFileExternalId)
        {
            var coverFileEntity = await CoverFileRepository.FirstOrDefaultAsync(new CoverFileSpecification()
                .ByExternalId(coverFileExternalId));

            return Result.SuccessIf(coverFileEntity != null, coverFileEntity, ErrorType.CannotFindSongCoverFile.ToString());
        }

        private async Task<Result<AudioFileEntity>> GetSongAudioFile(Guid audioFileExternalId)
        {
            var audioFileEntity = await AudioFileRepository.FirstOrDefaultAsync(new AudioFileSpecification()
                .ByExternalId(audioFileExternalId));

            return Result.SuccessIf(audioFileEntity != null, audioFileEntity, ErrorType.CannotFindSongAudioFile.ToString());
        }

        private async Task<Result<IReadOnlyList<GenreEntity>>> GetSongGenres(IList<Guid> genres)
        {
            var genreEntities = await GenreRepository.ListAsync(new GenreSpecification()
                .ByExternalIds(genres));

            return Result.SuccessIf(genreEntities.Count > 0, genreEntities, ErrorType.CannotFindGenres.ToString());
        }

        private async Task<Result> AddSongGenres(
            SongEntity song,
            IReadOnlyList<GenreEntity> genreEntities,
            SongGenreType songGenreType)
        {
            var songGenres = new List<SongGenreEntity>();

            genreEntities.ForEach(s => songGenres.Add(new SongGenreEntity()
            {
                Type = songGenreType,
                Song = song,
                SongId = song.Id,
                GenreId = s.Id,
            }));

            var addedSongGenres = await SongGenreRepository.AddRangeAsync(songGenres);

            return Result.SuccessIf(addedSongGenres.Count() > 0, ErrorType.CannotAddSongGenres.ToString());
        }

        private async Task<Result> AddSongSocialMedia(
            SongEntity song,
            AddSongWriteServiceModel addSongWriteServiceModel,
            bool editable)
        {
            var songSocialMedias = new List<SongSocialMediaEntity>();

            CheckIfSocialMediaCanBeAdded(addSongWriteServiceModel.InstagramUrl, song, SocialMediaType.Instagram, songSocialMedias);
            CheckIfSocialMediaCanBeAdded(addSongWriteServiceModel.FacebookUrl, song, SocialMediaType.Facebook, songSocialMedias);
            CheckIfSocialMediaCanBeAdded(addSongWriteServiceModel.SnapchatUrl, song, SocialMediaType.Snapchat, songSocialMedias);
            CheckIfSocialMediaCanBeAdded(addSongWriteServiceModel.YoutubeUrl, song, SocialMediaType.Youtube, songSocialMedias);

            if (editable)
            {
                await SongSocialMediaRepository.DeleteAsync((new SongSocialMediaSpecification()
                  .BySongId(song.Id)));
            }

            if (songSocialMedias.Count > 0)
            {              
                var addedSonSocialMedias = await SongSocialMediaRepository.AddRangeAsync(songSocialMedias);

                return Result.SuccessIf(addedSonSocialMedias.Count() > 0, ErrorType.CannotAddSongSocialMedia.ToString());
            }

            return Result.Success();
        }

        private void CheckIfSocialMediaCanBeAdded(
            string url,
            SongEntity song,
            SocialMediaType socialMediaType,
            List<SongSocialMediaEntity> songSocialMedias)
        {
            if (!string.IsNullOrEmpty(url))
            {
                songSocialMedias.Add(GenerateSongSocialMediaEntity(song, url, socialMediaType));
            }
        }

        private SongSocialMediaEntity GenerateSongSocialMediaEntity(
             SongEntity song,
             string url,
             SocialMediaType socialMediaType)
        {
            return new SongSocialMediaEntity()
            {
                Type = socialMediaType,
                Url = url,
                SongId = song.Id,
            };
        }

        private async Task<Result> AddSongDSPLinks(
            SongEntity song,
            AddSongWriteServiceModel addSongWriteServiceModel,
            bool editable)
        {
            var songDSPLinks = new List<SongDSPEntity>();

            if (editable)
            {
                await SongDSPRepository.DeleteAsync((new SongDSPSpecification()
                    .BySongId(song.Id)));
            }
            songDSPLinks.Add(GenerateSongDSPEntity(song, addSongWriteServiceModel.LinkToAppleMusic, DspType.AppleMusic));
            if (!String.IsNullOrEmpty(addSongWriteServiceModel.LinkToSpotify))
            {
                songDSPLinks.Add(GenerateSongDSPEntity(song, addSongWriteServiceModel.LinkToSpotify, DspType.Spotify));
            }
            

            var addedDSPs = await SongDSPRepository.AddRangeAsync(songDSPLinks);

            return Result.SuccessIf(addedDSPs.Count() > 0, ErrorType.CannotAddSongDspLinks.ToString());
        }

        private SongDSPEntity GenerateSongDSPEntity(
             SongEntity song,
             string url,
             DspType dspType)
        {
            return new SongDSPEntity()
            {
                DspType = dspType,
                Url = url,
                SongId = song.Id,
                SongDspId = dspType == DspType.Spotify
                    ? GetSpotifySongUri(url)
                    : GetAppleMusicSongUri(url),
            };
        }

        private async Task<Result<IReadOnlyList<SongEntity>>> GetSongsAsync(
            Pagination pagination)
        {
            var songs = await SongRepository.ListAsync(new SongSpecification
                .Paged(pagination)
                .WithGenres()
                .WithCover()
                .Search(pagination.SearchValue));

            return Result.Success(songs);
        }

        private string GetSpotifySongUri(string url)
        {
            var startIndex = url.IndexOf("track/") + "track/".Length;
            int endIndex = url.LastIndexOf("?si");

            return url.Substring(startIndex, endIndex - startIndex);
        }

        private string GetAppleMusicSongUri(string url)
        {
            var startIndex = url.IndexOf("?i=") + "?i=".Length;

            return url.Substring(startIndex);
        }

        private async Task RemoveSongFromPlannedPlaylists(Guid sonExternalId)
        {
            await PlaylistSongRepository.DeleteAsync(new PlaylistSongSpecification()
                .BySongExternalId(sonExternalId)
                .ByNewerThanTodayDate());
        }

        public async Task<Result<SongEntity>> EditSongAsync(AddSongWriteServiceModel addSongWriteServiceModel)
        {
            SongEntity editedSong = await SongRepository.FirstOrDefaultAsync(new SongSpecification()
                  .ByExternalId(addSongWriteServiceModel.SongCoverExternalId));


            editedSong.Artist = addSongWriteServiceModel.Artist;
            editedSong.Title = addSongWriteServiceModel.Title;
            editedSong.Album = addSongWriteServiceModel.Album;
            editedSong.OwnerLabel = addSongWriteServiceModel.OwnerLabel;
            editedSong.Licensor = addSongWriteServiceModel.Licensor;
            editedSong.Songwriter = addSongWriteServiceModel.SongWriter;
            editedSong.Producer = addSongWriteServiceModel.Producer;
            editedSong.FeaturedArtist = addSongWriteServiceModel.FeaturedArtist;
            editedSong.FirstReleaseDate = addSongWriteServiceModel.FirstPublicReleaseDate == null ? DateTime.Now : addSongWriteServiceModel.FirstPublicReleaseDate;
            editedSong.IsNewRelease = addSongWriteServiceModel.IsNewRelease;
            editedSong.IsArtWorkOriginal = addSongWriteServiceModel.IsAllArtworkOriginal;
            editedSong.IsAudioOriginal = addSongWriteServiceModel.IsAllAudioOriginal;
            editedSong.HasExplicitContent = addSongWriteServiceModel.IsSongWithExplicitContent;
            editedSong.HasSample = addSongWriteServiceModel.IsSongWithSample;
            editedSong.HasLegalClearance = addSongWriteServiceModel.IsLeagalClearanceObtained;
            editedSong.IsConfirmed = addSongWriteServiceModel.IsAllConfirmed;

            await SongGenreRepository.DeleteAsync((new SongGenreSpecification()
                .BySongId(editedSong.Id))
                .ByType(SongGenreType.Primary));


            if (addSongWriteServiceModel.PrimaryGenres.Count > 0)
            {
                
                var song = await GetSongGenres(addSongWriteServiceModel.PrimaryGenres);
                var genreEntities = song.Value;
                var songGenres = new List<SongGenreEntity>();

                genreEntities.ForEach(s => songGenres.Add(new SongGenreEntity()
                {
                    Type = SongGenreType.Primary,
                    Song = editedSong,
                    SongId = editedSong.Id,
                    GenreId = s.Id,
                }));

                await SongGenreRepository.AddRangeAsync(songGenres);

            }

            await SongGenreRepository.DeleteAsync((new SongGenreSpecification()
                .BySongId(editedSong.Id))
                .ByType(SongGenreType.Secondary));

            if (addSongWriteServiceModel.SecondaryGenres.Count > 0)
            {

                var song = await GetSongGenres(addSongWriteServiceModel.SecondaryGenres);
                var genreEntities = song.Value;
                var songGenres = new List<SongGenreEntity>();

                genreEntities.ForEach(s => songGenres.Add(new SongGenreEntity()
                {
                    Type = SongGenreType.Secondary,
                    Song = editedSong,
                    SongId = editedSong.Id,
                    GenreId = s.Id,
                }));

                await SongGenreRepository.AddRangeAsync(songGenres);
            }
           

            await AddSongSocialMedia(editedSong, addSongWriteServiceModel, true);

            await AddSongDSPLinks(editedSong, addSongWriteServiceModel, true);

            await SongRepository.UpdateAndSaveAsync(editedSong);

            return Result.Success(editedSong);
        }

        #endregion
    }
}
