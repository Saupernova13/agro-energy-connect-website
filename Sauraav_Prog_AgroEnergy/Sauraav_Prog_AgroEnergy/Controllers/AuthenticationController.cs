//ST10024620
//Sauraav Jayrajh
using Firebase.Auth;
using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Sauraav_Prog_AgroEnergy.Models;

namespace Sauraav_Prog_AgroEnergy.Controllers
{
    public class AuthenticationController : Controller
    {
        private readonly FirebaseAuthProvider _authProvider;
        private FirebaseAuthLink _authLink;

        public AuthenticationController()
        {
            _authProvider = new FirebaseAuthProvider(new FirebaseConfig("AIzaSyANoHZ_GAyvljFQOuinn-a3oVTknLgrWJE"));
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(FarmerModel registerModel, IFormFile FarmerProfileImage)
        {
            try
            {
                var authLink = await _authProvider.CreateUserWithEmailAndPasswordAsync(registerModel.accountEmail, registerModel.accountPassword);

                if (authLink.User.LocalId != null)
                {
                    registerModel.FarmerUID = authLink.User.LocalId;
                    registerModel.accountRegistrationTime = DateTime.Now;
                    registerModel.IsApproved = false;

                    if (FarmerProfileImage != null)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await FarmerProfileImage.CopyToAsync(memoryStream);
                            registerModel.FarmerProfileImageBase64 = Convert.ToBase64String(memoryStream.ToArray());
                        }
                    }

                    var firebaseClient = new FirebaseClient(
                        "https://sauraavagroenergy-default-rtdb.firebaseio.com/",
                        new FirebaseOptions { AuthTokenAsyncFactory = () => Task.FromResult(authLink.FirebaseToken) }
                    );

                    await firebaseClient
                        .Child("users")
                        .Child("farmers")
                        .Child(registerModel.FarmerUID)
                        .PutAsync(registerModel);

                    return View("AwaitingApproval", registerModel);
                }
            }
            catch (FirebaseAuthException ex)
            {
                var error = JsonConvert.DeserializeObject<FirebaseErrorModel>(ex.ResponseData);
                ModelState.AddModelError(string.Empty, error.error.message);
                return View(registerModel);
            }

            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AccountModel loginModel)
        {
            if (!ModelState.IsValid)
            {
                return View(loginModel);
            }

            try
            {
                var fbAuthLink = await _authProvider.SignInWithEmailAndPasswordAsync(loginModel.accountEmail, loginModel.accountPassword);
                string currentUserId = fbAuthLink.User.LocalId;

                if (currentUserId != null)
                {
                    var firebaseClient = new FirebaseClient(
                        "https://sauraavagroenergy-default-rtdb.firebaseio.com/",
                        new FirebaseOptions { AuthTokenAsyncFactory = () => Task.FromResult(fbAuthLink.FirebaseToken) }
                    );

                    // Check if user is an admin
                    var admin = await firebaseClient
                        .Child("users")
                        .Child("admins")
                        .Child(currentUserId)
                        .OnceSingleAsync<AdminModel>();

                    if (admin != null)
                    {
                        HttpContext.Session.SetString("currentUser", currentUserId);
                        HttpContext.Session.SetString("userRole", "admin");
                        HttpContext.Session.SetString("userName", $"{admin.AdminName} {admin.AdminSurname}");
                        HttpContext.Session.SetString("authToken", fbAuthLink.FirebaseToken);
                        return RedirectToAction("AdminDashboard", "Admin");
                    }
                    else
                    {
                        // Check if user is a farmer
                        var farmer = await firebaseClient
                            .Child("users")
                            .Child("farmers")
                            .Child(currentUserId)
                            .OnceSingleAsync<FarmerModel>();

                        if (farmer != null && farmer.IsApproved)
                        {
                            HttpContext.Session.SetString("currentUser", currentUserId);
                            HttpContext.Session.SetString("userRole", "farmer");
                            HttpContext.Session.SetString("userName", $"{farmer.FarmerName} {farmer.FarmerSurname}");
                            HttpContext.Session.SetString("authToken", fbAuthLink.FirebaseToken);

                            // Store profile image in session if available
                            if (!string.IsNullOrEmpty(farmer.FarmerProfileImageBase64))
                            {
                                HttpContext.Session.SetString("profileImageBase64", farmer.FarmerProfileImageBase64);
                            }

                            return RedirectToAction("Index", "Home");
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Account pending approval by admin.");
                            return View(loginModel);
                        }
                    }
                }
            }
            catch (FirebaseAuthException ex)
            {
                var firebaseEx = JsonConvert.DeserializeObject<FirebaseErrorModel>(ex.ResponseData);
                ModelState.AddModelError(string.Empty, firebaseEx.error.message);
                return View(loginModel);
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Append(".AspNetCore.Session", "", new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(-1),
                IsEssential = true,
            });
            return RedirectToAction("Index", "Home");
        }
    }
}