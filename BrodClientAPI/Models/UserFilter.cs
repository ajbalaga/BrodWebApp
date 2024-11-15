using Microsoft.AspNetCore.Authentication;

namespace BrodClientAPI.Models
{
    public class UserFilter
    {
        public string TypeOfWork { get; set; }
        public string Status { get; set; }
        public DateTime SubmissionDateFrom { get; set; }
        public DateTime SubmissionDateTo { get; set; }

    }
}
