using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace BrodClientAPI.Models
{
    public class TradieMessage
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }
        public string ClientId { get; set; }
        public string TradieId { get; set; }
        public string ClientName { get; set; }
        public string Clientlocation { get; set; }
        public string Picture { get; set; }
        public string Message { get; set; }
        public DateTime TimeStamp { get; set; }
        public bool SentByTradie { get; set; }
    }
}
