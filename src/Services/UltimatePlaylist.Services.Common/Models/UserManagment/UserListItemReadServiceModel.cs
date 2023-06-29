#region Usings

using UltimatePlaylist.Common.Enums;

#endregion

namespace UltimatePlaylist.Services.Common.Models.UserManagment
{
    public class UserListItemReadServiceModel
    {
        public string ExternalId { get; set; }

        public string Name { get; set; }

        public string LastName { get; set; }

        public string UserName { get; set; }

        public DateTime? LastActive { get; set; }

        public string ImageUrl { get; set; }

        public DateTime BirthDate { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        public double TotalMinutesListened { get; set; }

        public double AvarageDailyPlays { get; set; }

        public double AvarageTimeListened { get; set; }
    }
}
