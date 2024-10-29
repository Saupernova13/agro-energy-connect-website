//ST10024620
//Sauraav Jayrajh
namespace Sauraav_Prog_AgroEnergy.Models
{
  public class FarmerModel : AccountModel
{
    public string FarmerUID { get; set; }
    public string FarmerName { get; set; }
    public string FarmerSurname { get; set; }
    public string FarmerProfileImageBase64 { get; set; }
    public string FarmerLocation { get; set; }
    public bool IsApproved { get; set; }
}
}
