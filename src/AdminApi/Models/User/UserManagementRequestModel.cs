#region Usings

using UltimatePlaylist.Common.Enums;

#endregion

namespace UltimatePlaylist.AdminApi.Models.User
{
    public class UserManagementRequestModel
    {
        public List<string> Genders { get; set; }

        public string ZipCode { get; set; }
        
    }
}
