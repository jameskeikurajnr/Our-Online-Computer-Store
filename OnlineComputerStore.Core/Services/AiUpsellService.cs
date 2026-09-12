using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OnlineComputerStore.Core.Models;
namespace OnlineComputerStore.Core.Services
{
    public class AiUpsellService : AiServiceBase, IAiUpsellService
    {
        public AiUpsellService(HttpClient http, IOptions<AiOptions> options) : base(http, options) { }
        public Task<string> SuggestUpsellAsync(Product product) =>
            AskAsync($"Suggest 2-3 accessories or upgrades that pair well with: {product.Name} ({product.ShortSpecs}). Return as a short bullet list.");
    }
}
