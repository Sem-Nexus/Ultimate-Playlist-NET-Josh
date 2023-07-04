namespace UltimatePlaylist.Services.Common.Models.Song
{
    public class EngagementFilterServiceModel
    {
        public EngagementFilterServiceModel()
        {
            Date = 0;
            Genders = new List<string>();
            ZipCode = "";
        }

        public int? Date { get; set; }

        public List<string> Genders { get; set; }

        public string ZipCode { get; set; }

    }
}
