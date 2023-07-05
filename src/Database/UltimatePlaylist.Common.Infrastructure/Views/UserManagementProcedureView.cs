namespace UltimatePlaylist.Database.Infrastructure.Views
{
    public class UserManagementProcedureView
    {
        public long Id { get; set; }

        public Guid ExternalId { get; set; }

        public string UserName { get; set; }

        public string Name { get; set; }

        public string LastName { get; set; }

        public DateTime? LastActive { get; set; }

        public string ImageUrl { get; set; }

        public DateTime BirthDate { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        public double TotalMinutesListened { get; set; }

        public double AvarageDailyPlays { get; set; }

        public double AvarageTimeListened { get; set; }

        public int JackpotWon { get; set; }

        public int PrizesWon { get; set; }

        public string ZipCode { get; set; }
    }
}
