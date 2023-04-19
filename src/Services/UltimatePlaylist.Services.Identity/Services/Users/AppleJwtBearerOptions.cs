namespace UltimatePlaylist.Services.Identity.Services.Users
{
    internal class AppleJwtBearerOptions
    {
        public string Audience { get; set; }
        public string Issuer { get; set; }
        public bool ValidateIssuerSigningKey { get; set; }
        public bool ValidateIssuer { get; set; }
        public bool ValidateAudience { get; set; }
        public bool ValidateLifetime { get; set; }
    }
}