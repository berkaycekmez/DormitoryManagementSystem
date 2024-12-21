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
                .Where(r => !r.statusDeletedRoom)  // Soft delete işlemi
                .Include(x => x.Dormitory)
                .AsNoTracking()
                .ToList();

            if (!string.IsNullOrEmpty(search))
            {
                rooms = rooms.Where(r => r.Number.ToString().Contains(search)).ToList();
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

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var room = await context.Rooms
                .Include(r => r.Students)
                .FirstOrDefaultAsync(r => r.RoomID == id);

            if (room == null)
            {
                return NotFound();
            }

            // Odayı ve öğrencilerini soft delete yap
            room.statusDeletedRoom = true; // Odayı silindi olarak işaretle
            foreach (var student in room.Students)
            {
                student.statusDeletedStudent = true; // Öğrencileri de silindi olarak işaretle
            }

            await context.SaveChangesAsync(); // Değişiklikleri veritabanına kaydet
            return RedirectToAction(nameof(Index));
        }




    }
}
