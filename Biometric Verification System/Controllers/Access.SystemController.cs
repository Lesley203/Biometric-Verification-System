
using Biometric_Verification_System.Areas.Identity.Data;
using Biometric_Verification_System.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Biometric_Verification_System.Controllers
{
    public class Access : Controller
    {

        private readonly BioVerifyDbContext _context;
        private readonly UserManager<BioVerifyUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserStore<BioVerifyUser> _userStore;
        //private readonly ILogger<LoginModel> _logger;
        private readonly SignInManager<BioVerifyUser> _signInManager;
        public Access(BioVerifyDbContext dbContext, SignInManager<BioVerifyUser> signInManager,
            UserManager<BioVerifyUser> userManager, RoleManager<IdentityRole> roleManager, IUserStore<BioVerifyUser> userStore)
        {

            _signInManager = signInManager;
            this._userManager = userManager;
            _context = dbContext;
            _roleManager = roleManager;
            _userStore = userStore;
            //_logger = logger;

        }
        [HttpGet]
        public IActionResult Modal()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPos(LoginModell model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                if (ModelState.IsValid)
                {

                    var result = await _signInManager.PasswordSignInAsync(model.Email!, model.Password!, model.RememberMe, lockoutOnFailure: false);
                    if (result.Succeeded)
                    {

                        return RedirectToAction("Index", "Dashboard");
                    }
                }

            }


            return View(model);
        }
    }
}
