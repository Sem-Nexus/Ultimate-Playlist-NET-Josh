using UltimatePlaylist.Database.Infrastructure.Entities.Identity;
namespace UltimatePlaylist.Services.Common.Models.Identity
{
    public class AuthenticationReadServiceModel
    {
        public string RefreshToken { get; set; }

        public string Token { get; set; }

        public string? ConcurrencyStamp { get; set; }

        public string Device { get; set; }
    }
}