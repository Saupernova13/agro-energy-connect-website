//ST10024620
//Sauraav Jayrajh
namespace Sauraav_Prog_AgroEnergy.Models
{
    public class FirebaseErrorModel
    {
        public Error error { get; set; }
    }

    public class Error
    {
        public int code { get; set; }
        public string message { get; set; }
        public List<Error> errors { get; set; }
    }
}
