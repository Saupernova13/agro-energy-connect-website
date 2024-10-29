//ST10024620
//Sauraav Jayrajh
using System.Collections.Generic;
using System.Linq;

namespace Sauraav_Prog_AgroEnergy.Models
{
    public class CartModel
    {
        public string FarmerUID { get; set; }
        public List<int> ItemIDs { get; set; } = new List<int>();

        public double TotalValue { get; set; }

        public void UpdateTotalValue(IEnumerable<ItemModel> items)
        {
            TotalValue = items.Where(item => ItemIDs.Contains(item.itemID)).Sum(item => item.itemPrice);
        }
    }
}