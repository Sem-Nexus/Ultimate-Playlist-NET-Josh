namespace UltimatePlaylist.MobileApi.Models.Identity
{
    public class RefreshTokenRequestModel
    {
        public string RefreshToken { get; set; }

        public string Device { get; set; }
    }
}