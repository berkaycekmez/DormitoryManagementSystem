using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DormitoryManagementSystem.WEB.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Users()
        {
            return View();
        }

        public IActionResult Reports()
        {
            return View();
        }
    }
}