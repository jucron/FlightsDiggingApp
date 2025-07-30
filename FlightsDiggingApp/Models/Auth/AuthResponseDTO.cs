namespace FlightsDiggingApp.Models.Auth
{
    public class AuthResponseDTO
    {
        public string Token { get; set; }       
        public DateTime ExpiresAt { get; set; }     
    }
}
