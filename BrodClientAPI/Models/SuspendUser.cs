namespace BrodClientAPI.Models
{
    public class SuspendUser
    {
        public string userID { get; set; }
        public int WeeksSuspended { get; set; }
        public bool isSuspended { get; set; }
    }
}
