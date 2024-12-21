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
    }
}
