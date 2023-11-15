#region Usings

using System;
using UltimatePlaylist.Common.Enums;

#endregion

namespace UltimatePlaylist.Services.Common.Models.Ticket
{
    public class AddTicketForPlaylistActionServiceModel
    {
        public Guid UserExternalId { get; set; }

        public Guid SongExternalId { get; set; }

        public Guid PlaylistExternalId { get; set; }

        public TicketEarnedType EarnedType { get; set; }

        public TicketType TicketType { get; set; }

        public int TicketQty { get; set; }
    }
}
