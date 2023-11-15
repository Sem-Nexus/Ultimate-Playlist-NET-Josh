#region Usings

using System;
using System.Globalization;
using System.Linq;
using AutoMapper;
using UltimatePlaylist.Common.Extensions;
using UltimatePlaylist.MobileApi.Mappings.TypeConverters;
using UltimatePlaylist.MobileApi.Models.Playlist;
using UltimatePlaylist.MobileApi.Models.Song;
using UltimatePlaylist.Services.Common.Models.Playlist;
using UltimatePlaylist.Services.Common.Models.Song;

#endregion

namespace UltimatePlaylist.MobileApi.Mappings
{
    public class PlaylistProfile : Profile
    {
        #region Constructor(s)

        public PlaylistProfile()
        {
            // Request model => Write service model
            CreateMap<PlaylistHistoryRequestModel, PlaylistHistoryWriteServiceModel>()
                .ForMember(s => s.SelectedDate, o => o.MapFrom(t => ConvertStringToUnixTimeStamp(t.SelectedDateTimeStamp)));

            // Read service model => Response model
            CreateMap<PlaylistReadServiceModel, PlaylistResponseModel>();
            CreateMap<UserSongReadServiceModel, UserSongResponseModel>()
                .ForMember(s => s.UserRating, o => o.MapFrom(t => t.UserRating.HasValue ? t.UserRating : 0))
                .ForMember(s => s.SecondsListened, o => o.MapFrom(t => t.SecondsListened.HasValue ? t.SecondsListened : 0));

            CreateMap<PlaylistHistoryReadServiceModel, PlaylistHistoryResponseModel>()
                .ForMember(s => s.DateTimeStamp, o => o.MapFrom(t => t.StartDate))
                .ForMember(s => s.Date, o => o.MapFrom(t => PrepareHistoryPlaylistDate(t.StartDate)));

        }

        #endregion

        #region Private Methods

        private string PrepareHistoryPlaylistDate(DateTime startDate)
        {
            return $"{startDate.DayOfWeek.ToString().ToUpper().Substring(0, 3)} {startDate.Day} {startDate.ToString("MMMM", CultureInfo.InvariantCulture)}";
        }

        private DateTime? ConvertStringToUnixTimeStamp(string selectedDate)
        {
            selectedDate.TryFromStringUnixTimestamp(out DateTime? dateTime);
            return dateTime;
        }
        #endregion
    }
}
