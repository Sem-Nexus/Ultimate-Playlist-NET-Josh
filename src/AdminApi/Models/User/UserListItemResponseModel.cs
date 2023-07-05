namespace UltimatePlaylist.AdminApi.Models.User
{
    public class UserListItemResponseModel
    {
        public string ExternalId { get; set; }

        public string UserName { get; set; }

        public string Name { get; set; }

        public string LastName { get; set; }

        public DateTime? LastActive { get; set; }

        public string ImageUrl { get; set; }

        public DateTime BirthDate { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        public TimeSpan TotalMinutesListened { get; set; } = new TimeSpan(hours: 1, minutes: 10, seconds: 20);

        public int AvarageDailyPlays { get; set; } = 10;

        public TimeSpan AvarageTimeListened { get; set; } = new TimeSpan(hours: 1, minutes: 30, seconds: 20);

        public int JackpotWon { get; set; }

        public int PrizesWon { get; set; }

        public string ZipCode { get; set; }
    }
}
