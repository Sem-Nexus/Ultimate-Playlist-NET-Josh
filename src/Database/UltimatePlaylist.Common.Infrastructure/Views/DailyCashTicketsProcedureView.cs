using UltimatePlaylist.Database.Infrastructure.Entities.Base;

namespace UltimatePlaylist.Database.Infrastructure.Views
{
    public class DailyCashTicketsProcedureView
    {

        public Int64 Id{ get; set; }

        public Guid UserExternalId { get; set; }

        public Guid TicketExternalId { get; set; }

    }
}