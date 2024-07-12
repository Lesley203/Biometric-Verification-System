using Biometric_Verification_System.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Biometric_Verification_System.Controllers
{
    public class DashboardController : Controller
    {

        private readonly BioVerifyDbContext _context;
        private readonly UserManager<BioVerifyUser> _userManager;
        public DashboardController(BioVerifyDbContext dbContext, UserManager<BioVerifyUser> userManager)
        {
            this._userManager = userManager;
            _context = dbContext;
        }

        public async Task<IActionResult> Index()
        {
            int userCount = _context.Users.Count();
            ViewBag.UserCount = userCount;

            int FingerprintCount = _context.FingerprintData.Count();
            ViewBag.FingerPrintCount = FingerprintCount;

            var userI = await this._userManager.GetUserAsync(User);
            string firstName = userI.FirstName;
            string lastName = userI.LastName;
            string email = userI.Email;
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            ViewBag.Role = roleClaim;
            ViewData["LastNameUser"] = firstName + " " + lastName;
            ViewData["Surname"] = lastName;
            ViewData["email"] = email;
            return View();
        }

        public IActionResult Ind()
        {
           

            return View();
        }
    }
}
