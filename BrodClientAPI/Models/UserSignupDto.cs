namespace BrodClientAPI.Models
{
    public class UserSignupDto
    {
        public User User { get; set; } // The User details
        public IFormFile ProfilePicture { get; set; } // The profile picture
        public List<IFormFile> CertificationFiles { get; set; } // The certification files
    }

}
