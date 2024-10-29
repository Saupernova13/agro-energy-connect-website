//ST10024620
//Sauraav Jayrajh
using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.AspNetCore.Mvc;
using Sauraav_Prog_AgroEnergy.Models;

public class AdminController : Controller
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

    public async Task<IActionResult> AdminDashboard(string filter = "all")
    {
        if (HttpContext.Session.GetString("userRole") != "admin")
        {
            return Unauthorized();
        }

        var firebaseClient = GetAuthenticatedClient();
        if (firebaseClient == null)
        {
            return Unauthorized();
        }

        var farmers = await firebaseClient
            .Child("users")
            .Child("farmers")
            .OnceAsync<FarmerModel>();

        List<FarmerModel> farmerList = farmers.Select(f => f.Object).ToList();

        if (filter == "authenticated")
        {
            farmerList = farmerList.Where(f => f.IsApproved).ToList();
        }
        else if (filter == "unauthenticated")
        {
            farmerList = farmerList.Where(f => !f.IsApproved).ToList();
        }
        return View(farmerList);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteFarmerAccount([FromBody] FarmerModel farmerModel)
    {
        if (HttpContext.Session.GetString("userRole") != "admin")
        {
            return Unauthorized();
        }

        if (farmerModel == null || string.IsNullOrEmpty(farmerModel.FarmerUID))
        {
            return BadRequest("Invalid farmer UID");
        }

        var firebaseClient = GetAuthenticatedClient();
        if (firebaseClient == null)
        {
            return Unauthorized();
        }

        await firebaseClient
            .Child("users")
            .Child("farmers")
            .Child(farmerModel.FarmerUID)
            .DeleteAsync();

        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> ApproveFarmer([FromBody] FarmerModel farmerModel)
    {
        if (HttpContext.Session.GetString("userRole") != "admin")
        {
            return Unauthorized();
        }

        if (farmerModel == null || string.IsNullOrEmpty(farmerModel.FarmerUID))
        {
            return BadRequest("Invalid farmer UID");
        }

        var firebaseClient = GetAuthenticatedClient();
        if (firebaseClient == null)
        {
            return Unauthorized();
        }

        var farmer = await firebaseClient
            .Child("users")
            .Child("farmers")
            .Child(farmerModel.FarmerUID)
            .OnceSingleAsync<FarmerModel>();

        if (farmer != null)
        {
            farmer.IsApproved = true;
            await firebaseClient
                .Child("users")
                .Child("farmers")
                .Child(farmerModel.FarmerUID)
                .PutAsync(farmer);

            return Ok();
        }

        return NotFound();
    }
}