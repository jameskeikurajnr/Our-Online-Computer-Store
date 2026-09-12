using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
namespace OnlineComputerStore.Core.Services
{
    public class AiAssistantService : AiServiceBase, IAiAssistantService
    {
        public AiAssistantService(HttpClient http, IOptions<AiOptions> options) : base(http, options) { }
        public Task<string> GetReplyAsync(string message) =>
            AskAsync($"You are a friendly assistant for an online computer store. A customer asked: \"{message}\". Reply helpfully in 2-3 sentences.");
    }
}
