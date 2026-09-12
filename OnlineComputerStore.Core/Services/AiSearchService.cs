using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
namespace OnlineComputerStore.Core.Services
{
    public class AiSearchService : AiServiceBase, IAiSearchService
    {
        public AiSearchService(HttpClient http, IOptions<AiOptions> options) : base(http, options) { }
        public Task<string> InterpretQueryAsync(string query) =>
            AskAsync($"A shopper searched for: \"{query}\". In one short sentence, describe what they're likely looking for.");
    }
}
