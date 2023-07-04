namespace UltimatePlaylist.AdminApi.Models.Song
{
    public class EngagementFilterRequestModel
    {
        public List<string> Genders { get; set; }

        public string ZipCode { get; set; }

        public int? Date { get; set; }

    }
}
