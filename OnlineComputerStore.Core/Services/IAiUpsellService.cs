using System.Threading.Tasks;
using OnlineComputerStore.Core.Models;
namespace OnlineComputerStore.Core.Services
{
    public interface IAiUpsellService { Task<string> SuggestUpsellAsync(Product product); }
}
