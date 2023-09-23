namespace UltimatePlaylist.MobileApi.Models.Identity
{
    public class UserLoginRequestModel
    {
        public string Email { get; set; }

        public string Password { get; set; }

        public string Device { get; set; }
    }
}