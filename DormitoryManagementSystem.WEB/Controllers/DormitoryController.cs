using DormitoryManagementSystem.DAL.Context;
using DormitoryManagementSystem.MODEL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            dormitories = context.Dormitories.Include(y=>y.Rooms).ToList();
            return View(dormitories);
        }
       public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Dormitory dormitory)
        {
            if (ModelState.IsValid)
            {
                dormitory.DormitoryID = Guid.NewGuid();
                dormitory.DormitoryCurrentCapacity = 0;
                dormitory.OccupancyRate = 0;

                context.Dormitories.Add(dormitory);
                await context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(dormitory);
        }
    }
}
