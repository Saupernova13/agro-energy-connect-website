//ST10024620
//Sauraav Jayrajh
using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sauraav_Prog_AgroEnergy.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Sauraav_Prog_AgroEnergy.Controllers
{
    public class ItemController : Controller
    {
        private readonly FirebaseClient _firebaseClient;
        private readonly ILogger<ItemController> _logger;
        private string FirebaseUrl = "https://sauraavagroenergy-default-rtdb.firebaseio.com/";
        public ItemController(ILogger<ItemController> logger)
        {
            _firebaseClient = new FirebaseClient(FirebaseUrl);
            _logger = logger;
        }

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
            if (newItem == null || string.IsNullOrEmpty(newItem.FarmerUID))
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
            await _firebaseClient
                .Child("items")
                .Child(itemID)
                .PutAsync(newItem);

            return RedirectToAction("ViewItems", new { farmerUID = newItem.FarmerUID });
        }

        [HttpGet]
        public async Task<IActionResult> ViewItems(string farmerUID)
        {
            var items = await _firebaseClient
                .Child("items")
                .OrderBy("FarmerUID")
                .EqualTo(farmerUID)
                .OnceAsync<ItemModel>();

            return View(items.Select(i => i.Object).ToList());
        }

        [HttpGet]
        public async Task<IActionResult> ItemDetails(int itemId)
        {
            var firebaseClient = GetAuthenticatedClient();
            if (firebaseClient == null)
            {
                return Unauthorized();
            }

            try
            {
                var items = await firebaseClient
                    .Child("items")
                    .OnceAsync<ItemModel>();

                var item = items.Select(i => i.Object).FirstOrDefault(i => i.itemID == itemId);

                if (item == null)
                {
                    _logger.LogWarning("Item not found for item ID: {ItemId}", itemId);
                    return NotFound();
                }

                _logger.LogInformation("Item found for item ID: {ItemId}, Name: {ItemName}", itemId, item.itemName);
                return View(item);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error fetching item details for item ID: {ItemId}", itemId);
                return Unauthorized();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Marketplace(string searchString, string category)
        {
            var firebaseClient = GetAuthenticatedClient();
            if (firebaseClient == null)
            {
                return Unauthorized();
            }

            try
            {
                var items = await firebaseClient
                    .Child("items")
                    .OnceAsync<ItemModel>();

                var itemList = items.Select(i => i.Object).ToList();

                if (!string.IsNullOrEmpty(searchString))
                {
                    itemList = itemList.Where(i => i.itemName.Contains(searchString, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrEmpty(category))
                {
                    itemList = itemList.Where(i => i.itemCategory == category).ToList();
                }

                var categories = itemList.Select(i => i.itemCategory).Distinct().ToList();

                var viewModel = new MarketplaceViewModel
                {
                    Items = itemList,
                    Categories = categories,
                    SelectedCategory = category
                };

                return View(viewModel);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error fetching items for marketplace");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditItem(int itemId)
        {
            var firebaseClient = GetAuthenticatedClient();
            if (firebaseClient == null)
            {
                return Unauthorized();
            }

            var role = HttpContext.Session.GetString("userRole");
            var currentUserUID = HttpContext.Session.GetString("currentUser");

            try
            {
                var items = await firebaseClient
                    .Child("items")
                    .OnceAsync<ItemModel>();

                var item = items.Select(i => i.Object).FirstOrDefault(i => i.itemID == itemId);

                if (item == null)
                {
                    return NotFound();
                }

                if (item.FarmerUID != currentUserUID && role != "admin")
                {
                    return Forbid();
                }

                return View(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching item details for editing. Item ID: {itemId}", itemId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditItem(ItemModel updatedItem, IFormFile itemImage)
        {
            var firebaseClient = GetAuthenticatedClient();
            if (firebaseClient == null)
            {
                return Unauthorized();
            }

            var role = HttpContext.Session.GetString("userRole");
            var currentUserUID = HttpContext.Session.GetString("currentUser");

            if (updatedItem.FarmerUID != currentUserUID && role != "admin")
            {
                return Forbid();
            }

            try
            {
                var items = await firebaseClient
                    .Child("items")
                    .OnceAsync<ItemModel>();

                var existingItemKvp = items.FirstOrDefault(i => i.Object.itemID == updatedItem.itemID);

                if (existingItemKvp == null)
                {
                    _logger.LogError("Item not found. Item ID: {itemID}", updatedItem.itemID);
                    return NotFound("Item not found.");
                }

                var existingItem = existingItemKvp.Object;

                if (itemImage != null && itemImage.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await itemImage.CopyToAsync(memoryStream);
                        updatedItem.itemImageAsBase64 = Convert.ToBase64String(memoryStream.ToArray());
                    }
                }
                else
                {
                    updatedItem.itemImageAsBase64 = existingItem.itemImageAsBase64;
                }

                await firebaseClient
                    .Child("items")
                    .Child(existingItemKvp.Key)
                    .PutAsync(updatedItem);

                return RedirectToAction("ItemDetails", new { itemId = updatedItem.itemID });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating item details. Item ID: {updatedItem.itemID}", updatedItem.itemID);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}