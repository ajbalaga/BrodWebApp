namespace BrodClientAPI.Models
{
    public class HireTradie
    {
        public string ClientID { get; set; }
        public string TradieID { get; set; }
        public string JobAdTitle { get; set; }
        public string ServiceID { get; set; }
        public string Status { get; set; } //Sent Offer, Completed, In Progress, Declined, Cancelled
        public string DescriptionServiceNeeded { get; set; }
        public string ClientContactNumber { get; set; }
        public string ClientPostCode { get; set; }
        public string StartDate { get; set; }
        public string CompletionDate { get; set; }
        public decimal ClientBudget { get; set; }
        public string BudgetCurrency { get; set; }
        public string JobActionDate { get; set; }
    }
}
