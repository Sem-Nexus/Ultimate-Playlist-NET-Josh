#region Usings

using AutoMapper;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using UltimatePlaylist.Common.Const;
using UltimatePlaylist.Common.Models;
using UltimatePlaylist.Database.Infrastructure.Entities.Identity;
using UltimatePlaylist.Database.Infrastructure.Entities.Identity.Specifications;
using UltimatePlaylist.Database.Infrastructure.Entities.Playlist;
using UltimatePlaylist.Database.Infrastructure.Repositories.Interfaces;
using UltimatePlaylist.Database.Infrastructure.Views;
using UltimatePlaylist.Database.Infrastructure.Views.Specifications;
using UltimatePlaylist.Services.Common.Interfaces.Leaderboard;
using UltimatePlaylist.Services.Common.Interfaces.Ticket;
using UltimatePlaylist.Services.Common.Models.Files;
using UltimatePlaylist.Services.Common.Models.Leaderboard;

#endregion

namespace UltimatePlaylist.Services.Leaderboard
{
    public class LeaderboardService : ILeaderboardService
    {
        #region Private members

        private readonly Lazy<IReadOnlyRepository<User>> UserRepositoryProvider;

        private readonly Lazy<IReadOnlyRepository<UserPlaylistSongEntity>> UserPlaylistSongRepositoryProvider;

        private readonly Lazy<IReadOnlyRepository<LeaderboardRankingBySongCountView>> LeaderboardRankingBySongCountViewRepositoryProvider;

        private readonly Lazy<IReadOnlyRepository<LeaderboardRankingByTicketCountView>> LeaderboardRankingByTicketCountViewRepositoryProvider;

        private readonly Lazy<ITicketProcedureRepository> TicketProcedureRepositoryProvider;

        private readonly Lazy<ILogger<LeaderboardService>> LoggerProvider;

        private readonly Lazy<IMapper> MapperProvider;

        #endregion

        #region Constructor(s)

        public LeaderboardService(
            Lazy<IReadOnlyRepository<User>> userRepositoryProvider,
            Lazy<IReadOnlyRepository<UserPlaylistSongEntity>> userPlaylistSongRepositoryProvider,
            Lazy<IReadOnlyRepository<LeaderboardRankingBySongCountView>> leaderboardRankingBySongCountViewRepositoryProvider,
            Lazy<IReadOnlyRepository<LeaderboardRankingByTicketCountView>> leaderboardRankingByTicketCountViewRepositoryProvider,
            Lazy<ITicketProcedureRepository> ticketProcedureRepositoryProvider,
            Lazy<ILogger<LeaderboardService>> loggerProvider,
            Lazy<IMapper> mapperProvider)
        {
            UserRepositoryProvider = userRepositoryProvider;
            UserPlaylistSongRepositoryProvider = userPlaylistSongRepositoryProvider;
            LeaderboardRankingBySongCountViewRepositoryProvider = leaderboardRankingBySongCountViewRepositoryProvider;
            LeaderboardRankingByTicketCountViewRepositoryProvider = leaderboardRankingByTicketCountViewRepositoryProvider;
            TicketProcedureRepositoryProvider = ticketProcedureRepositoryProvider;
            LoggerProvider = loggerProvider;
            MapperProvider = mapperProvider;
        }

        #endregion

        #region Properties

        private IReadOnlyRepository<User> UserRepository => UserRepositoryProvider.Value;

        private IReadOnlyRepository<UserPlaylistSongEntity> UserPlaylistSongRepository => UserPlaylistSongRepositoryProvider.Value;

        private IReadOnlyRepository<LeaderboardRankingBySongCountView> LeaderboardRankingBySongCountViewRepository => LeaderboardRankingBySongCountViewRepositoryProvider.Value;

        private IReadOnlyRepository<LeaderboardRankingByTicketCountView> LeaderboardRankingByTicketCountViewRepository => LeaderboardRankingByTicketCountViewRepositoryProvider.Value;

        private ITicketProcedureRepository TicketProcedureRepository => TicketProcedureRepositoryProvider.Value;

        private ILogger<LeaderboardService> Logger => LoggerProvider.Value;

        private IMapper Mapper => MapperProvider.Value;

        #endregion

        #region Public Method(s)

        public async Task<Result<LeaderboardReadServiceModel>> GetLeaderboardInfoAsync(Guid userExternalId)
        {
            return await GetUserAsync(userExternalId)
                .Bind(async user => await GetUserScores(user))
                .Bind(async leaderboardServiceModel => await GetLeaderboardRanking(leaderboardServiceModel));
        }

        #endregion

        #region Private Method(s)

        private async Task<Result<User>> GetUserAsync(Guid userExternalId)
        {
            var user = await UserRepository.FirstOrDefaultAsync(new UserSpecification()
                .WithAvatar()
                .ByExternalId(userExternalId));

            return Result.SuccessIf(user != null, user, ErrorMessages.UserDoesNotExist);
        }

        private async Task<Result<LeaderboardReadServiceModel>> GetUserScores(User user)
        {
            var userSongStats = await LeaderboardRankingBySongCountViewRepository.FirstOrDefaultAsync(new LeaderboardRankingBySongCountViewSpecification()
                .ByUserExternalId(user.ExternalId));

            //var userTicketStats = await LeaderboardRankingByTicketCountViewRepository.FirstOrDefaultAsync(new LeaderboardRankingByTicketCountViewSpecification()
            //    .ByUserExternalId(user.ExternalId));

            var ticketCount = await TicketProcedureRepository.LeaderboardRankingTicket(user.ExternalId);
            var userTickets = ticketCount.FirstOrDefault();
            
            return Result.Success()
                .Map(() => Mapper.Map<LeaderboardUserScoresReadServiceModel>(userSongStats))
                .CheckIf(mapped => mapped is null, mapped => Result.Failure<LeaderboardReadServiceModel>(ErrorMessages.UserDoesNotExist))
                .Tap(mapped =>
                {
                    mapped.TicketCount = (int)userTickets.TicketCount;
                    mapped.TicketRankingPosition = userTickets?.RankingPosition ?? 0;
                })
                .Map(mapped => Mapper.Map<LeaderboardReadServiceModel>(mapped));
        }

        private async Task<Result<LeaderboardReadServiceModel>> GetLeaderboardRanking(LeaderboardReadServiceModel leaderboardReadServiceModel)
        {
            var songCountRanking = await LeaderboardRankingBySongCountViewRepository.ListAsync(
                new LeaderboardRankingBySongCountViewSpecification().Paged(new Pagination(50, 1)));

            //var ticketCountRanking = await LeaderboardRankingByTicketCountViewRepository.ListAsync(
            //    new LeaderboardRankingByTicketCountViewSpecification().Paged(new Pagination(50, 1)));

            var ticketCountRanking = await TicketProcedureRepository.LeaderboardRankingTicket(Guid.Empty);

            leaderboardReadServiceModel.OtherUsersSongListeningRanking = Mapper.Map<IList<LeaderboardOtherUserScoresReadServiceModel>>(songCountRanking);

            leaderboardReadServiceModel.OtherUsersTicketEarningRanking = Mapper.Map<IList<LeaderboardOtherUserScoresReadServiceModel>>(ticketCountRanking);

            return leaderboardReadServiceModel;
        }
        #endregion
    }
}