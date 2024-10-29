//ST10024620
//Sauraav Jayrajh
using Firebase.Database;
using Microsoft.AspNetCore.Mvc;
using Sauraav_Prog_AgroEnergy.Models;
using Firebase.Database.Query;

namespace Sauraav_Prog_AgroEnergy.Controllers
{
    public class FarmerController : Controller
    {
        private string FirebaseUrl = "https://sauraavagroenergy-default-rtdb.firebaseio.com/";

        private FirebaseClient GetAuthenticatedClient()
        {
            var authToken = HttpContext.Session.GetString("authToken");
            if (string.IsNullOrEmpty(authToken))
            {
                return null;
            }
            return new FirebaseClient(
                FirebaseUrl,
                new FirebaseOptions { AuthTokenAsyncFactory = () => Task.FromResult(authToken) }
            );
        }

        [HttpGet]
        public IActionResult AddItem()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem(ItemModel newItem, IFormFile itemImage)
        {
            if (newItem == null)
            {
                return BadRequest("Invalid item details");
            }

            if (itemImage != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await itemImage.CopyToAsync(memoryStream);
                    newItem.itemImageAsBase64 = Convert.ToBase64String(memoryStream.ToArray());
                }
            }

            var itemID = Guid.NewGuid().ToString();
            newItem.itemID = Convert.ToInt32(itemID.GetHashCode());

            var firebaseClient = GetAuthenticatedClient();
            if (firebaseClient == null)
            {
                return Unauthorized();
            }

            await firebaseClient
                .Child("items")
                .Child(itemID)
                .PutAsync(newItem);

            return RedirectToAction("ViewItems");
        }

        [HttpGet]
        public async Task<IActionResult> ViewItems(string farmerUID = null)
        {
            var role = HttpContext.Session.GetString("userRole");
            var currentUserUID = HttpContext.Session.GetString("currentUser");

            if (role == null)
            {
                return RedirectToAction("Login", "Authentication");
            }

            if (farmerUID == null)
            {
                farmerUID = currentUserUID;
            }

            var firebaseClient = GetAuthenticatedClient();
            if (firebaseClient == null)
            {
                return Unauthorized();
            }

            try
            {
                var items = await firebaseClient
                    .Child("items")
                    .OrderBy("FarmerUID")
                    .EqualTo(farmerUID)
                    .OnceAsync<ItemModel>();

                return View(items.Select(i => i.Object).ToList());
            }
            catch (Exception e)
            {
                return Unauthorized();
            }
        }

        [HttpGet]
        public async Task<IActionResult> FarmerProfile(string farmerUID = null)
        {
            var role = HttpContext.Session.GetString("userRole");
            var currentUserUID = HttpContext.Session.GetString("currentUser");

            if (role == null)
            {
                return RedirectToAction("Login", "Authentication");
            }

            if (farmerUID == null)
            {
                farmerUID = currentUserUID;
            }

            var firebaseClient = GetAuthenticatedClient();
            if (firebaseClient == null)
            {
                return Unauthorized();
            }

            var farmer = await firebaseClient
                .Child("users")
                .Child("farmers")
                .Child(farmerUID)
                .OnceSingleAsync<FarmerModel>();

            if (farmer == null)
            {
                return NotFound("Farmer not found.");
            }

            var items = await firebaseClient
                .Child("items")
                .OrderBy("FarmerUID")
                .EqualTo(farmerUID)
                .OnceAsync<ItemModel>();

            var viewModel = new FarmerProfileViewModel
            {
                Farmer = farmer,
                Items = items.Select(i => i.Object).ToList()
            };

            return View(viewModel);
        }
    }
}   