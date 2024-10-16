namespace BrodClientAPI.Models
{
    public class AddReviewToJobPostAd
    {
        public string _id { get; set; }
        public string ServiceID { get; set; }
        public string ClientID { get; set; }
        public int StarRating { get; set; }
        public string ReviewDescription { get; set; }
    }
}
