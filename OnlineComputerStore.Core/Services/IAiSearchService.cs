using System.Threading.Tasks;
namespace OnlineComputerStore.Core.Services
{
    public interface IAiSearchService { Task<string> InterpretQueryAsync(string query); }
}
