using DormitoryManagementSystem.DAL.Context;
using DormitoryManagementSystem.MODEL;
using DormitoryManagementSystem.WEB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace DormitoryManagementSystem.WEB.Controllers
{
    public class HomeController : Controller
    {
        public MyDbContext context;

        // GoogleAI servisini �imdilik kald�r�yoruz ��nk� Identity sistemini kurmak �nceli�imiz
        // private readonly GoogleAI _googleAI;

        // Constructor'� g�ncelliyoruz
        public HomeController(MyDbContext _context/*, GoogleAI googleAI*/)
        {
            context = _context;
            //_googleAI = googleAI;
        }

        public IActionResult Index()
        {
            IEnumerable<Dormitory> dormitories = new List<Dormitory>();
            dormitories = context.Dormitories.ToList();
            return View(dormitories);
        }

        // AI chat fonksiyonunu �imdilik yoruma al�yoruz
        /*
        [HttpPost]
        public async Task<IActionResult> Index([FromForm] MessageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserMessage))
            {
                return Json(new { response = "Mesaj bo� olamaz." });
            }

            var dormitories = context.Dormitories.ToList();
            var students = context.Students.Include(x => x.Room).ThenInclude(y => y.Dormitory).ToList();
            var rooms = context.Rooms.Include(x => x.Dormitory).ToList();

            string dormitoryInfo = string.Join("; ", dormitories.Select(d =>
                $"Yurt �smi: {d.DormitoryName}, Adres: {d.Address}, Id: {d.DormitoryID}, Telefon: {d.Phone}, Kapasite: {d.DormitoryCapacity}, Mevcut Kapasite: {d.DormitoryCurrentCapacity}, Doluluk Oran�: {d.OccupancyRate}%"));

            string roomInfo = string.Join("; ", rooms.Select(r =>
                $"Oda No: {r.Number}, Kat: {r.Floor}, Id: {r.RoomID}, Kapasite: {r.Capacity}, Mevcut ��renci Say�s�: {r.CurrentStudentNumber}, Yurt: {r.Dormitory.DormitoryName}"));

            string studentInfo = string.Join("; ", students.Select(s =>
                $"��renci �smi: {s.FirstName} {s.LastName}, Id: {s.StudentId}, Telefon: {s.Phone}, Oda No: {s.Room.Number}, Yurt: {s.Room.Dormitory.DormitoryName}"));

            // AI prompt ve i�lemleri...
            
            var model = _googleAI.GenerativeModel(Model.GeminiPro);
            var response = await model.GenerateContent(request.UserMessage);

            string responseText = FormatResponse(response.Text);

            if (!Guid.TryParse(response.Text, out Guid id))
            {
                return Json(new { response = responseText });
            }

            var studentIds = students.Select(s => s.StudentId).ToList();
            var roomIds = rooms.Select(r => r.RoomID).ToList();
            var dormitoryIds = dormitories.Select(d => d.DormitoryID).ToList();

            if (studentIds.Contains(id))
            {
                Student student = context.Students.FirstOrDefault(x => x.StudentId == id);   
                context.Students.Remove(student);
                context.SaveChanges();
                return Json(new { response = $"�stemi� oldu�unuz silme iste�i ba�ar�yla ger�ekle�tirilmi�tir." });
            }
            else if (roomIds.Contains(id))
            {
                Room room = context.Rooms.FirstOrDefault(x => x.RoomID == id);
                context.Rooms.Remove(room);
                context.SaveChanges();
                return Json(new { response = $"�stemi� oldu�unuz silme iste�i ba�ar�yla ger�ekle�tirilmi�tir." });
            }
            else if (dormitoryIds.Contains(id))
            {
                Dormitory dormitory = context.Dormitories.FirstOrDefault(x => x.DormitoryID == id);
                context.Dormitories.Remove(dormitory);
                context.SaveChanges();
                return Json(new { response = $"�stemi� oldu�unuz silme iste�i ba�ar�yla ger�ekle�tirilmi�tir." });
            }
            else
            {
                return Json(new { response = "Ge�ersiz ID: Bu ID sistemde bulunmamaktad�r." });
            }
        }

        private string FormatResponse(string? responseText)
        {
            var formattedText = responseText
                .Replace("**", "")
                .Replace("\n", "")
                .Replace("* ", "")
                .Insert(0, "")
                .Insert(responseText.Split('\n').Length, "");

            return formattedText;
        }
        */
    }

    // Bu s�n�f� �imdilik yoruma alabiliriz ��nk� AI �zelli�ini ge�ici olarak kald�rd�k
    /*
    public class MessageRequest
    {
        public string UserMessage { get; set; }
    }
    */
}