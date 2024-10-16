using System.ComponentModel.DataAnnotations;

namespace BrodClientAPI.Models
{
    public class LoginInput
    {
        [DataType(DataType.EmailAddress)]
        public required string Email { get; set; }
        
        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public required string Password { get; set; }
    }
}
