namespace UltimatePlaylist.Services.Common.Models.Identity
{
    public class ExternalAuthenticationReadServiceModel
    {
        public string Token { get; set; }
        public string Provider { get; set; }
        public string Device { get; set; }
    }
}