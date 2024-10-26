using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace BrodClientAPI.Models
{
    public class OTPEMAIL
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }
        public string email { get; set; }
        public string OTP { get; set; }
        public DateTime expirationMin { get; set; }
    }
}
