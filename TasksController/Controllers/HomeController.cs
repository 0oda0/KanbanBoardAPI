using Microsoft.AspNetCore.Mvc;

namespace KanbanBoardAPI.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return Content("KanbanBoard API is running! Use /swagger for API documentation");
        }
    }
}