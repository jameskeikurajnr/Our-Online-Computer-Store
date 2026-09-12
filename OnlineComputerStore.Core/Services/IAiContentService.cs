using System.Threading.Tasks;
using OnlineComputerStore.Core.Models;
namespace OnlineComputerStore.Core.Services
{
    public interface IAiContentService { Task<string> GenerateDescriptionAsync(Product product); }
}
