#region Usings

using AutoMapper;
using UltimatePlaylist.Database.Infrastructure.Entities.Games;
using UltimatePlaylist.Database.Infrastructure.Entities.Ticket;
using UltimatePlaylist.Database.Infrastructure.Views;
using UltimatePlaylist.Games.Models.Raffle;
using UltimatePlaylist.Services.Common.Models.Reward;

#endregion

namespace UltimatePlaylist.Services.Ticket.Mapping
{
    public class TicketProfile : Profile
    {
        public TicketProfile()
        {
            CreateMap<TicketEntity, RaffleUserTicketReadServiceModel>()
                .ForMember(p => p.UserFriendlyTicketId, opt => opt.MapFrom(m => m.ExternalId.ToString()))
                .ForMember(p => p.UserTicketExternalId, opt => opt.MapFrom(m => m.ExternalId))
                .ForMember(p => p.UserExternalId, opt => opt.Ignore())
                .AfterMap((src, dest) =>
                {
                    if (src.UserSongHistory?.User is not null)
                    {
                        dest.UserExternalId = src.UserSongHistory.User.ExternalId;
                    }
                    else if (src.UserPlaylistSong?.UserPlaylist?.User is not null)
                    {
                        dest.UserExternalId = src.UserPlaylistSong.UserPlaylist.User.ExternalId;
                    }
                });


            CreateMap<DailyCashTicketsProcedureView, RaffleUserTicketReadServiceModel>()
                .ForMember(p => p.UserFriendlyTicketId, opt => opt.MapFrom(m => m.TicketExternalId.ToString()))
                .ForMember(p => p.UserTicketExternalId, opt => opt.MapFrom(m => m.TicketExternalId))
                .ForMember(p => p.UserExternalId, opt => opt.MapFrom(m => m.UserExternalId))
                ;
                

            CreateMap<WinningEntity, CollectedDrawingRewardReadServiceModel>()
                .ForMember(p => p.Name, opt => opt.MapFrom(m => m.Game.Type.ToString()))
                .ForMember(p => p.CollectedDate, opt => opt.MapFrom(m => m.Created))
                .ForMember(p => p.Value, opt => opt.MapFrom(m => m.Amount));

            CreateMap<WinningEntity, ActiveDrawingRewardReadServiceModel>()
                .ForMember(p => p.Name, opt => opt.MapFrom(m => m.Game.Type.ToString()))
                .ForMember(p => p.Value, opt => opt.MapFrom(m => m.Amount));
        }
    }
}
