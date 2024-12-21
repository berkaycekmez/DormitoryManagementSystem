using DormitoryManagementSystem.DAL.Context;
using DormitoryManagementSystem.MODEL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DormitoryManagementSystem.WEB.Controllers
{
    
    public class StudentController : Controller
    {
        private readonly MyDbContext _context;

        public StudentController(MyDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string search)
        {
            var query = _context.Students
                .Include(x => x.Room)
                .ThenInclude(y => y.Dormitory)
                .Where(s => !s.statusDeletedStudent) // Soft delete olmayanları getir
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(s =>
                    s.FirstName.Contains(search) ||
                    s.LastName.Contains(search) ||
                    s.Phone.Contains(search));
            }

            var students = await query.ToListAsync();
            return View(students);
        }


        public async Task<IActionResult> Create()
        {
            ViewBag.Dormitories = await _context.Dormitories.ToListAsync();
            return View();
        }

        [HttpGet("Student/GetAvailableRooms/{dormitoryId}")]
        public async Task<JsonResult> GetAvailableRooms([FromRoute] Guid dormitoryId)
        {
            try
            {
                var rooms = await _context.Rooms
                    .Where(r => r.DormitoryID == dormitoryId)
                    .Select(r => new
                    {
                        roomID = r.RoomID,
                        number = r.Number,
                        capacity = r.Capacity,
                        currentCapacity = r.CurrentCapacity,
                        floor = r.Floor
                    })
                    .ToListAsync();

                return Json(rooms);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Student student)
        {
            try
            {
                // Debug için gelen değerleri yazdır
                Console.WriteLine($"DormitoryId: {student.DormitoryId}");
                Console.WriteLine($"RoomId: {student.RoomId}");

                var room = await _context.Rooms
                    .Include(r => r.Dormitory)
                    .FirstOrDefaultAsync(r => r.RoomID == student.RoomId);

                if (room == null)
                {
                    ModelState.AddModelError("RoomId", "Seçilen oda bulunamadı.");
                    ViewBag.Dormitories = await _context.Dormitories.ToListAsync();
                    return View(student);
                }

                // Oda kapasitesi kontrolü
                if (room.CurrentCapacity >= room.Capacity)
                {
                    ModelState.AddModelError("RoomId", "Bu oda dolu.");
                    ViewBag.Dormitories = await _context.Dormitories.ToListAsync();
                    return View(student);
                }

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        student.StudentId = Guid.NewGuid();
                        student.CreatedAt = DateTime.Now;
                        student.UpdatedAt = DateTime.Now;
                        student.DormitoryId = room.DormitoryID;

                        room.CurrentCapacity++;
                        room.CurrentStudentNumber++;
                        room.Dormitory.DormitoryCurrentCapacity++;

                        _context.Students.Add(student);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("", "Öğrenci eklenirken bir hata oluştu: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "İşlem sırasında bir hata oluştu: " + ex.Message);
            }

            ViewBag.Dormitories = await _context.Dormitories.ToListAsync();
            return View(student);
        }



        public async Task<IActionResult> Edit(Guid id)
        {
            var student = await _context.Students
                .Include(s => s.Room)
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null)
            {
                return NotFound();
            }

            ViewBag.Dormitories = await _context.Dormitories.ToListAsync();
            ViewBag.Rooms = await _context.Rooms
                .Where(r => r.DormitoryID == student.DormitoryId)
                .ToListAsync();

            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Student student)
        {
            if (id != student.StudentId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var oldStudent = await _context.Students
                            .Include(s => s.Room)
                            .FirstOrDefaultAsync(s => s.StudentId == id);

                        if (oldStudent == null)
                        {
                            return NotFound();
                        }

                        // Eski odadan çıkar
                        var oldRoom = oldStudent.Room;
                        oldRoom.CurrentCapacity--;
                        oldRoom.CurrentStudentNumber--;
                        oldRoom.Dormitory.DormitoryCurrentCapacity--;

                        // Yeni odaya ekle
                        var newRoom = await _context.Rooms
                            .Include(r => r.Dormitory)
                            .FirstOrDefaultAsync(r => r.RoomID == student.RoomId);

                        if (newRoom.CurrentCapacity >= newRoom.Capacity)
                        {
                            ModelState.AddModelError("RoomId", "Seçilen oda dolu.");
                            return View(student);
                        }

                        newRoom.CurrentCapacity++;
                        newRoom.CurrentStudentNumber++;
                        newRoom.Dormitory.DormitoryCurrentCapacity++;

                        student.UpdatedAt = DateTime.Now;
                        student.DormitoryId = newRoom.DormitoryID;

                        _context.Entry(oldStudent).CurrentValues.SetValues(student);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("", "Öğrenci güncellenirken bir hata oluştu: " + ex.Message);
                    }
                }
            }

            ViewBag.Dormitories = await _context.Dormitories.ToListAsync();
            ViewBag.Rooms = await _context.Rooms
                .Where(r => r.DormitoryID == student.DormitoryId)
                .ToListAsync();

            return View(student);
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            var student = await _context.Students
                .Include(s => s.Room)
                .ThenInclude(r => r.Dormitory)
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var student = await _context.Students
                .Include(s => s.Room)
                .ThenInclude(r => r.Dormitory)
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null)
            {
                return NotFound();
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Öğrenciyi soft delete yap
                    student.statusDeletedStudent = true;
                    student.UpdatedAt = DateTime.Now;

                    // Oda ve yurt kapasitesini güncelle
                    if (student.Room != null)
                    {
                        student.Room.CurrentCapacity--;
                        student.Room.CurrentStudentNumber--;
                        if (student.Room.Dormitory != null)
                        {
                            student.Room.Dormitory.DormitoryCurrentCapacity--;
                        }
                    }

                    // Veritabanını güncelle
                    _context.Students.Update(student);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Silme işlemi sırasında bir hata oluştu: " + ex.Message);
                }
            }

            return RedirectToAction(nameof(Index));
        }



        public async Task<IActionResult> Details(Guid id)
        {
            var student = await _context.Students
                .Include(s => s.Room)
                .ThenInclude(r => r.Dormitory)
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }
    }
}