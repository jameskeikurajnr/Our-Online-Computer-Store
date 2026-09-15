using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OnlineComputerStore.Core.Services;

namespace OnlineComputerStore.Core.Controllers
{
    public class SearchController : Controller
    {
        private readonly IAiSearchService _ai;
        private readonly IProductService _products;
        private readonly IProductRequestService _productRequests;

        public SearchController(IAiSearchService ai, IProductService products, IProductRequestService productRequests)
        {
            _ai = ai;
            _products = products;
            _productRequests = productRequests;
        }

        public async Task<IActionResult> Index(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return RedirectToAction("Index", "Shop");

            ViewBag.Query = q;
            ViewBag.AiInterpretation = await _ai.InterpretQueryAsync(q);

            var results = (await _products.SearchAsync(q)).ToList();

            // The site has nothing matching this search — log it so an admin can
            // see what customers are actually looking for and go source it.
            if (results.Count == 0)
                await _productRequests.LogAsync(q);

            return View(results);
        }
    }
}
