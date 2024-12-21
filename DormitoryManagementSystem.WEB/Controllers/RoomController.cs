using DormitoryManagementSystem.DAL.Context;
using DormitoryManagementSystem.MODEL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DormitoryManagementSystem.WEB.Controllers
{
    [Authorize]

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
                .Where(r => !r.statusDeletedRoom)  
                .Include(x => x.Dormitory)
                .AsNoTracking()
                .ToList();

            if (!string.IsNullOrEmpty(search))
            {
                rooms = rooms.Where(r => r.Number.ToString().Contains(search)).ToList();
            }

            return View(rooms);
        }
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewBag.Dormitories = new SelectList(
                context.Dormitories.Where(d => !d.statusDeletedDormitory),
                "DormitoryID",
                "DormitoryName"
            );

            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(Room room, Guid DormitoryID)
        {
            if (!ModelState.IsValid)
            {
                room.RoomID = Guid.NewGuid();
                room.CurrentCapacity = 0;  
                room.DormitoryID = DormitoryID;

                var dormitory = await context.Dormitories
                    .FirstOrDefaultAsync(d => d.DormitoryID == DormitoryID);

                if (dormitory != null)
                {
                    dormitory.DormitoryCapacity += room.Capacity; 
                    context.Dormitories.Update(dormitory);
                }

                context.Rooms.Add(room); 
                await context.SaveChangesAsync(); 

                return RedirectToAction(nameof(Index));
            }

            
            ViewBag.Dormitories = new SelectList(
                context.Dormitories.Where(d => !d.statusDeletedDormitory),
                "DormitoryID",
                "DormitoryName"
            );

            return View(room);
        }


        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var room = await context.Rooms
                .Include(r => r.Students)
                .FirstOrDefaultAsync(r => r.RoomID == id);

            if (room == null)
            {
                return NotFound();
            }

            
            room.statusDeletedRoom = true; 
            foreach (var student in room.Students)
            {
                student.statusDeletedStudent = true; 
            }

            await context.SaveChangesAsync(); 
            return RedirectToAction(nameof(Index));
        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var room = await context.Rooms
                .Include(r => r.Dormitory)
                .Include(r => r.Students)
                .FirstOrDefaultAsync(r => r.RoomID == id);

            if (room == null)
            {
                return NotFound();
            }

            ViewBag.Dormitories = new SelectList(context.Dormitories.Where(d => !d.statusDeletedDormitory),
                "DormitoryID", "DormitoryName", room.DormitoryID);

            return View(room);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(Guid id, Room room)
        {
            if (id != room.RoomID)
            {
                return NotFound();
            }

            var existingRoom = await context.Rooms
                .Include(r => r.Dormitory)
                .Include(r => r.Students)
                .FirstOrDefaultAsync(r => r.RoomID == id);

            if (existingRoom == null)
            {
                return NotFound();
            }

            if (room.Capacity < existingRoom.CurrentCapacity)
            {
                ModelState.AddModelError("Capacity",
                    $"Kapasite mevcut öğrenci sayısından ({existingRoom.CurrentCapacity}) az olamaz!");
                ViewBag.Dormitories = new SelectList(context.Dormitories.Where(d => !d.statusDeletedDormitory),
                    "DormitoryID", "DormitoryName", room.DormitoryID);
                return View(room);
            }

            try
            {
                
                room.CurrentCapacity = existingRoom.CurrentCapacity;
                room.CurrentStudentNumber = existingRoom.CurrentStudentNumber;

               
                if (existingRoom.DormitoryID != room.DormitoryID)
                {
                    var newDormitory = await context.Dormitories
                        .FirstOrDefaultAsync(d => d.DormitoryID == room.DormitoryID);

                    if (newDormitory == null)
                    {
                        ModelState.AddModelError("DormitoryID", "Seçilen yurt bulunamadı.");
                        ViewBag.Dormitories = new SelectList(context.Dormitories.Where(d => !d.statusDeletedDormitory),
                            "DormitoryID", "DormitoryName", room.DormitoryID);
                        return View(room);
                    }
                }

               
                if (existingRoom.Capacity != room.Capacity)
                {
                    var dormitory = await context.Dormitories
                        .FirstOrDefaultAsync(d => d.DormitoryID == room.DormitoryID);

                    if (dormitory != null)
                    {
                        dormitory.DormitoryCapacity += room.Capacity - existingRoom.Capacity; 
                        context.Dormitories.Update(dormitory);
                    }
                }

                context.Entry(existingRoom).CurrentValues.SetValues(room);
                await context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await context.Rooms.AnyAsync(r => r.RoomID == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

    }
}
