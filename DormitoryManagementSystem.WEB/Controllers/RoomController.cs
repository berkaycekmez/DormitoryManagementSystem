using DormitoryManagementSystem.DAL.Context;
using DormitoryManagementSystem.MODEL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        public IActionResult Index(string search)
        {
            var rooms = context.Rooms
                .Include(x => x.Dormitory)
                .AsNoTracking()  // Performans için
                .ToList();

            if (!string.IsNullOrEmpty(search))
            {
                rooms = rooms.Where(r => r.Number.ToString().Contains(search)).ToList();
            }

            if (rooms == null)
            {
                return View(new List<Room>());  // 
            }

            return View(rooms);
        }
        public IActionResult Create()
        {
            ViewBag.Dormitories = new SelectList(context.Dormitories, "DormitoryID", "DormitoryName");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Room room, Guid DormitoryID)
        {
            if (!ModelState.IsValid)
            {
                room.RoomID = Guid.NewGuid();
                room.CurrentCapacity = 0;
                room.DormitoryID = DormitoryID;
                context.Rooms.Add(room);
                await context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));

            }
            return View(room);
        }
        public IActionResult Delete(Guid id)
        {
            var room = context.Rooms
                .Include(x => x.Dormitory)
                .FirstOrDefault(r => r.RoomID == id);

            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }

        // Silme işlemi onaylandıktan sonra oda silme
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var room = await context.Rooms
                .Include(x => x.Dormitory)
                .FirstOrDefaultAsync(r => r.RoomID == id);

            if (room == null)
            {
                return NotFound();
            }

            // Odayı sil
            context.Rooms.Remove(room);
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

    }
}
