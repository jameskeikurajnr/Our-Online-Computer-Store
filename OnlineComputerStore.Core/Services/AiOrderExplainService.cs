using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OnlineComputerStore.Core.Models;
namespace OnlineComputerStore.Core.Services
{
    public class AiOrderExplainService : AiServiceBase, IAiOrderExplainService
    {
        public AiOrderExplainService(HttpClient http, IOptions<AiOptions> options) : base(http, options) { }
        public Task<string> ExplainStatusAsync(Order order) =>
            AskAsync($"Explain this order status to a customer in one friendly sentence: Order #{order.Id}, status \"{order.Status}\", placed {order.CreatedAt:d}.");
    }
}
