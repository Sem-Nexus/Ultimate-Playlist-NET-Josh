#region Usings

using CSharpFunctionalExtensions;
using Microsoft.Extensions.Options;
using System.Transactions;
using UltimatePlaylist.Common.Config;
using UltimatePlaylist.Common.Const;
using UltimatePlaylist.Common.Enums;
using UltimatePlaylist.Database.Infrastructure.Entities.Identity;
using UltimatePlaylist.Database.Infrastructure.Entities.Identity.Specifications;
using UltimatePlaylist.Database.Infrastructure.Entities.Playlist;
using UltimatePlaylist.Database.Infrastructure.Entities.Playlist.Specifications;
using UltimatePlaylist.Database.Infrastructure.Entities.Song;
using UltimatePlaylist.Database.Infrastructure.Entities.Song.Specifications;
using UltimatePlaylist.Database.Infrastructure.Entities.Ticket;
using UltimatePlaylist.Database.Infrastructure.Entities.Ticket.Specifications;
using UltimatePlaylist.Database.Infrastructure.Entities.UserSongHistory;
using UltimatePlaylist.Database.Infrastructure.Entities.UserSongHistory.Specifications;
using UltimatePlaylist.Database.Infrastructure.Repositories.Interfaces;
using UltimatePlaylist.Services.Common.Interfaces.Ticket;
using UltimatePlaylist.Services.Common.Models.Ticket;

#endregion

namespace UltimatePlaylist.Services.Ticket
{
    public class TicketService : ITicketService
    {
        #region Private members

        private readonly Lazy<IRepository<TicketEntity>> TicketRepositoryProvider;

        private readonly Lazy<IReadOnlyRepository<User>> UserRepositoryProvider;

        private readonly Lazy<IRepository<UserSongHistoryEntity>> UserSongHistoryRepositoryProvider;

        private readonly Lazy<IReadOnlyRepository<SongEntity>> SongRepositoryProvider;

        private readonly Lazy<IReadOnlyRepository<UserPlaylistEntity>> UserPlaylistRepositoryProvider;

        private readonly Lazy<ITicketStatsService> TicketStatsServiceProvider;

        private readonly Lazy<ITicketProcedureRepository> TicketProcedureRepositoryProvider;

        private readonly TicketConfig TicketConfig;

        #endregion

        #region Constructor(s)

        public TicketService(
            Lazy<IRepository<TicketEntity>> ticketRepositoryProvider,
            Lazy<IReadOnlyRepository<User>> userRepositoryProvider,
            Lazy<IReadOnlyRepository<SongEntity>> songRepositoryProvider,
            Lazy<IRepository<UserSongHistoryEntity>> userSongHistoryRepositoryProvider,
            Lazy<IReadOnlyRepository<UserPlaylistEntity>> userPlaylistRepositoryProvider,
            Lazy<ITicketStatsService> ticketStatsServiceProvider,
            Lazy<ITicketProcedureRepository> ticketProcedureRepositoryProvider,
            IOptions<TicketConfig> ticketConfigOptions)
        {
            TicketRepositoryProvider = ticketRepositoryProvider;
            UserRepositoryProvider = userRepositoryProvider;
            SongRepositoryProvider = songRepositoryProvider;
            UserSongHistoryRepositoryProvider = userSongHistoryRepositoryProvider;
            UserPlaylistRepositoryProvider = userPlaylistRepositoryProvider;
            TicketStatsServiceProvider = ticketStatsServiceProvider;
            TicketConfig = ticketConfigOptions.Value;
            TicketProcedureRepositoryProvider = ticketProcedureRepositoryProvider;
        }

        #endregion

        #region Properties

        private IRepository<TicketEntity> TicketRepository => TicketRepositoryProvider.Value;

        private IReadOnlyRepository<User> UserRepository => UserRepositoryProvider.Value;

        private IReadOnlyRepository<SongEntity> SongRepository => SongRepositoryProvider.Value;

        private IRepository<UserSongHistoryEntity> UserSongHistoryRepository => UserSongHistoryRepositoryProvider.Value;

        private IReadOnlyRepository<UserPlaylistEntity> UserPlaylistRepository => UserPlaylistRepositoryProvider.Value;

        private ITicketStatsService TicketStatsService => TicketStatsServiceProvider.Value;

        private ITicketProcedureRepository TicketProcedureRepository => TicketProcedureRepositoryProvider.Value;

        #endregion

        #region Public Method(s)

        public async Task<Result<EarnedTicketsReadServiceModel>> AddUserTicketAsync(Guid userExternalId, AddTicketWriteServiceModel addTicketWriteServiceModel)
        {
            return await GetUserAsync(userExternalId)
                .Bind(async user => await AddUserTicketAsync(user, addTicketWriteServiceModel));
        }
        //new2022-10-14-from
        public async Task<Result<EarnedTicketsReadServiceModel>> UpdateUserTicketAsync(Guid userExternalId, UpdateTicketWriteServiceModel updateTicketWriteServiceModel)
        {
            return await GetUserAsync(userExternalId)
                .Bind(async user => await UpdateUserTicketAsync(user, updateTicketWriteServiceModel));
        }

        private async Task<Result<EarnedTicketsReadServiceModel>> UpdateUserTicketAsync(User user, UpdateTicketWriteServiceModel updateTicketWriteServiceModel)
        {
            return await UpdateUserTicketForPlaylistActionAsync(user, updateTicketWriteServiceModel);
        }

        private async Task<Result<EarnedTicketsReadServiceModel>> UpdateUserTicketForPlaylistActionAsync(
            User user,
            UpdateTicketWriteServiceModel updateTicketWriteServiceModel)
        {
            return await GetPlaylist(updateTicketWriteServiceModel.PlaylistExternalId)
                .Bind(playlist => GetPlaylistSong(playlist, updateTicketWriteServiceModel.ExternalId))
                .Tap(async userPlaylistSong => await GetOrAddUserSongHistoryAsync(user, userPlaylistSong.Song))
                .Map(async userPlaylistSong => await UpdatePlaylistSpecificTicketAsync(
                    user.ExternalId,
                    userPlaylistSong,
                    updateTicketWriteServiceModel.Type,
                    updateTicketWriteServiceModel.EarnedType,
                    updateTicketWriteServiceModel.IsErrorTriggered));
        }

        private async Task<EarnedTicketsReadServiceModel> UpdatePlaylistSpecificTicketAsync(
            Guid userExternalId,
            UserPlaylistSongEntity playlistSongEntity,
            TicketType ticketType,
            TicketEarnedType ticketEarnedType,
            int isErrorTriggered)
        {
            // do something with EF here
            var ticketsExist = await TicketRepository.AnyAsync(new TicketSpecification()
            .ByType(ticketType)
            .ByEarnedType(ticketEarnedType)
            .ByPlaylistSongExternalId(playlistSongEntity.ExternalId));

            if (!ticketsExist)
            {
                var ticketList = new List<TicketEntity>();
                var ticketsAmount = EarnedTicketsAmountByEarnedType(ticketEarnedType);

                for (int i = 0; i < ticketsAmount; i++)
                {
                    await TicketRepository.AddAsync(new TicketEntity()
                    {
                        Type = ticketType,
                        EarnedType = ticketEarnedType,
                        IsUsed = false,
                        UserPlaylistSong = playlistSongEntity,
                        IsErrorTriggered = isErrorTriggered
                    });
                    Thread.Sleep(50);
                }
            }

            var ticketsToBadge = await GetTickets(userExternalId);

            return new EarnedTicketsReadServiceModel()
            {
                LatestEarnedTickets = ticketsToBadge,
            };

        }

        //new2022-10-14-to
        public async Task<int> GetThirtySecondsTickets(Guid userExternalId)
        {
            var counts = await TicketRepository.CountAsync(new TicketSpecification()
                .WithUser()
                .ByUserExternalIdUsingPlaylistRelation(userExternalId)
                .ByTodaysTickets()
                .ByType(TicketType.Daily)
                .ByEarnedType(TicketEarnedType.ThirtySecondsOfListenedSong)
                .OnlyNotUsed());
            return counts;
        }

        public async Task<int> GetThirtySecondsHistoryTickets(Guid userExternalId)
        {
            var counts = await TicketRepository.CountAsync(new TicketSpecification()
                .WithUser()
                .ByUserSongHistoryExternalId(userExternalId)
                .ByTodaysTickets()
                .ByType(TicketType.Daily)
                .ByEarnedType(TicketEarnedType.ThirtySecondsOfListenedSong)
                .OnlyNotUsed());
            return counts;
        }

        public async Task<Result<bool>> FastAddUserTicketForPlaylistActionAsync(Guid userExternalId, AddTicketWriteServiceModel addTicketWriteServiceModel)
        {
            await TicketProcedureRepository.AddTicketForPlaylistActionAsync(
                new AddTicketForPlaylistActionServiceModel()
                {
                    UserExternalId = userExternalId,
                    PlaylistExternalId = addTicketWriteServiceModel.PlaylistExternalId,
                    SongExternalId = addTicketWriteServiceModel.ExternalId,
                    EarnedType = addTicketWriteServiceModel.EarnedType,
                    TicketType = addTicketWriteServiceModel.Type,
                    TicketQty = EarnedTicketsAmountByEarnedType(addTicketWriteServiceModel.EarnedType)
                });
            return Result.Success(true);
        }

        #endregion

        #region Private Methods

        private async Task<Result<User>> GetUserAsync(Guid userExternalId)
        {
            var user = await UserRepository.FirstOrDefaultAsync(new UserSpecification()
                .ByExternalId(userExternalId));

            return Result.SuccessIf(user != null, user, ErrorMessages.UserDoesNotExist);
        }

        private async Task<Result<SongEntity>> GetSongAsync(Guid songExternalId)
        {
            var song = await SongRepository.FirstOrDefaultAsync(new SongSpecification()
                .ByExternalId(songExternalId));

            return Result.SuccessIf(song != null, song, ErrorMessages.SongDoesNotExist);
        }

        private async Task<Result<EarnedTicketsReadServiceModel>> AddUserTicketAsync(User user, AddTicketWriteServiceModel addTicketWriteServiceModel)
        {
            if (addTicketWriteServiceModel.EarnedType == TicketEarnedType.AddedToSpotify || addTicketWriteServiceModel.EarnedType == TicketEarnedType.AddedToAppleMusic)
            {
                return await AddUserTicketForSongActionAsync(user, addTicketWriteServiceModel);
            }
            else if (addTicketWriteServiceModel.Type == TicketType.Jackpot)
            {
                return await AddTicketForUltimatePayout(user, addTicketWriteServiceModel);
            }
            else
            {
                return await AddUserTicketForPlaylistActionAsync(user, addTicketWriteServiceModel);
            }
        }

        private async Task<Result<EarnedTicketsReadServiceModel>> AddUserTicketForPlaylistActionAsync(
            User user,
            AddTicketWriteServiceModel addTicketWriteServiceModel)
        {
            return await GetPlaylist(addTicketWriteServiceModel.PlaylistExternalId)
                .Bind(playlist => GetPlaylistSong(playlist, addTicketWriteServiceModel.ExternalId))
                .Tap(async userPlaylistSong => await GetOrAddUserSongHistoryAsync(user, userPlaylistSong.Song))
                .Map(async userPlaylistSong => await AddPlaylistSpecificTicketAsync(
                    user.ExternalId,
                    userPlaylistSong,
                    addTicketWriteServiceModel.Type,
                    addTicketWriteServiceModel.EarnedType));
        }

        private async Task<Result<EarnedTicketsReadServiceModel>> AddUserTicketForSongActionAsync(
            User user,
            AddTicketWriteServiceModel addTicketWriteServiceModel)
        {
            /* return await GetSongAsync(addTicketWriteServiceModel.ExternalId)
                 .Map(async song => await GetOrAddUserSongHistoryAsync(user, song))
                 .Map(async songHistory => await AddSongSpecificTicketAsync(
                     user.ExternalId,
                     songHistory,
                     addTicketWriteServiceModel.Type,
                     addTicketWriteServiceModel.EarnedType));*/
            return await GetPlaylist(addTicketWriteServiceModel.PlaylistExternalId)
                 .Bind(playlist => GetPlaylistSong(playlist, addTicketWriteServiceModel.ExternalId))
                 .Tap(async userPlaylistSong => await GetOrAddUserSongHistoryAsync(user, userPlaylistSong.Song))
                 .Map(async userPlaylistSong => await AddPlaylistSpecificTicketAsync(
                     user.ExternalId,
                     userPlaylistSong,
                     addTicketWriteServiceModel.Type,
                     addTicketWriteServiceModel.EarnedType));
        }

        private async Task<Result<EarnedTicketsReadServiceModel>> AddTicketForUltimatePayout(
            User user,
            AddTicketWriteServiceModel addTicketWriteServiceModel)
        {
            return await GetPlaylist(addTicketWriteServiceModel.PlaylistExternalId)
                .Bind(playlist => GetPlaylistSong(playlist, addTicketWriteServiceModel.ExternalId))
                .Tap(async userPlaylistSong => await GetOrAddUserSongHistoryAsync(user, userPlaylistSong.Song))
                .Map(async userPlaylistSong => await AddUltimateTicketAsync(
                    userPlaylistSong,
                    addTicketWriteServiceModel.EarnedType));
        }

        private async Task<UserSongHistoryEntity> GetOrAddUserSongHistoryAsync(User user, SongEntity songEntity)
        {
            var userSongHistory = await UserSongHistoryRepository.FirstOrDefaultAsync(new UserSongHistorySpecification()
               .BySongExternalId(songEntity.ExternalId)
               .ByUserExternalId(user.ExternalId)
               .OrderByCreatedDescending());

            if (userSongHistory is null)
            {
                userSongHistory = await UserSongHistoryRepository.AddAsync(new UserSongHistoryEntity()
                {
                    SongId = songEntity.Id,
                    UserId = user.Id,
                });
            }

            return userSongHistory;
        }

        private async Task<EarnedTicketsReadServiceModel> AddSongSpecificTicketAsync(
            Guid userExternalId,
            UserSongHistoryEntity userSongHistoryEntity,
            TicketType ticketType,
            TicketEarnedType ticketEarnedType)
        {
            Thread.Sleep(50);
            var ticketsExist = await TicketRepository.AnyAsync(new TicketSpecification()
            .ByType(ticketType)
            .ByEarnedType(ticketEarnedType)
            .ByUserSongHistoryExternalId(userSongHistoryEntity.ExternalId));

            if (!ticketsExist)
            {
                var ticketList = new List<TicketEntity>();
                var ticketsAmount = EarnedTicketsAmountByEarnedType(ticketEarnedType);

                for (int i = 0; i < ticketsAmount; i++)
                {
                    await TicketRepository.AddAsync(new TicketEntity()
                    {
                        Type = ticketType,
                        EarnedType = ticketEarnedType,
                        IsUsed = false,
                        UserSongHistory = userSongHistoryEntity,
                    });
                    Thread.Sleep(50);
                    /*ticketList.Add(new TicketEntity()
                    {
                        Type = ticketType,
                        EarnedType = ticketEarnedType,
                        IsUsed = false,
                        UserSongHistory = userSongHistoryEntity,
                    });*/
                }

                //await TicketRepository.AddRangeAsync(ticketList);
            }

            var ticketsToBadge = await GetTickets(userExternalId);

            return new EarnedTicketsReadServiceModel()
            {
                LatestEarnedTickets = ticketsToBadge,
            };
        }

        private async Task<EarnedTicketsReadServiceModel> AddPlaylistSpecificTicketAsync(
            Guid userExternalId,
            UserPlaylistSongEntity playlistSongEntity,
            TicketType ticketType,
            TicketEarnedType ticketEarnedType)
        {
            // do something with EF here
            var ticketsExist = await TicketRepository.AnyAsync(new TicketSpecification()
            .ByType(ticketType)
            .ByEarnedType(ticketEarnedType)
            .ByPlaylistSongExternalId(playlistSongEntity.ExternalId));

            if (!ticketsExist)
            {
                var ticketList = new List<TicketEntity>();
                var ticketsAmount = EarnedTicketsAmountByEarnedType(ticketEarnedType);

                for (int i = 0; i < ticketsAmount; i++)
                {
                    await TicketRepository.AddAsync(new TicketEntity()
                    {
                        Type = ticketType,
                        EarnedType = ticketEarnedType,
                        IsUsed = false,
                        UserPlaylistSong = playlistSongEntity,
                    });
                    Thread.Sleep(50);
                    /*ticketList.Add(new TicketEntity()
                    {
                        Type = ticketType,
                        EarnedType = ticketEarnedType,
                        IsUsed = false,
                        UserPlaylistSong = playlistSongEntity,
                    });*/
                }

                //await TicketRepository.AddRangeAsync(ticketList);
            }

            var ticketsToBadge = await GetTickets(userExternalId);

            return new EarnedTicketsReadServiceModel()
            {
                LatestEarnedTickets = ticketsToBadge,
            };
            
        }

        private async Task<EarnedTicketsReadServiceModel> AddUltimateTicketAsync(
              UserPlaylistSongEntity playlistSongEntity,
              TicketEarnedType ticketEarnedType)
        {
            Thread.Sleep(50);
            var ticketsExist = await TicketRepository.AnyAsync(new TicketSpecification()
            .ByType(TicketType.Jackpot)
            .ByEarnedType(ticketEarnedType)
            .ByPlaylistSongExternalId(playlistSongEntity.ExternalId));

            if (!ticketsExist)
            {
                var ticketList = new List<TicketEntity>();
                var ticketsAmount = EarnedTicketsAmountByEarnedType(ticketEarnedType);

                for (int i = 0; i < ticketsAmount; i++)
                {
                    await TicketRepository.AddAsync(new TicketEntity()
                    {
                        Type = TicketType.Jackpot,
                        EarnedType = ticketEarnedType,
                        IsUsed = false,
                        UserPlaylistSong = playlistSongEntity,
                    });
                    Thread.Sleep(50);
                    /*ticketList.Add(new TicketEntity()
                    {
                        Type = TicketType.Jackpot,
                        EarnedType = ticketEarnedType,
                        IsUsed = false,
                        UserPlaylistSong = playlistSongEntity,
                    });*/
                }

                //await TicketRepository.AddRangeAsync(ticketList);
            }

            return new EarnedTicketsReadServiceModel()
            {
                LatestEarnedTickets = 0,
            };
        }

        private async Task<int> GetTickets(Guid userExternalId)
        {
            var tickets = await TicketStatsService.UserTicketStatsAsync(userExternalId);
            return tickets.IsSuccess ? tickets.Value.TicketsAmountForTodayDrawing : 0;
        }

        private async Task<Result<UserPlaylistEntity>> GetPlaylist(Guid playlistExternalId)
        {
            var playlist = await UserPlaylistRepository.FirstOrDefaultAsync(new UserPlaylistSpecification()
                .ByExternalId(playlistExternalId)
                .OrderByCreatedDescending()
                .WithSongs());

            return Result.SuccessIf(playlist is not null, playlist, ErrorMessages.PlaylistDoesNotExist);
        }

        private Result<UserPlaylistSongEntity> GetPlaylistSong(
           UserPlaylistEntity userPlaylist,
           Guid songExternalId)
        {
            var userPlaylistSong = userPlaylist.UserPlaylistSongs.FirstOrDefault(s => s.Song.ExternalId == songExternalId);

            return Result.SuccessIf(userPlaylistSong is not null, userPlaylistSong, ErrorMessages.SongDoesNotExist);
        }

        private int EarnedTicketsAmountByEarnedType(TicketEarnedType ticketEarnedType)
            => ticketEarnedType switch
            {
                TicketEarnedType.ThirtySecondsOfListenedSong => TicketConfig.AmountByEarnedType.ThirtySecondsOfListenedSong,
                TicketEarnedType.SixtySecondsOfListenedSong => TicketConfig.AmountByEarnedType.SixtySecondsOfListenedSong,
                TicketEarnedType.EntireSong => TicketConfig.AmountByEarnedType.EntireSong,
                TicketEarnedType.Rating => TicketConfig.AmountByEarnedType.Rating,
                TicketEarnedType.AddedToAppleMusic => TicketConfig.AmountByEarnedType.AddedToAppleMusic,
                TicketEarnedType.AddedToSpotify => TicketConfig.AmountByEarnedType.AddedToSpotify,
                TicketEarnedType.ThreeSongsWithoutSkip => TicketConfig.AmountByEarnedType.ThreeSongsWithoutSkip,
                TicketEarnedType.HalfOfPlaylist => TicketConfig.AmountByEarnedType.HalfPlaylist,
                TicketEarnedType.FullPlaylist => TicketConfig.AmountByEarnedType.FullPlaylist,
                _ => throw new Exception(ErrorType.NotSupportedEarnedTicketType.ToString())
            };

        #endregion
    }
}
