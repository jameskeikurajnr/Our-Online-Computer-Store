using System.Threading.Tasks;
namespace OnlineComputerStore.Core.Services
{
    public interface IAiAssistantService { Task<string> GetReplyAsync(string message); }
}
