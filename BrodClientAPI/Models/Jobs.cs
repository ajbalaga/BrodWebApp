using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace BrodClientAPI.Models
{
    public class Jobs
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }
        public string ServiceID { get; set; }
        public string ClientID { get; set; }
        public string TradieID { get; set; }
        public string Status { get; set; }
        public string DescriptionServiceNeeded { get; set; }
        public string? ClientName { get; set; }
        public string? ClientContactNumber { get; set; }
        public string? ClientCity { get; set; }
        public string? ClientState { get; set; }
        public string? ClientPostalCode { get; set; }
        public string? JobPostAdTitle { get; set; }
        public string? StartDate { get; set; }
        public string? CompletionDate { get; set; }
        public decimal? ClientBudget { get; set; }
        public string? BudgetCurrency { get; set; }
        public string? TradieName { get; set; }
        public string? Proximity { get; set; }
        public string? TradieLocation { get; set; }
        public string? JobActionDate { get; set; }
        public string? JobAdDescription { get; set; }
        public int Rating { get; set; } = 0;
        public string? RatingDesc { get; set; }

    }
}
