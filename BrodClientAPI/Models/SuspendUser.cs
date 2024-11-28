namespace BrodClientAPI.Models
{
    public class SuspendUser
    {
        public string userID { get; set; }
        public int weeksSuspended { get; set; }
        public DateTime suspensionStartDate { get; set; }
        public bool isSuspended { get; set; }
    }
}
