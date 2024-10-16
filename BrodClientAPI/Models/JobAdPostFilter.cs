namespace BrodClientAPI.Models
{
    public class JobAdPostFilter
    {
        public string Postcode { get; set; }
        public int? ProximityToWorkMin { get; set; }
        public int? ProximityToWorkMax { get; set; }
        public List<string> JobCategories { get; set; }
        public string Keywords { get; set; }
        public List<string> AvailabilityToWork { get; set; }
        public int? CallOutRateMin { get; set; }
        public int? CallOutRateMax { get; set; }
        public int? PricingStartsMin { get; set; }
        public int? PricingStartsMax { get; set; }
    }
}
