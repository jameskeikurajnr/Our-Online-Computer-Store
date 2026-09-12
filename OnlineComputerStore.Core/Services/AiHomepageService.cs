using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
namespace OnlineComputerStore.Core.Services
{
    public class AiHomepageService : AiServiceBase, IAiHomepageService
    {
        public AiHomepageService(HttpClient http, IOptions<AiOptions> options) : base(http, options) { }
        public Task<string> GetPersonaAsync(string history) =>
            AskAsync($"A shopper has viewed these products this session: {history}. Based on that, classify them in one word as 'student', 'professional', 'gamer', or 'casual'. If there is no meaningful browsing history, respond with 'casual'.");
    }
}