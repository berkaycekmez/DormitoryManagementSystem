using DormitoryManagementSystem.WEB.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace DormitoryManagementSystem.WEB.Controllers
{
    public class HomeController : Controller
    {

        

        public IActionResult Index()
        {
            return View();
        }

       
    }
}
