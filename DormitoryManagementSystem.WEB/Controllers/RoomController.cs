using DormitoryManagementSystem.DAL.Context;
using DormitoryManagementSystem.MODEL;
using Microsoft.AspNetCore.Mvc;

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
            rooms = context.Rooms.ToList();
            return View(rooms);
        }
    }
}
