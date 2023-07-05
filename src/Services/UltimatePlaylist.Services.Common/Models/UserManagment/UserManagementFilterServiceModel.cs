namespace UltimatePlaylist.Services.Common.Models.Song
{
    public class UserManagementFilterServiceModel
    {
        public UserManagementFilterServiceModel()
        {
            Genders = new List<string>();
            ZipCode = "";
        }

        public List<string> Genders { get; set; }

        public string ZipCode { get; set; }

    }
}
