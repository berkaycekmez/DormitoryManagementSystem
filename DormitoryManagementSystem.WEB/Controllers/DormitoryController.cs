using DormitoryManagementSystem.DAL.Context;
using DormitoryManagementSystem.MODEL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DormitoryManagementSystem.WEB.Controllers
{
    [Authorize]

    public class DormitoryController : Controller
    {
        MyDbContext context;
        public DormitoryController(MyDbContext _context)
        {
            context = _context;
        }
        
        private List<string> GetImagesList()
        {
            try
            {
                var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var imagesPath = Path.Combine(webRootPath, "images");

                // Directory.GetFiles yerine DirectoryInfo kullanarak dosyaları alalım
                var directory = new DirectoryInfo(imagesPath);
                var imageFiles = directory.GetFiles()
                                        .Select(f => $"/images/{f.Name}")
                                        .ToList();
                return imageFiles;
            }
            catch (Exception)
            {
                // Hata durumunda boş liste dön
                return new List<string>();
            }
        }
        public IActionResult Index()
        {
            IEnumerable<Dormitory> dormitories = context.Dormitories
                .Include(y => y.Rooms)
                .Where(d => !d.statusDeletedDormitory) // Sadece silinmemiş yurtları al
                .ToList();

            return View(dormitories);
        }

        [Authorize(Roles = "Admin")] 
        public IActionResult Create()
        {
            ViewBag.Images = GetImagesList();

            return View();
        }
        [Authorize(Roles = "Admin")] 
        [HttpPost]
        public async Task<IActionResult> Create(Dormitory dormitory,int RoomCount)
        {
            if (ModelState.IsValid)
            {
                dormitory.DormitoryID = Guid.NewGuid();
                dormitory.DormitoryCurrentCapacity = 0;
                dormitory.OccupancyRate = 0;

                context.Dormitories.Add(dormitory);
                await context.SaveChangesAsync();
                for (int i = 1; i <= RoomCount; i++)
                {
                    var room = new Room
                    {
                        RoomID = Guid.NewGuid(),           // Benzersiz Room ID
                        Number = i,
                        Capacity = dormitory.DormitoryCapacity/RoomCount,                      // Varsayılan kapasite (her oda için 5)
                        CurrentCapacity = 0,               // Başlangıçta doluluk 0
                        Floor = (i - 1) / 10 + 1,          // Her 10 odada bir yeni kat (örnek kat hesaplama)
                        CurrentStudentNumber = 0,   // Oda numarası
                        DormitoryID = dormitory.DormitoryID // Yeni yurdun ID'si ile ilişkilendir
                    };

                    context.Rooms.Add(room); // Odayı veritabanına ekle
                }

                // Değişiklikleri kaydet
                await context.SaveChangesAsync();

                // İşlem tamamlandığında geri yönlendirme
                return RedirectToAction("Index");
            }

            return View(dormitory);
        }

        [Authorize(Roles = "Admin")] 
        [HttpGet]
        public IActionResult Delete(Guid id)
        {
            var dormitory = context.Dormitories
                .Include(d => d.Rooms)
                .FirstOrDefault(d => d.DormitoryID == id && !d.statusDeletedDormitory);

            if (dormitory == null)
            {
                return NotFound();
            }

            return View(dormitory);
        }
        [Authorize(Roles = "Admin")] 
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var dormitory = await context.Dormitories
                .Include(d => d.Rooms)
                .ThenInclude(r => r.Students) // Odalarla ilişkili öğrencileri de dahil et
                .FirstOrDefaultAsync(d => d.DormitoryID == id); // FirstOrDefaultAsync kullanımı

            if (dormitory != null)
            {
                // Yurt verisini silindi olarak işaretle
                dormitory.statusDeletedDormitory = true;

                // Odalarla ilişkili öğrencileri de güncelle
                foreach (var room in dormitory.Rooms)
                {
                    room.statusDeletedRoom = true; // Oda statüsünü 1 yapıyoruz
                    foreach (var student in room.Students)
                    {
                        student.statusDeletedStudent = true; // Öğrencinin durumunu 1 yapıyoruz
                    }
                }

                await context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")] 

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var dormitory = await context.Dormitories
                .Include(d => d.Rooms)
                .FirstOrDefaultAsync(d => d.DormitoryID == id);

            if (dormitory == null)
            {
                return NotFound();
            }

            ViewBag.Images = GetImagesList();
            return View(dormitory);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Edit(Guid id, Dormitory dormitory)
        {
            if (id != dormitory.DormitoryID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    context.Update(dormitory);
                    await context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!context.Dormitories.Any(d => d.DormitoryID == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(dormitory);
        }
    }
}
