//ST10024620
//Sauraav Jayrajh
using System.ComponentModel.DataAnnotations;

namespace Sauraav_Prog_AgroEnergy.Models
{
    public class ItemModel
    {
        [Key]
        public int itemID { get; set; }
        public String FarmerUID { get; set; }
        public string itemName { get; set; }
        public double itemPrice { get; set; }
        public string itemDescription { get; set; }
        public string itemImageAsBase64 { get; set; }
        public string itemCategory { get; set; }

    }
}
