//ST10024620
//Sauraav Jayrajh
using System.ComponentModel.DataAnnotations;

namespace Sauraav_Prog_AgroEnergy.Models
{
    public class AccountModel
    {
        [Required]
        [EmailAddress]
        public string accountEmail { get; set; }
        [Required]
        public string accountPassword { get; set; }
        public DateTime accountRegistrationTime { get; set; }
    }
}
