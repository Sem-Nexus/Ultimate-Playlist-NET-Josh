#region Usings

using AutoMapper;
using UltimatePlaylist.Database.Infrastructure.Views;
using UltimatePlaylist.Services.Common.Models.Leaderboard;

#endregion

namespace UltimatePlaylist.Services.Leaderboard.Mappings
{
    public class LeaderboardProfile : Profile
    {
        #region Constructor(s)

        public LeaderboardProfile()
        {
            // Entity View model => Read service model
            CreateMap<LeaderboardRankingBySongCountView, LeaderboardUserScoresReadServiceModel>()
                .ForMember(s => s.SongRankingPosition, o => o.MapFrom(t => t.RankingPosition))
                .ForMember(s => s.TicketCount, o => o.Ignore())
                .ForMember(s => s.TicketRankingPosition, o => o.Ignore());

            CreateMap<LeaderboardRankingBySongCountView, LeaderboardOtherUserScoresReadServiceModel>()
                .ForMember(s => s.Amount, o => o.MapFrom(t => t.SongCount));

            CreateMap<LeaderboardRankingByTicketCountView, LeaderboardOtherUserScoresReadServiceModel>()
                .ForMember(s => s.Amount, o => o.MapFrom(t => t.TicketCount));

            CreateMap<LeaderboardRankingTicket, LeaderboardOtherUserScoresReadServiceModel>()
            .ForMember(s => s.Amount, o => o.MapFrom(t => t.TicketCount));

            // Read Service Model => Read Service Model
            CreateMap<LeaderboardUserScoresReadServiceModel, LeaderboardReadServiceModel>()
                .ForMember(s => s.UserStats, o => o.MapFrom(t => t))
                .ForMember(s => s.OtherUsersSongListeningRanking, o => o.Ignore())
                .ForMember(s => s.OtherUsersTicketEarningRanking, o => o.Ignore());
        }

        #endregion
    }
}
