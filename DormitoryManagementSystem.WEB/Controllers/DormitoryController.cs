using DormitoryManagementSystem.DAL.Context;
using DormitoryManagementSystem.MODEL;
using Microsoft.AspNetCore.Mvc;

namespace DormitoryManagementSystem.WEB.Controllers
{
    public class DormitoryController : Controller
    {
        MyDbContext context;
        public DormitoryController(MyDbContext _context)
        {
            context = _context;
        }
        public IActionResult Index()
        {
            IEnumerable<Dormitory> dormitories = new List<Dormitory>();
            dormitories = context.Dormitories.ToList();
            return View(dormitories);
        }
    }
}
