using Biometric_Verification_System.Areas.Identity.Data;
using Biometric_Verification_System.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Biometric_Verification_System.Controllers
{
    public class EnrollController : Controller
    {
        private readonly BioVerifyDbContext _context;
        private readonly UserManager<BioVerifyUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserStore<BioVerifyUser> _userStore;
        private readonly SignInManager<BioVerifyUser> _signInManager;
        public EnrollController(BioVerifyDbContext dbContext, SignInManager<BioVerifyUser> signInManager,
            UserManager<BioVerifyUser> userManager, RoleManager<IdentityRole> roleManager, IUserStore<BioVerifyUser> userStore)
        {

            _signInManager = signInManager;
            this._userManager = userManager;
            _context = dbContext;
            _roleManager = roleManager;
            _userStore = userStore;
            //_logger = logger;

        }
        public async Task<IActionResult> Index()
        {
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


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(EnrolUserModel model)
        {
            if (await _userManager.FindByEmailAsync(model.Email) == null)
            {
                if (ModelState.IsValid)
                {
                    var user = new BioVerifyUser();
                    user.Email = model.Email;
                    user.UserName = model.Email;
                    user.EmailConfirmed = true;
                    user.FirstName = model.FirstName;
                    user.LastName = model.LastName;
                    user.PhoneNumb = model.PhoneNumb;
                    user.Gender = model.Gender;
                    await _userStore.SetUserNameAsync(user, model.Email, CancellationToken.None);
                    var result = await _userManager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, "Accesor");
                        TempData["AlertMess"] = "You are about to capture fingerprint for enrolled user! If yyou want to capture later, click Later ";
                        return RedirectToAction("Index", "Fingerprint", new { Id = user.Id });
                    }
                }

            }


            return View(model);
        }
    }
}
