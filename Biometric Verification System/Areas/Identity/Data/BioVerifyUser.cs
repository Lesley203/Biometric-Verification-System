using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Biometric_Verification_System.Models;

namespace Biometric_Verification_System.Areas.Identity.Data;

// Add profile data for application users by adding properties to the BioVerifyUser class
public class BioVerifyUser : IdentityUser
{
    [Required]
    [StringLength(50)]
    [DataType(DataType.Text)]
    public string FirstName { get; set; }

    [Required]
    [StringLength(50)]
    [DataType(DataType.Text)]
    public string LastName { get; set; }

    //[Required]
    //[DisplayName("Identity No")]
    //[RegularExpression(@"^\d{13}$", ErrorMessage = "The field must contain exactly 13 digits")]
    //public string IdentityNo { get; set; }

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


    //public FingerprintDeviceModel FingerprintDevice { get; set; }


    //public UserFileModel UserFileModel { get; set; }

    public BioVerifyUser()
    {
        Date_AccountCreated = DateTime.Now;
    }
}

