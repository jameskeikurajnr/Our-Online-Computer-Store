namespace OnlineComputerStore.Core.Services
{
    public class AiOptions
    {
        public bool Enabled { get; set; } = false;
        public string Endpoint { get; set; } = "";
        public string ApiKey { get; set; } = "";
        public string Model { get; set; } = "";
    }
}
