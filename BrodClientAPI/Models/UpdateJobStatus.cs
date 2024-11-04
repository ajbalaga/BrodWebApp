namespace BrodClientAPI.Models
{
    public class UpdateJobStatus
    {
        public string TradieID { get; set; }
        public string JobID { get; set; }
        public string Status { get; set; }
        public string? JobActionDate { get; set; }
    }
}
