//ST10024620
//Sauraav Jayrajh
using System.Collections.Generic;

namespace Sauraav_Prog_AgroEnergy.Models
{
    public class MarketplaceViewModel
    {
        public List<ItemModel> Items { get; set; }
        public List<string> Categories { get; set; }
        public string SelectedCategory { get; set; }
    }
}