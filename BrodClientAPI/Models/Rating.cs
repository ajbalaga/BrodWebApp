using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace BrodClientAPI.Models
{
    public class Rating
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }
        public string tradieId { get; set; }
        public string clientId { get; set; }
        public string jobId { get; set; }
        public string jobAdId { get; set; }
        public int rating { get; set; }
        public string clientLocation { get; set; }
        public string ratingDescription { get; set; }
    }
}
