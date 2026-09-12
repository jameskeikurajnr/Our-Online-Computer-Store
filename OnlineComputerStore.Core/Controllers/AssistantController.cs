using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OnlineComputerStore.Core.Services;

namespace OnlineComputerStore.Core.Controllers
{
    [Route("assistant")]
    public class AssistantController : Controller
    {
        private readonly IAiAssistantService _ai;
        public AssistantController(IAiAssistantService ai) => _ai = ai;

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] AiMessage msg)
        {
            var reply = await _ai.GetReplyAsync(msg.Text);
            return Json(new { response = reply });
        }
    }

    public class AiMessage
    {
        public string Text { get; set; } = "";
    }
}
