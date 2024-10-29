//ST10024620
//Sauraav Jayrajh
using Microsoft.AspNetCore.Mvc;

namespace Sauraav_Prog_AgroEnergy.Models
{
    public class FarmerProfileViewModel
    {
        public FarmerModel Farmer { get; set; }
        public List<ItemModel> Items { get; set; }
    }
}