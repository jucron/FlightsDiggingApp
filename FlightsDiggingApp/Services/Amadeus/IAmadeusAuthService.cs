namespace FlightsDiggingApp.Services.Amadeus
{
    public interface IAmadeusAuthService
    {
        void ClearToken();
        public string GetToken();
    }
}
