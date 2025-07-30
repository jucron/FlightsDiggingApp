namespace FlightsDiggingApp.Models.Auth
{
    public class AuthRequestDTO
    {
        public string ClientId { get; set; }        
        public long Timestamp { get; set; }          
        public string Signature { get; set; }
    }
}
