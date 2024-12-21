using DormitoryManagementSystem.DAL.Context;
using DormitoryManagementSystem.MODEL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DormitoryManagementSystem.WEB.Controllers
{
    [Authorize(Roles = "Admin")]

    public class StudentController : Controller
    {
        private readonly MyDbContext _context;

        public StudentController(MyDbContext context)
        {
            _context = context;
        }
        

        private List<string> GetImagesList()
        {
            try
            {
                var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var imagesPath = Path.Combine(webRootPath, "images");

                // Klasör var mı kontrol et
                if (!Directory.Exists(imagesPath))
                {
                    return new List<string>();
                }

                var directory = new DirectoryInfo(imagesPath);
                if (!directory.Exists)
                {
                    return new List<string>();
                }

                var imageFiles = directory.GetFiles()
                                        .Where(f => f.Extension.ToLower() is ".jpg" or ".jpeg" or ".png" or ".gif")
                                        .Select(f => $"/images/{f.Name}")
                                        .ToList();

                return imageFiles;
            }
            catch (Exception ex)
            {
                // Hata loglanabilir
                return new List<string>();
            }
        }
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
        [HttpGet]
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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            var imagesList = GetImagesList();
            if (imagesList == null)
            {
                imagesList = new List<string>(); // Null ise boş liste oluştur
            }

            // ViewBag'e atamaları yapalım
            ViewBag.Images = imagesList;
            ViewBag.Dormitories = await _context.Dormitories.ToListAsync();
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Student student)
        {
            try
            {
                var room = await _context.Rooms
                    .Include(r => r.Dormitory)
                    .FirstOrDefaultAsync(r => r.RoomID == student.RoomId);

                if (room == null)
                {
                    ModelState.AddModelError("RoomId", "Seçilen oda bulunamadı.");
                    ViewBag.Dormitories = await _context.Dormitories.ToListAsync();
                    return View(student);
                }

                // Oda durumu kontrolü
                if (room.statusDeletedRoom)
                {
                    ModelState.AddModelError("RoomId", "Seçilen oda aktif değil.");
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






        [Authorize(Roles = "Admin")]

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var student = await _context.Students
                .Include(s => s.Room)
                .ThenInclude(r => r.Dormitory)
                .FirstOrDefaultAsync(s => s.StudentId == id && !s.statusDeletedStudent);

            if (student == null)
            {
                return NotFound();
            }

            // Sadece aktif yurtları getir
            ViewBag.Dormitories = await _context.Dormitories
                .Where(d => !d.statusDeletedDormitory)
                .ToListAsync();

            // Seçili yurdun aktif odaları
            ViewBag.Rooms = await _context.Rooms
                .Where(r => r.DormitoryID == student.DormitoryId && !r.statusDeletedRoom)
                .ToListAsync();

            return View(student);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Student student)
        {
            // ID kontrolü
            if (id != student.StudentId)
            {
                return NotFound();
            }

            try
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    var oldStudent = await _context.Students
                        .Include(s => s.Room)
                        .ThenInclude(r => r.Dormitory)
                        .FirstOrDefaultAsync(s => s.StudentId == id);

                    if (oldStudent == null)
                    {
                        return NotFound();
                    }

                    // Önceki değerleri koruyalım
                    student.CreatedAt = oldStudent.CreatedAt;
                    student.UpdatedAt = DateTime.Now;

                    if (oldStudent.RoomId != student.RoomId)
                    {
                        // Eski oda bilgileri
                        var oldRoom = await _context.Rooms
                            .Include(r => r.Dormitory)
                            .FirstOrDefaultAsync(r => r.RoomID == oldStudent.RoomId);

                        if (oldRoom != null)
                        {
                            oldRoom.CurrentCapacity--;
                            oldRoom.CurrentStudentNumber--;
                            oldRoom.Dormitory.DormitoryCurrentCapacity--;
                            _context.Update(oldRoom);
                        }

                        // Yeni oda bilgileri
                        var newRoom = await _context.Rooms
                            .Include(r => r.Dormitory)
                            .FirstOrDefaultAsync(r => r.RoomID == student.RoomId);

                        if (newRoom == null)
                        {
                            ModelState.AddModelError("RoomId", "Seçilen oda bulunamadı.");
                            await transaction.RollbackAsync();

                            // ViewBag'leri doldur
                            ViewBag.Dormitories = await _context.Dormitories
                                .Where(d => !d.statusDeletedDormitory)
                                .ToListAsync();
                            ViewBag.Rooms = await _context.Rooms
                                .Where(r => r.DormitoryID == student.DormitoryId && !r.statusDeletedRoom)
                                .ToListAsync();

                            return View(student);
                        }

                        if (newRoom.CurrentCapacity >= newRoom.Capacity)
                        {
                            ModelState.AddModelError("RoomId", "Seçilen oda dolu.");
                            await transaction.RollbackAsync();

                            // ViewBag'leri doldur
                            ViewBag.Dormitories = await _context.Dormitories
                                .Where(d => !d.statusDeletedDormitory)
                                .ToListAsync();
                            ViewBag.Rooms = await _context.Rooms
                                .Where(r => r.DormitoryID == student.DormitoryId && !r.statusDeletedRoom)
                                .ToListAsync();

                            return View(student);
                        }

                        newRoom.CurrentCapacity++;
                        newRoom.CurrentStudentNumber++;
                        newRoom.Dormitory.DormitoryCurrentCapacity++;
                        student.DormitoryId = newRoom.DormitoryID;
                        _context.Update(newRoom);
                    }
                    else
                    {
                        // Oda değişmiyorsa eski yurt ID'sini koru
                        student.DormitoryId = oldStudent.DormitoryId;
                    }

                    // Öğrenciyi güncelle
                    _context.Entry(oldStudent).CurrentValues.SetValues(student);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Güncelleme sırasında bir hata oluştu: " + ex.Message);
            }

            // Hata durumunda ViewBag'leri doldur
            ViewBag.Dormitories = await _context.Dormitories
                .Where(d => !d.statusDeletedDormitory)
                .ToListAsync();
            ViewBag.Rooms = await _context.Rooms
                .Where(r => r.DormitoryID == student.DormitoryId && !r.statusDeletedRoom)
                .ToListAsync();

            return View(student);
        }
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
                    student.statusDeletedStudent = false;
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

                    TempData["Message"] = $"{student.FirstName} {student.LastName} başarıyla pasifleştirildi.";
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