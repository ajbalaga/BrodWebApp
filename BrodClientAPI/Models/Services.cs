using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace BrodClientAPI.Models
{
    public class Services
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }
        public string UserID { get; set; } //tradie ID
        public string BusinessPostcode { get; set; }
        public string JobCategory { get; set; }
        public string JobAdTitle { get; set; }
        public string DescriptionOfService { get; set; }
        public string PricingOption { get; set; }
        public string PricingStartsAt { get; set; }
        public string Currency { get; set; }
        public string ThumbnailImage { get; set; }
        public List<string> ProjectGallery { get; set; }
        public bool IsActive { get; set; }
        public List<Review> ClientReviews { get; set; } = new List<Review>();
    }
    public class Review {
        public string ReviewDescription { get; set; }
        public int StarRating { get; set; }
        public string ClientID { get; set; }
        public string ClientUserName { get; set; }
        public string ClientCity { get; set; }
        public string ClientState { get; set; }
        public string ClientPostalCode { get; set; }
    }
}
