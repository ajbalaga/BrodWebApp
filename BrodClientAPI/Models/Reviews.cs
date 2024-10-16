using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace BrodClientAPI.Models
{
    public class Reviews
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }
        public string ServiceID { get; set; }
        public string ClientID { get; set; }
        public string ClientUserName { get; set; }
        public string ClientCity { get; set; }
        public string ClientState { get; set; }
        public string ClientPostalCode { get; set; }
        public int StarRating { get; set; }
        public string ReviewDescription { get; set; }
    }
}
