using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace BrodClientAPI.Models
{
    public class OTPSMS
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }
        public string phoneNumber { get; set; }
        public string OTP { get; set; }
        public DateTime expirationMin { get; set; }
    }
}
