using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OnlineComputerStore.Core.Helpers;
using OnlineComputerStore.Core.Models;

namespace OnlineComputerStore.Core.Services
{
    // Unlike the other AI features (each answers one narrow question about
    // one product it's handed directly), the chat assistant needs to know
    // the *whole* catalog to be useful, remember the last few turns, and
    // point back to real product pages instead of just describing things in
    // prose. GetReplyAsync grounds every reply in the actual Products table
    // and resolves the model's own [[product:ID]] / [[nomatch:...]] tags
    // against it, so a hallucinated id or a made-up item never reaches the
    // customer as a broken link or a phantom recommendation.
    public class AiAssistantService : AiServiceBase, IAiAssistantService
    {
        private readonly IProductService _products;
        private readonly IProductRequestService _productRequests;

        private static readonly Regex ProductTagRegex = new(@"\[\[product:(\d+)\]\]", RegexOptions.Compiled);
        private static readonly Regex NoMatchTagRegex = new(@"\[\[nomatch:([^\]]{2,80})\]\]", RegexOptions.Compiled);

        public AiAssistantService(HttpClient http, IOptions<AiOptions> options, IProductService products, IProductRequestService productRequests)
            : base(http, options)
        {
            _products = products;
            _productRequests = productRequests;
        }

        public async Task<AssistantReply> GetReplyAsync(string message, IReadOnlyList<AssistantTurn> history)
        {
            var catalog = (await _products.GetAllAsync()).ToList();

            var systemPrompt =
                "You are the Store Assistant for an online computer store. You may ONLY talk about items in the CATALOG " +
                "below — never invent a product, price, spec, or stock level that isn't listed there. Prices are in AUD.\n\n" +
                "When you recommend or mention a specific catalog item by name, tag it immediately afterwards with " +
                "[[product:ID]] using its exact numeric id from the catalog, e.g. \"the ViewMax 27\\\" 4K Monitor [[product:12]]\". " +
                "Tag each distinct item at most once, right after the first time you name it.\n\n" +
                "If nothing in the catalog matches what the customer is asking for, say so plainly, suggest the closest real " +
                "catalog alternative if a reasonable one exists, and include the tag [[nomatch:their item, in a few words]] " +
                "exactly once, using the customer's own words for what they wanted.\n\n" +
                "Keep replies short and conversational — 2 to 4 sentences, no markdown headings or bullet lists.\n\n" +
                "CATALOG (id | name | brand | category | price | stock):\n" + BuildCatalogText(catalog);

            var turns = (history ?? Array.Empty<AssistantTurn>())
                .Where(t => !string.IsNullOrWhiteSpace(t.Text))
                .TakeLast(6)
                .Select(t => (Role: t.Role == "assistant" ? "assistant" : "user", Content: t.Text));

            var rawReply = await AskAsync(systemPrompt, turns, message);

            var (withoutProductTags, products) = ResolveProductTags(rawReply, catalog);
            var (finalText, missedTerm) = ResolveNoMatchTag(withoutProductTags);

            var flagged = false;
            if (!string.IsNullOrWhiteSpace(missedTerm))
            {
                // Feeds the exact same "Not on Website" tracker (and 2-strikes
                // admin email alert) that zero-result site searches already
                // trigger — so a customer asking the assistant for something
                // you don't stock shows up right alongside search-box misses,
                // not in a separate silo nobody checks.
                await _productRequests.LogAsync(missedTerm);
                flagged = true;
            }

            return new AssistantReply { Text = finalText, Products = products, Flagged = flagged };
        }

        private static string BuildCatalogText(List<Product> catalog)
        {
            var sb = new StringBuilder();
            foreach (var p in catalog)
            {
                sb.Append(p.Id).Append(" | ").Append(p.Name).Append(" | ").Append(p.Brand).Append(" | ")
                  .Append(p.Category).Append(" | ").Append(p.Price.ToMoney()).Append(" | ")
                  .Append(p.StockQuantity > 0 ? $"{p.StockQuantity} in stock" : "out of stock")
                  .Append('\n');
            }
            return sb.ToString();
        }

        // Strips every [[product:ID]] tag from the reply, looking each one up
        // against the catalog we just fetched (not trusting the id blindly)
        // and collecting the real product rows it resolves to. A tag whose id
        // doesn't exist — a hallucination — is simply dropped; the sentence
        // around it still reads fine since the tag always comes right after
        // the product's name in prose, never mid-sentence.
        private static (string Text, List<AssistantProduct> Products) ResolveProductTags(string reply, List<Product> catalog)
        {
            var seen = new HashSet<int>();
            var products = new List<AssistantProduct>();

            var text = ProductTagRegex.Replace(reply, match =>
            {
                if (!int.TryParse(match.Groups[1].Value, out var id)) return "";

                var product = catalog.FirstOrDefault(p => p.Id == id);
                if (product == null) return "";

                if (seen.Add(id))
                {
                    products.Add(new AssistantProduct
                    {
                        Id = product.Id,
                        Name = product.Name,
                        Brand = product.Brand,
                        Category = product.Category,
                        Price = product.Price,
                        ImageUrl = product.ImageUrl,
                        StockQuantity = product.StockQuantity,
                        Url = $"/Shop/Details/{product.Id}"
                    });
                }

                return ""; // the card rendered below the bubble carries the name/price/link — no need to repeat it inline
            });

            return (CollapseWhitespace(text), products);
        }

        private static (string Text, string? MissedTerm) ResolveNoMatchTag(string reply)
        {
            var match = NoMatchTagRegex.Match(reply);
            if (!match.Success) return (reply, null);

            var term = match.Groups[1].Value.Trim();
            var text = CollapseWhitespace(NoMatchTagRegex.Replace(reply, "", 1));
            return (text, term);
        }

        // Tag removal can leave a doubled space, or a space before punctuation
        // ("Monitor ." instead of "Monitor.") — tidy those up rather than
        // shipping visibly stitched-together text.
        private static string CollapseWhitespace(string text) =>
            Regex.Replace(text, @"[ \t]{2,}", " ")
                 .Replace(" .", ".")
                 .Replace(" ,", ",")
                 .Trim();
    }
}
