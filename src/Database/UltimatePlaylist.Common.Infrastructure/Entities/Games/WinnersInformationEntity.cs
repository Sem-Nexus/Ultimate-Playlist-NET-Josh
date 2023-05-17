using UltimatePlaylist.Database.Infrastructure.Entities.Base;

namespace UltimatePlaylist.Services.Common.Models.Games
{
    public class WinnersInformationEntity : BaseEntity
    {
        public decimal PrizeTier { get; set; }

        public long PlayerId { get; set; }

        public string UserName { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        public string ZipCode { get; set; } 

        public string PhoneNumber { get; set; }

        public string Gender { get; set; }

        public DateTime BirthDate { get; set; }

        public long GameId { get; set; }

        public int WinsCount { get; set; }

        public decimal TotalWinsAmount { get; set; }


    }
}
