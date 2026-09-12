using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OnlineComputerStore.Core.Helpers;
using OnlineComputerStore.Core.Models;
namespace OnlineComputerStore.Core.Services
{
    public class AiComparisonService : AiServiceBase, IAiComparisonService
    {
        public AiComparisonService(HttpClient http, IOptions<AiOptions> options) : base(http, options) { }
        public Task<string> CompareAsync(Product a, Product b) =>
            AskAsync($"Compare these two products for a shopper:\nA: {a.Name}, {a.ShortSpecs}, {a.Price.ToMoney()}\nB: {b.Name}, {b.ShortSpecs}, {b.Price.ToMoney()}\nGive a short pros/cons summary and say who each is best for.");
    }
}
