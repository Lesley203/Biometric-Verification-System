using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Biometric_Verification_System.Models
{
    public class LoginModell
    {

        [Required]
        [EmailAddress]
        public string Email { get; set; }

       
        [Required]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }
}
