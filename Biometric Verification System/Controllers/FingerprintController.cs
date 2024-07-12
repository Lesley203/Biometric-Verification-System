using Biometric_Verification_System.Areas.Identity.Data;
using Biometric_Verification_System.Models;
//using ImageProcessor.Imaging.Formats;
using libzkfpcsharp;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Windows;
using System.Runtime.InteropServices;
using System.Security.Claims;
using Sample;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Biometric_Verification_System.SignalR;
using System.Drawing.Imaging;

namespace Biometric_Verification_System.Controllers
{
    public class FingerprintController : Controller
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<HomeController> _logger;
        private readonly BioVerifyDbContext _context;
        private readonly UserManager<BioVerifyUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserStore<BioVerifyUser> _userStore;
        private readonly IHubContext<FingerprintHub> _hubContext;
       
        private readonly SignInManager<BioVerifyUser> _signInManager;
        public FingerprintController(BioVerifyDbContext dbContext, SignInManager<BioVerifyUser> signInManager,
            UserManager<BioVerifyUser> userManager, RoleManager<IdentityRole> roleManager, ILogger<HomeController> logger, IUserStore<BioVerifyUser> userStore, IServiceScopeFactory scopeFactory, IHubContext<FingerprintHub> hubContext)
        {

            _signInManager = signInManager;
            this._userManager = userManager;
            _context = dbContext;
            _roleManager = roleManager;
            _logger = logger;
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _userStore = userStore;
        }

        IntPtr mDevHandle = IntPtr.Zero;
        IntPtr mDBHandle = IntPtr.Zero;
        IntPtr FormHandle = IntPtr.Zero;
        private static bool bIsTimeToDie = false;
        private static bool IsRegister = false;
        private static bool bIdentify = true;
        byte[] FPBuffer;
        int RegisterCount = 0;
        const int REGISTER_FINGER_COUNT = 3;

        byte[][] RegTmps = new byte[3][];
        byte[] RegTmp = new byte[2048];
        byte[] CapTmp = new byte[2048];
        int cbCapTmp = 2048;
        private static int cbRegTmp = 0;
        int iFid = 1;
        private Thread captureThread = null;

        private int mfpWidth = 0;
        private int mfpHeight = 0;

        const int MESSAGE_CAPTURED_OK = 0x0400 + 6;

        public object BitMapFormat { get; private set; }

        [DllImport("user32.dll", EntryPoint = "SendMessageA")]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        public delegate void FingerprintProcessedHandler(string message);
        public static event FingerprintProcessedHandler OnFingerprintProcessed;


        private void UpdateTempDataMessage(string message)
        {
            TempData["Message"] = message;
        }
        public string BitmapToBase64(Bitmap bmp)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Bmp);
                byte[] imageBytes = ms.ToArray();
                return Convert.ToBase64String(imageBytes);
            }
        }
        public async Task<IActionResult> Index(string Id)
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
            ViewData["Id"] = Id;
            return View();
        }
        private string deviceStatus = "red";
        public async Task<IActionResult> Initialize(string Id)
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
            ViewData["Id"] = Id; // Ensure the user Id is set here

            zkfp2.Init();
            ViewBag.Message = "The Device initialized successfully! You can now Run the scanner!!";
            //await _hubContext.Clients.All.SendAsync("UpdateDeviceStatus", "yellow");
            ViewBag.DeviceStatus = deviceStatus;
            ViewBag.DeviceStatus = "yellow";
            return View("Index", "Fingerprint");
        }

        public async Task<IActionResult> Open(string Id)
        {
            int deviceIndex = 0;
          

            OnFingerprintProcessed += UpdateTempDataMessage;

            if (IntPtr.Zero == (mDevHandle = zkfp2.OpenDevice(deviceIndex)))
            {
                ViewBag.Message = "OpenDevice fail";
                return View("Index");
            }
            if (IntPtr.Zero == (mDBHandle = zkfp2.DBInit()))
            {
                ViewBag.Message = "Init DB fail";
                zkfp2.CloseDevice(mDevHandle);
                mDevHandle = IntPtr.Zero;
                return View("Index");
            }

            RegisterCount = 0;
            cbRegTmp = 0;
            for (int i = 0; i < 3; i++)
            {
                RegTmps[i] = new byte[2048];
            }
            byte[] paramValue = new byte[4];
            int size = 4;
            zkfp2.GetParameters(mDevHandle, 1, paramValue, ref size);
            zkfp2.ByteArray2Int(paramValue, ref mfpWidth);

            size = 4;
            zkfp2.GetParameters(mDevHandle, 2, paramValue, ref size);
            zkfp2.ByteArray2Int(paramValue, ref mfpHeight);

            FPBuffer = new byte[mfpWidth * mfpHeight];

            captureThread = new Thread(() => DoCapture(Id));
            captureThread.IsBackground = true;
            captureThread.Start();

            ViewBag.Message = "The Fingerprint Device has connected successfully and running!";
            await _hubContext.Clients.All.SendAsync("UpdateDeviceStatus", "green");
            ViewBag.DeviceStatus = "green";
            ViewBag.Status = "green";
            return View("Index");
        }

        private void DoCapture(string Id)
        {
            while (true)
            {
                cbCapTmp = 2048;
                int ret = zkfp2.AcquireFingerprint(mDevHandle, FPBuffer, CapTmp, ref cbCapTmp);
                if (ret == zkfp.ZKFP_ERR_OK)
                {
                    ProcessFingerprint(Id);
                }
                Thread.Sleep(200);
            }
        }

        private async void ProcessFingerprint(string Id)
        {
            _logger.LogInformation("ProcessFingerprint method called");
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<BioVerifyDbContext>();
                using (MemoryStream ms = new MemoryStream())
                {
                    BitmapFormat.GetBitmap(FPBuffer, mfpWidth, mfpHeight, ms);
                    Bitmap bmp = new Bitmap(ms);
                    string base64Image = BitmapToBase64(bmp);

                    await _hubContext.Clients.All.SendAsync("ReceiveFingerprint", base64Image);
                }

                string message = "";

                if (IsRegister)
                {
                    int ret = zkfp.ZKFP_ERR_OK;
                    int fid = 0, score = 0;
                    ret = zkfp2.DBIdentify(mDBHandle, CapTmp, ref fid, ref score);

                    var fingerprints = context.FingerprintData.ToList();

                    foreach (var fingerprint in fingerprints)
                    {
                        int retr = zkfp2.DBMatch(mDBHandle, CapTmp, fingerprint.Data);
                        if (retr > 0)
                        {
                            message = $"This finger is already registered on the database!";
                            await _hubContext.Clients.All.SendAsync("ReceiveMessage", message);
                            return;
                        }
                    }

                    if (RegisterCount > 0 && zkfp2.DBMatch(mDBHandle, CapTmp, RegTmps[RegisterCount - 1]) <= 0)
                    {
                        message = "Please press the same finger 3 times for the enrollment";
                        await _hubContext.Clients.All.SendAsync("ReceiveMessage", message);
                        return;
                    }

                    Array.Copy(CapTmp, RegTmps[RegisterCount], cbCapTmp);
                    RegisterCount++;

                    if (RegisterCount >= REGISTER_FINGER_COUNT)
                    {
                        RegisterCount = 0;

                        if (zkfp.ZKFP_ERR_OK == (ret = zkfp2.DBMerge(mDBHandle, RegTmps[0], RegTmps[1], RegTmps[2], RegTmp, ref cbRegTmp)))
                        {
                            SaveFingerprintToDatabase(context, RegTmp, Id); // Pass the userId
                            message = "Enroll successful";
                            await _hubContext.Clients.All.SendAsync("ReceiveMessage", message);
                            AutoCloseDevice() ; return;
                        }
                        else
                        {
                            message = "Enroll failed, error code=" + ret;
                            await _hubContext.Clients.All.SendAsync("ReceiveMessage", message);
                        }

                        IsRegister = false;
                        return;
                    }
                    else
                    {
                        message = "You need to press the finger " + (REGISTER_FINGER_COUNT - RegisterCount) + " more times";
                        await _hubContext.Clients.All.SendAsync("ReceiveMessage", message);
                    }
                }
                else
                {
                    if (cbRegTmp <= 0)
                    {
                        message = "Please register your finger first!";
                        await _hubContext.Clients.All.SendAsync("ReceiveMessage", message);
                        return;
                    }
                    else if (bIdentify)
                    {
                        int ret = zkfp.ZKFP_ERR_OK;
                        int fid = 0, score = 0;
                        ret = zkfp2.DBIdentify(mDBHandle, CapTmp, ref fid, ref score);

                        if (zkfp.ZKFP_ERR_OK == ret)
                        {
                            ViewBag.Message = "Identify successful, fid= " + fid + ", score=" + score + "!";

                            return;
                        }
                        else
                        {
                            ViewBag.Message = "Identify failed, ret= " + ret;
                            return;
                        }
                    }
                    else
                    {
                        //var fingerprints
                        //= context.FingerprintData.ToList();

                        //if (!fingerprints.Any())
                        //{
                        //    message = "No fingerprint templates found in the database.";
                        //    await _hubContext.Clients.All.SendAsync("ReceiveMessage", message);
                        //    return;
                        //}

                        //foreach (var fingerprint in fingerprints)
                        //{
                        //    int ret = zkfp2.DBMatch(mDBHandle, CapTmp, fingerprint.Data);
                        //    if (ret > 0)
                        //    {
                        //        message = "Fingerprint Match successful!";
                        //        await _hubContext.Clients.All.SendAsync("ReceiveMessage", message);
                        //        return;
                        //    }
                        //}
                        //message = "Fingerprint not matching!";
                        //await _hubContext.Clients.All.SendAsync("ReceiveMessage", message);
                        //return;


                        var fingerprints = context.FingerprintData.Include(f => f.GetBioUser).ToList();

                        if (!fingerprints.Any())
                        {
                            message = "No fingerprint templates found in the database.";
                            await _hubContext.Clients.All.SendAsync("ReceiveMessage", message);
                            return;
                        }

                        foreach (var fingerprint in fingerprints)
                        {
                            int ret = zkfp2.DBMatch(mDBHandle, CapTmp, fingerprint.Data);
                            if (ret > 0)
                            {
                                var user = fingerprint.GetBioUser;
                                message = "Fingerprint Match successful!";
                                await _hubContext.Clients.All.SendAsync("ReceiveUserDetails", user.Id, user.FirstName, user.LastName, user.Email);
                                await _hubContext.Clients.All.SendAsync("ReceiveMessage", message);
                                return;
                            }
                        }
                        message = "Fingerprint not matching!";
                        await _hubContext.Clients.All.SendAsync("ReceiveMessage", message);
                        return;
                    }
                }
                ViewBag.Message = "Out of Context";
            }
        }

        private void SaveFingerprintToDatabase(BioVerifyDbContext context, byte[] fingerprintData, string Id)
        {
            var fingerprint = new FingerprintDeviceModel
            {
                Data = fingerprintData,
                Id = Id // Store the user Id
            };
            context.FingerprintData.Add(fingerprint);
            context.SaveChanges();
        }

        public async Task<IActionResult> enroll(string Id)
        {
            _logger.LogInformation("Enroll method called");

            // Ensure Id is received and handled correctly
            var userI = await this._userManager.GetUserAsync(User);
            string firstName = userI.FirstName;
            string lastName = userI.LastName;

            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            ViewBag.Role = roleClaim;

            if (!IsRegister)
            {
                IsRegister = true;
                bIdentify = false;
                RegisterCount = 0;
                cbRegTmp = 0;
                ViewData["Id"] = Id;
                ViewBag.Message = "Please press your finger 3 times!";
                _logger.LogInformation("Enrollment process started");
            }
            else
            {
                _logger.LogWarning("Enrollment already in progress");
            }
            ViewBag.DeviceStatus = "green";
            ViewData["Id"] = Id;
            return View("Index");
        }




        public IActionResult CloseDevicee()
        {
            CloseDevice();
            RegisterCount = 0;
            Thread.Sleep(1000);
            ViewBag.Message = "The device successfully stopped running!";
            ViewBag.DeviceStatus = "red";
            return View("Index");


        }
        private async void AutoCloseDevice()
        {

            CloseDevice();
            RegisterCount = 0;
            Thread.Sleep(1000);
            ViewBag.Message = "The device successfully stopped running!";
            await _hubContext.Clients.All.SendAsync("UpdateDeviceStatus", "red");
            return;

        }
        public IActionResult Verify()
        {
            _logger.LogInformation("Verify method called");
            if (bIdentify)
            {
                bIdentify = false;
                IsRegister = false;
                cbRegTmp = 5;
                ViewBag.Message = "Please press your finger to Veirify!";
                _logger.LogInformation("Verification process started");
            }
            else
            {
                _logger.LogWarning("Verification already in progress");
            }
            return View("Index");
        }
        private void CloseDevice()
        {

            bIsTimeToDie = true;
            Thread.Sleep(1000);

            zkfp2.CloseDevice(mDevHandle);
            zkfp2.Terminate();
            mDevHandle = IntPtr.Zero;

        }
        public async Task<IActionResult> Intake()
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
            var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var file_check = _context.FingerprintData.Where(m => m.Id == user).FirstOrDefault();
            IEnumerable<BioVerifyUser> solutions;

            IEnumerable<BioVerifyUser> usersWithoutFile = _context.Users
                .Where(u => !_context.FingerprintData.Any(mf => mf.Id == u.Id));

          

            TempData["UsersWithoutFile"] = usersWithoutFile;
            TempData["FileChecked"] = file_check != null ? "True" : null;
            var ino = _context.FingerprintData.Any(mf => mf.Id == user);
            TempData["File"] = ino;
            return View();
        }
    }
}
