namespace BrodClientAPI.Models
{
    public class JobPostWithRatings
    {
        public Services service { get; set; }
        public List<Rating> ratings { get; set; }
        public int TotalRating { get; set; }
    }
}
