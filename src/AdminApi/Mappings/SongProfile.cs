#region Usings

using AutoMapper;
using UltimatePlaylist.AdminApi.Models.Song;
using UltimatePlaylist.Services.Common.Models.Song;

#endregion

namespace UltimatePlaylist.AdminApi.Mappings
{
    public class SongProfile : Profile
    {
        #region Constructor(s)

        public SongProfile()
        {
            // Request model => Write service model
            CreateMap<AddSongRequestModel, AddSongWriteServiceModel>();

            CreateMap<RemoveSongRequestModel, RemoveSongWriteServiceModel>();
            CreateMap<SongsAnalyticsFilterRequestModel, SongsAnalyticsFilterServiceModel>();
            CreateMap<AgeRequestModel, AgeServiceModel>();

            // Read service model => Response model
            CreateMap<GenreReadServiceModel, GenreResponseModel>();

            CreateMap<SongReadServiceModel, SongResponseModel>();
            CreateMap<GeneralSongDataListItemReadServiceModel, GeneralSongDataListItemResponseModel>();

            CreateMap<GeneralSongDataListItemReadServiceModel, SongResponseModel>()
                .ForMember(c => c.TotalSongPlays, opt => opt.MapFrom(i => i.UniquePlays))
                .ForMember(c => c.PrimaryGenres, opt => opt.MapFrom(i => i.Genre))
                .ForMember(c => c.SecondaryGenres, opt => opt.MapFrom(i => i.GenreSecondary))
                .ForMember(c => c.TotalTimeListened, opt => opt.MapFrom(i => i.TotalTimeListened))
                .ForMember(c => c.TotalAddedToDSP, opt => opt.MapFrom(i => i.NumberOfTimesAddedToDSP));

            CreateMap<GeneralMusicDataListItemReadServiceModel, GeneralMusicDataListItemReadServiceModel>();
            CreateMap<GeneralMusicDataListItemReadServiceModel, MusicResponseModel>()
                .ForMember(c => c.PrimaryGenres, opt => opt.MapFrom(i => i.Genre))
                .ForMember(c => c.SecondaryGenres, opt => opt.MapFrom(i => i.GenreSecondary))
                .ForMember(c => c.TotalAddedToDSP, opt => opt.MapFrom(i => i.NumberOfTimesAddedToDSP));

            CreateMap<SongsAnalyticsFileServiceReadModel, SongsAnalyticsFileServiceResponseModel>();
        }

        #endregion
    }
}
