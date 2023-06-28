#region Usings

using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using UltimatePlaylist.Common.Enums;
using UltimatePlaylist.Common.Extensions;
using UltimatePlaylist.Database.Infrastructure.Entities.Song;
using UltimatePlaylist.Database.Infrastructure.Views;
using UltimatePlaylist.Services.Common.Models.Song;

#endregion

namespace UltimatePlaylist.Services.Song.Mapping
{
    public class SongProfile : Profile
    {
        #region Constructor(s)

        public SongProfile()
        {
            // Write service model => Entity model
            CreateMap<AddSongWriteServiceModel, SongEntity>()
                .ForMember(s => s.FirstReleaseDate, o => o.MapFrom(t => t.FirstPublicReleaseDate))
                .ForMember(s => s.IsArtWorkOriginal, o => o.MapFrom(t => t.IsAllArtworkOriginal))
                .ForMember(s => s.IsAudioOriginal, o => o.MapFrom(t => t.IsAllAudioOriginal))
                .ForMember(s => s.IsConfirmed, o => o.MapFrom(t => t.IsAllConfirmed))
                .ForMember(s => s.HasExplicitContent, o => o.MapFrom(t => t.IsSongWithExplicitContent))
                .ForMember(s => s.HasLegalClearance, o => o.MapFrom(t => t.IsLeagalClearanceObtained))
                .ForMember(s => s.HasSample, o => o.MapFrom(t => t.IsSongWithSample));

            // Entity model => Read service model
            CreateMap<GenreEntity, GenreReadServiceModel>();

            CreateMap<SongEntity, SongReadServiceModel>()
                .ForMember(s => s.PrimaryGenres, o => o.MapFrom(t => GetPrimaryGenres(t.SongGenres)))
                .ForMember(s => s.CoverUrl, o => o.MapFrom(t => t.CoverFile.Url));

            CreateMap<GeneralSongDataProcedureView, GeneralSongDataListItemReadServiceModel>()
                .AfterMap((src, dst) =>
                {
                    dst.AverageRating = dst.AverageRating.HasValue ? dst.AverageRating.Value.ToRound(2) : dst.AverageRating;
                });

            CreateMap<GeneralMusicDataProcedureView, GeneralMusicDataListItemReadServiceModel>();
            CreateMap<GeneralSongsAnalyticsFileInformationView, SongsAnalyticsFileServiceReadModel>();
        }

        #endregion

        #region Private Method(s)

        private string GetPrimaryGenres(ICollection<SongGenreEntity> songGenres)
        {
            var primaryOnly = songGenres.Where(s => s.Type == SongGenreType.Primary).Select(s => s.Genre.Name);

            return string.Join(", ", primaryOnly);
        }

        #endregion
    }
}
