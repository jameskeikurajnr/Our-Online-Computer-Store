using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OnlineComputerStore.Core.Models;
namespace OnlineComputerStore.Core.Services
{
    public class AiContentService : AiServiceBase, IAiContentService
    {
        public AiContentService(HttpClient http, IOptions<AiOptions> options) : base(http, options) { }
        public Task<string> GenerateDescriptionAsync(Product product) =>
            AskAsync($"Write a short, modern, engaging product description (2-3 sentences) for: {product.Name}, specs: {product.ShortSpecs}.");
    }
}
