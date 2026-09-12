using System.Threading.Tasks;
using OnlineComputerStore.Core.Models;
namespace OnlineComputerStore.Core.Services
{
    public interface IAiOrderExplainService { Task<string> ExplainStatusAsync(Order order); }
}
