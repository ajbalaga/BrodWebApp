namespace BrodClientAPI.Models
{
    public class TradieProfile
    {
        public User user { get; set; }
        public List<Rating> ratings { get; set; }
        public int TotalRating { get; set; }
    }
}
