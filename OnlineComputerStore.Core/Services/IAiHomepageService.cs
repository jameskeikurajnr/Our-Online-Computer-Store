using System.Threading.Tasks;
namespace OnlineComputerStore.Core.Services
{
    public interface IAiHomepageService { Task<string> GetPersonaAsync(string history); }
}
