using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OnlineComputerStore.Core.Services;

namespace OnlineComputerStore.Core.Controllers
{
    public class CompareController : Controller
    {
        private readonly IProductService _products;
        private readonly IAiComparisonService _ai;

        public CompareController(IProductService products, IAiComparisonService ai)
        {
            _products = products;
            _ai = ai;
        }

        public async Task<IActionResult> Index(int aId, int bId)
        {
            var a = await _products.GetByIdAsync(aId);
            var b = await _products.GetByIdAsync(bId);
            if (a == null || b == null) return NotFound();

            ViewBag.Summary = await _ai.CompareAsync(a, b);
            return View((a, b));
        }
    }
}
