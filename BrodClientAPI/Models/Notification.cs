using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace BrodClientAPI.Models
{
    public class Notification
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }
        public string userID { get; set; }
        public string Content { get; set; }
        public bool isRead { get; set; }
    }
}
