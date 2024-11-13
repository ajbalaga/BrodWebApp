using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace BrodClientAPI.Models
{
    public class Messages
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }
        public string ClientId { get; set; }
        public string TradieId { get; set; }
        public DateTime TimeStamp { get; set; }
        public string message { get; set; }
    }
}
