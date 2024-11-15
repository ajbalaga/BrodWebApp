using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace BrodClientAPI.Models
{
    public class ClientMessage
    {

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }
        public string ClientId { get; set; }
        public string TradieId { get; set; }
        public string TradieName { get; set; }
        public string Tradielocation { get; set; }
        public string Picture { get; set; }
        public string Message { get; set; }
        public DateTime TimeStamp { get; set; }
        public bool SentByClient { get; set; }
    }
}
