#region Usings

using UltimatePlaylist.Database.Infrastructure.Entities.Base;

#endregion

namespace UltimatePlaylist.Database.Infrastructure.Views
{
    public class DailyUsersView
    {
        public long Id { get; set; }
        
        public string Email { get; set; }

        public string UserName { get; set; }

        public string Name { get; set; }

        public string LastName { get; set; }

        public string Gender { get; set; }

        public string PhoneNumber { get; set; }

        public string ZipCode { get; set; }

        public bool EmailConfirmed { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? LastActive { get; set; }

        public DateTime? Updated { get; set; }

        public DateTime? Created { get; set; }
        public string BirthDate { get; set; }

        public string Device { get; set; }
    }
}
