using System.Collections.Generic;
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
            var reply = await _ai.GetReplyAsync(msg.Text, msg.History ?? new List<AssistantTurn>());
            return Json(new { response = reply.Text, products = reply.Products, flagged = reply.Flagged });
        }
    }

    public class AiMessage
    {
        public string Text { get; set; } = "";

        // The last few turns of this conversation, sent by the browser so the
        // assistant can follow up naturally ("what about in black?") instead
        // of answering every message cold. Capped server-side in
        // AiAssistantService, so a long-running chat can't balloon the prompt
        // sent to the model.
        public List<AssistantTurn> History { get; set; } = new();
    }
}
