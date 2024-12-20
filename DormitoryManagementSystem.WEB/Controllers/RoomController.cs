using DormitoryManagementSystem.DAL.Context;
using DormitoryManagementSystem.MODEL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DormitoryManagementSystem.WEB.Controllers
{
    public class RoomController : Controller
    {
        MyDbContext context;
        public RoomController(MyDbContext _context)
        {
            context = _context;
        }
        public IActionResult Index()
        {
            IEnumerable<Room> rooms = new List<Room>();
            rooms = context.Rooms.Include(x=>x.Dormitory).ToList();
            return View(rooms);
        }
    }
}
