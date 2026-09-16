using System.Collections.Generic;
using System.Threading.Tasks;

namespace OnlineComputerStore.Core.Services
{
    public interface IAiAssistantService
    {
        Task<AssistantReply> GetReplyAsync(string message, IReadOnlyList<AssistantTurn> history);
    }

    // One prior turn of the conversation, as sent by the browser — just
    // enough for the assistant to follow up naturally ("what about in
    // black?") without re-explaining itself on every message.
    public class AssistantTurn
    {
        public string Role { get; set; } = "user"; // "user" or "assistant"
        public string Text { get; set; } = "";
    }

    // What the controller sends back to the browser: the assistant's reply
    // text (already cleaned of its internal [[product:ID]] / [[nomatch:...]]
    // tagging — see AiAssistantService) plus zero or more real catalog
    // products it referenced, each carrying a link built from the actual
    // database row rather than anything the AI wrote itself.
    public class AssistantReply
    {
        public string Text { get; set; } = "";
        public List<AssistantProduct> Products { get; set; } = new();

        // True when the assistant couldn't find a catalog match and logged
        // the request — lets the widget tell the customer their ask was
        // noted, instead of that happening silently server-side.
        public bool Flagged { get; set; }
    }

    public class AssistantProduct
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Brand { get; set; } = "";
        public string Category { get; set; } = "";
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = "";
        public int StockQuantity { get; set; }
        public string Url { get; set; } = "";
    }
}
