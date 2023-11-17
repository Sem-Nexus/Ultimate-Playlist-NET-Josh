#region Usings

using AutoMapper;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using UltimatePlaylist.Database.Infrastructure.Entities.Ticket;
using UltimatePlaylist.Database.Infrastructure.Entities.Ticket.Specifications;
using UltimatePlaylist.Database.Infrastructure.Repositories.Interfaces;
using UltimatePlaylist.Database.Infrastructure.Views;
using UltimatePlaylist.Games.Models.Raffle;
using UltimatePlaylist.Services.Common.Interfaces.Ticket;

#endregion

namespace UltimatePlaylist.Services.Ticket
{
    public class DailyCashTicketsService : IDailyCashTicketsService
    {
        #region Private members

        private readonly Lazy<IRepository<TicketEntity>> TicketRepositoryProvider;

        private readonly Lazy<IMapper> MapperProvider;

        private readonly Lazy<ITicketProcedureRepository> TicketProcedureRepositoryProvider;
        Lazy<ILogger<DailyCashTicketsService>> LoggerProvider;
        #endregion

        #region Constructor(s)

        public DailyCashTicketsService(
            Lazy<IRepository<TicketEntity>> ticketRepositoryProvider,
            Lazy<IMapper> mapperProvider,
            Lazy<ILogger<DailyCashTicketsService>> loggerProvider,
            Lazy<ITicketProcedureRepository>ticketProcedureRepositoryProvider
            )
        {
            TicketRepositoryProvider = ticketRepositoryProvider;
            MapperProvider = mapperProvider;
            LoggerProvider = loggerProvider;
            TicketProcedureRepositoryProvider = ticketProcedureRepositoryProvider;
        }

        #endregion

        #region Properties

        private ILogger<DailyCashTicketsService> Logger => LoggerProvider.Value;

        private IRepository<TicketEntity> TicketRepository => TicketRepositoryProvider.Value;

        private IMapper Mapper => MapperProvider.Value;

        private ITicketProcedureRepository TicketProcedureRepository => TicketProcedureRepositoryProvider.Value;

        #endregion

        #region Public methods

        public async Task<Result<List<RaffleUserTicketReadServiceModel>>> GetTicketsForDailyCashAsync()
        {
            var tickets = new List<RaffleUserTicketReadServiceModel>();

            return await Result.Success()
                //.Tap(() => Logger.LogInformation("\n\n\n GetTicketsForDailyCashAsync start  ---------------------------------------- \n\n\n"))
                .Map(async () => await TicketRepository.ListAsync(new TicketSpecification()
                     .ByType(UltimatePlaylist.Common.Enums.TicketType.Daily)
                     .ByEarnedForPlaylistType()
                     .WithUserByPlaylist()
                     .OnlyNotUsed()))
                .Tap(ticketsForPlaylist => tickets.AddRange(Mapper.Map<List<RaffleUserTicketReadServiceModel>>(ticketsForPlaylist)))
                .Map(async ticketsForPlaylist => await TicketRepository.ListAsync(
                     new TicketSpecification()
                         .ByType(UltimatePlaylist.Common.Enums.TicketType.Daily)
                         .ByEarnedForSongType()
                         .WithUser()
                         .OnlyNotUsed()))
                .Tap(ticketsForSongs => tickets.AddRange(Mapper.Map<List<RaffleUserTicketReadServiceModel>>(ticketsForSongs)))
                //.Tap(() => Logger.LogInformation($"\n\n\n GetTicketsForDailyCashAsync end {tickets.Count.ToString()}  ---------------------------------------- \n\n\n"))
                .Map(_ => tickets);
        }

        public async Task<Result<List<RaffleUserTicketReadServiceModel>>> GetTicketsForDailyCashAsyncSP()
        {
            return await Result.Success()
                .Tap(() => Logger.LogInformation("GetTicketsForDailyCashAsyncSP start  ---------------------------------------- \n\n\n"))
                .Map(async () => await TicketProcedureRepository.GetDailyCashTickets())
                .Map(ticketsView => Mapper.Map<List<RaffleUserTicketReadServiceModel>>(ticketsView))
                .Tap((tickets) => Logger.LogInformation($"GetTicketsForDailyCashAsyncSP end  {tickets.Count.ToString()}  ---------------------------------------- \n\n\n"));
        }

        public async Task UseTickets(IEnumerable<Guid> ticketsExternalIds)
        {
            var tickets = await TicketRepository.ListAsync(new TicketSpecification()
                .ByExternalIds(ticketsExternalIds.ToArray()));
            tickets.ToList().ForEach(ticket => ticket.IsUsed = true);
            await TicketRepository.UpdateAndSaveRangeAsync(tickets);
        }

        #endregion
    }
}
