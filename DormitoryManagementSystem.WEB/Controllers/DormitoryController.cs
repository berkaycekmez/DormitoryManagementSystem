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
            dormitories = context.Dormitories.Include(y => y.Rooms).ToList();
            return View(dormitories);
        }

        public IActionResult Create()
        {
            return View();
        }

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
        [HttpGet]
        public IActionResult Delete(Guid id)
        {
            // Silinecek yurdu veritabanından getir  
            var dormitory = context.Dormitories.Include(d => d.Rooms).FirstOrDefault(d => d.DormitoryID == id);

            if (dormitory == null)
            {
                return NotFound();
            }

            return View(dormitory); // Silme onayı için bir View gösterilebilir  
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            // Silinecek yurdu ve ilişkili odaları veritabanından getir  
            var dormitory = context.Dormitories.Include(d => d.Rooms).FirstOrDefault(d => d.DormitoryID == id);

            if (dormitory != null)
            {
                // İlgili odaları sil  
                context.Rooms.RemoveRange(dormitory.Rooms);

                // Yurdu sil  
                context.Dormitories.Remove(dormitory);

                // Değişiklikleri kaydet  
                await context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
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

            return View(dormitory);
        }

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
