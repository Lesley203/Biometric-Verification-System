using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Biometric_Verification_System.Models
{
    public class EnrolUserModel : IdentityUser
    {
        [Required]
        [StringLength(50)]
        [DataType(DataType.Text)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        [DataType(DataType.Text)]
        public string LastName { get; set; }

        [Required]
        [StringLength(50)]
        [DataType(DataType.Text)]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Phone Number is required")]
        [Display(Name = "Phone Number")]
        [DataType(DataType.PhoneNumber)]
        [RegularExpression(@"^1?[0-9]{10}$", ErrorMessage = "Not a valid Phone number")]
        public string PhoneNumb { get; set; }

        [Required]
        [Display(Name = "Creation Date of Account")]
        [DataType(DataType.DateTime)]
        public System.DateTime Date_AccountCreated { get; set; }


        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }


        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        //public UserFileModel UserFileModel { get; set; }

        public EnrolUserModel()
        {
            Date_AccountCreated = DateTime.Now;
        }
    }
}
