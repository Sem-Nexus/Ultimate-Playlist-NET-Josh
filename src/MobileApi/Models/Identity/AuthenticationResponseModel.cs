namespace UltimatePlaylist.MobileApi.Models.Identity
{
    public class AuthenticationResponseModel
    {
        public string RefreshToken { get; set; }

        public string Token { get; set; }

        public string Device { get; set; }
    }
}