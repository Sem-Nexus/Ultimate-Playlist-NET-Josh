#region Usings

using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using UltimatePlaylist.Services.Common.Models.Dsp;
using UltimatePlaylist.Services.Common.Models.Ticket;

#endregion

namespace UltimatePlaylist.Services.Common.Interfaces.Ticket
{
    public interface ITicketService
    {
        Task<Result<EarnedTicketsReadServiceModel>> AddUserTicketAsync(
            Guid userExternalId,
            AddTicketWriteServiceModel addTicketWriteServiceModel);
        //new2022-10-14-from
        Task<Result<EarnedTicketsReadServiceModel>> UpdateUserTicketAsync(
            Guid userExternalId,
            UpdateTicketWriteServiceModel updateTicketWriteServiceModel);
        //new2022-10-14-to
        Task<int> GetThirtySecondsTickets(Guid userExternalId);

        Task<int> GetThirtySecondsHistoryTickets(Guid userExternalId);

        Task<Result<bool>> FastAddUserTicketForPlaylistActionAsync(Guid userExternalId, AddTicketWriteServiceModel addTicketWriteServiceModel);
    }
}
