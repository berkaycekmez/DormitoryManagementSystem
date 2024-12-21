using DormitoryManagementSystem.DAL.Context;
using DormitoryManagementSystem.MODEL;
using DormitoryManagementSystem.WEB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Mscc.GenerativeAI;
using System.Diagnostics;

namespace DormitoryManagementSystem.WEB.Controllers
{
    public class HomeController : Controller
    {
        public MyDbContext context;
        private readonly GoogleAI _googleAI;

        public HomeController(MyDbContext _context, GoogleAI googleAI)
        {
            context = _context;
            _googleAI = googleAI;
        }
        public IActionResult Index()
        {
            IEnumerable<Dormitory> dormitories = new List<Dormitory>();
            dormitories = context.Dormitories.ToList();
            return View(dormitories);
        }


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

            request.UserMessage += $": NOT! Sen bir yapay zeka asistan�s�n ve yaln�zca veritaban�ndaki verilere dayanarak cevap vermekle y�k�ml�s�n. Ancak, verilen sorular� tekrar etme; direkt cevap ver. �imdi sana veritaban�ndaki verileri veriyorum. Bilgileri dikkate alarak sorular� yan�tla: " +
    $"Yurt bilgileri: {dormitoryInfo}. " +
    $"Yurtlar�n odalar� hakk�ndaki bilgiler: {roomInfo}. " +
    $"E�er gelen soru �ok genel veya tan�d�k bir selamla�ma gibi ise, kibarca \"Bu konuda yard�mc� olamam; yaln�zca sistemdeki yurtlar, odalar ve ��rencilerle ilgili sorular� yan�tlayabilirim.\" �eklinde yan�t ver."+
    $"Ve son olarak yurtlar�n odalar�nda kalan ��rencilerin bilgileri: {studentInfo}. " +
    $"Kullan�c�lar�n istedi�i bilgilere bu verilerden ula�abiliyorsan d�zg�n bir �ekilde a��klayarak anlat"+
    $"�NCEL�KLE UNUTMA, SEN�N B�R�NC� VAZ�FEN ORTADA DELETE ��LEM� YOKSA H��B�R VER�N�N ID'S�N� K�ML���N� RESPONSE OLARAK VERME. ama delete varsa sadece id vereceksin " +
    $"AYRICA ASLA NULL RESPONSE D�NME; HEP B�R CEVABIN OLSUN, EN K�T� B�LM�YORSAN DA \"Bilmiyorum\" de. " +
    $"E�er ki kullan�c� senden delete - silme i�lemi isterse, �rne�in 'Berkay �ekmez olan Muhammed Fatih Safit�rk yurdundaki ��renciyi sil' '�mer isimli ��renciyi sil' derse veya '1. kat 1. oday� sil' derse ya da '�u isimli yurdu sil' derse, l�tfen �nce veritaban�ndaki verilere bak ve e�le�en veri olup olmad���n� kontrol et. " +
    $"E�er e�le�en veri yoksa, \"Silmek istedi�iniz veri sistemde bulunmamaktad�r.\" �eklinde yan�t ver. " +
    $"E�er silmek istedi�i �ey verdi�im verilerde mevcut ise idsini response olarak d�n ama response da sadece id si yazs�n"+
    $"E�er gelen soru �ok genel veya tan�d�k bir selamla�ma gibi ise, kibarca \"Bu konuda yard�mc� olamam; yaln�zca sistemdeki yurtlar, odalar ve ��rencilerle ilgili sorular� yan�tlayabilirim.\" �eklinde yan�t ver.";

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
    }
    public class MessageRequest
    {
        public string UserMessage { get; set; }
    }
}
