using System.Threading.Tasks;
using OnlineComputerStore.Core.Models;
namespace OnlineComputerStore.Core.Services
{
    public interface IAiComparisonService { Task<string> CompareAsync(Product a, Product b); }
}
