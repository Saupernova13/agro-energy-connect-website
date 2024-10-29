//ST10024620
//Sauraav Jayrajh
using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.AspNetCore.Mvc;
using Sauraav_Prog_AgroEnergy.Models;

namespace Sauraav_Prog_AgroEnergy.Controllers
{
    public class CartController : Controller
    {
        private readonly FirebaseClient _firebaseClient;
        private readonly ILogger<CartController> _logger;
        private string FirebaseUrl = "https://sauraavagroenergy-default-rtdb.firebaseio.com/";

        public CartController(ILogger<CartController> logger)
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
        public async Task<IActionResult> Index()
        {
            var farmerUID = HttpContext.Session.GetString("currentUser");
            if (string.IsNullOrEmpty(farmerUID))
            {
                return RedirectToAction("Login", "Authentication");
            }

            var firebaseClient = GetAuthenticatedClient();
            if (firebaseClient == null)
            {
                return Unauthorized();
            }

            var cart = await firebaseClient
                .Child("carts")
                .Child(farmerUID)
                .OnceSingleAsync<CartModel>();

            var cartItems = new List<ItemModel>();
            if (cart != null)
            {
                var items = await firebaseClient
                    .Child("items")
                    .OnceAsync<ItemModel>();

                var allItems = items.Select(i => i.Object).ToList();
                cartItems = allItems.Where(item => cart.ItemIDs.Contains(item.itemID)).ToList();
            }

            return View(cartItems);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] int itemId)
        {
            var farmerUID = HttpContext.Session.GetString("currentUser");
            if (string.IsNullOrEmpty(farmerUID))
            {
                _logger.LogWarning("User not logged in.");
                return RedirectToAction("Login", "Authentication");
            }

            var firebaseClient = GetAuthenticatedClient();
            if (firebaseClient == null)
            {
                _logger.LogWarning("User not authenticated.");
                return Unauthorized();
            }

            try
            {
                var items = await firebaseClient
                    .Child("items")
                    .OnceAsync<ItemModel>();

                var item = items.Select(i => i.Object).FirstOrDefault(i => i.itemID == itemId);

                if (item == null || item.FarmerUID == farmerUID)
                {
                    _logger.LogWarning("Item not found or user attempting to add own item. Item ID: {itemId}", itemId);
                    return Forbid();
                }

                var cart = await firebaseClient
                    .Child("carts")
                    .Child(farmerUID)
                    .OnceSingleAsync<CartModel>();

                if (cart == null)
                {
                    cart = new CartModel { FarmerUID = farmerUID };
                }

                if (!cart.ItemIDs.Contains(itemId))
                {
                    cart.ItemIDs.Add(itemId);
                }
                var cartItems = await firebaseClient
                    .Child("items")
                    .OnceAsync<ItemModel>();
                cart.UpdateTotalValue(cartItems.Select(i => i.Object));

                await firebaseClient
                    .Child("carts")
                    .Child(farmerUID)
                    .PutAsync(cart);

                _logger.LogInformation("Item added to cart. Item ID: {itemId}", itemId);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding item to cart. Item ID: {itemId}", itemId);
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart([FromBody] int itemId)
        {
            var farmerUID = HttpContext.Session.GetString("currentUser");
            if (string.IsNullOrEmpty(farmerUID))
            {
                return RedirectToAction("Login", "Authentication");
            }

            var firebaseClient = GetAuthenticatedClient();
            if (firebaseClient == null)
            {
                return Unauthorized();
            }

            try
            {
                var cart = await firebaseClient
                    .Child("carts")
                    .Child(farmerUID)
                    .OnceSingleAsync<CartModel>();

                if (cart == null)
                {
                    return NotFound("Cart not found");
                }

                if (cart.ItemIDs.Contains(itemId))
                {
                    cart.ItemIDs.Remove(itemId);
                }
                var cartItems = await firebaseClient
                    .Child("items")
                    .OnceAsync<ItemModel>();
                cart.UpdateTotalValue(cartItems.Select(i => i.Object));

                await firebaseClient
                    .Child("carts")
                    .Child(farmerUID)
                    .PutAsync(cart);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while removing item from cart. Item ID: {itemId}", itemId);
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            var farmerUID = HttpContext.Session.GetString("currentUser");
            if (string.IsNullOrEmpty(farmerUID))
            {
                return RedirectToAction("Login", "Authentication");
            }

            var firebaseClient = GetAuthenticatedClient();
            if (firebaseClient == null)
            {
                return Unauthorized();
            }

            try
            {
                await firebaseClient
                    .Child("carts")
                    .Child(farmerUID)
                    .DeleteAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking out cart.");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}