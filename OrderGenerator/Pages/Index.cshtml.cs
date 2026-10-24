using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OrderGenerator.Pages
{
    public class IndexModel : PageModel
    {
        public string OrderApiUrl { get; private set; } = "/api/orders";

        public void OnGet()
        {
            OrderApiUrl = HttpContext.RequestServices.GetService<IConfiguration>()?["OrderApi:Url"] ?? "/api/orders";
        }
    }
}
