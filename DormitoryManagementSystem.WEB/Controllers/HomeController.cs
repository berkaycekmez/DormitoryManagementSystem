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
                return Json(new { response = "Mesaj boþ olamaz." });
            }

            var dormitories = context.Dormitories.ToList();
            var students = context.Students.Include(x => x.Room).ThenInclude(y => y.Dormitory).ToList();
            var rooms = context.Rooms.Include(x => x.Dormitory).ToList();

            string dormitoryInfo = string.Join("; ", dormitories.Select(d =>
                $"Yurt Ýsmi: {d.DormitoryName}, Adres: {d.Address}, Id: {d.DormitoryID}, Telefon: {d.Phone}, Kapasite: {d.DormitoryCapacity}, Mevcut Kapasite: {d.DormitoryCurrentCapacity}, Doluluk Oraný: {d.OccupancyRate}%"));

            string roomInfo = string.Join("; ", rooms.Select(r =>
                $"Oda No: {r.Number}, Kat: {r.Floor}, Id: {r.RoomID}, Kapasite: {r.Capacity}, Mevcut Öðrenci Sayýsý: {r.CurrentStudentNumber}, Yurt: {r.Dormitory.DormitoryName}"));

            string studentInfo = string.Join("; ", students.Select(s =>
                $"Öðrenci Ýsmi: {s.FirstName} {s.LastName}, Id: {s.StudentId}, Telefon: {s.Phone}, Oda No: {s.Room.Number}, Yurt: {s.Room.Dormitory.DormitoryName}"));

            request.UserMessage += $": NOT! Sen bir yapay zeka asistanýsýn ve yalnýzca veritabanýndaki verilere dayanarak cevap vermekle yükümlüsün. Ancak, verilen sorularý tekrar etme; direkt cevap ver. Þimdi sana veritabanýndaki verileri veriyorum. Bilgileri dikkate alarak sorularý yanýtla: " +
    $"Yurt bilgileri: {dormitoryInfo}. " +
    $"Yurtlarýn odalarý hakkýndaki bilgiler: {roomInfo}. " +
    $"Ve son olarak yurtlarýn odalarýnda kalan öðrencilerin bilgileri: {studentInfo}. " +
    $"Kullanýcýlarýn istediði bilgilere bu verilerden ulaþabiliyorsan düzgün bir þekilde açýklayarak anlat"+
    $"ÖNCELÝKLE UNUTMA, SENÝN BÝRÝNCÝ VAZÝFEN UPDATE VEYA DELETE ÝÞLEMÝ YOKSA, HÝÇBÝR VERÝNÝN ID'SÝNÝ KÝMLÝÐÝNÝ RESPONSE OLARAK VERME. " +
    $"AYRICA ASLA NULL RESPONSE DÖNME; HEP BÝR CEVABIN OLSUN, EN KÖTÜ BÝLMÝYORSAN DA \"Bilmiyorum\" de. " +
    $"Eðer ki kullanýcý senden delete - silme iþlemi isterse, örneðin 'Berkay Çekmez olan Muhammed Fatih Safitürk yurdundaki öðrenciyi sil' 'Ömer isimli öðrenciyi sil' derse veya '1. kat 1. odayý sil' derse ya da 'þu isimli yurdu sil' derse, lütfen önce veritabanýndaki verilere bak ve eþleþen veri olup olmadýðýný kontrol et. " +
    $"Eðer eþleþen veri yoksa, \"Silmek istediðiniz veri sistemde bulunmamaktadýr.\" þeklinde yanýt ver. " +
    $"Eðer silmek istediði þey verdiðim verilerde mevcut ise idsini response olarak dön ama response da sadece id si yazsýn";

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
                return Json(new { response = $"Ýstemiþ olduðunuz silme isteði baþarýyla gerçekleþtirilmiþtir." });
            }
            else if (roomIds.Contains(id))
            {
                Room room = context.Rooms.FirstOrDefault(x => x.RoomID == id);
                context.Rooms.Remove(room);
                context.SaveChanges();
                return Json(new { response = $"Ýstemiþ olduðunuz silme isteði baþarýyla gerçekleþtirilmiþtir." });
            }
            else if (dormitoryIds.Contains(id))
            {
                Dormitory dormitory = context.Dormitories.FirstOrDefault(x => x.DormitoryID == id);
                context.Dormitories.Remove(dormitory);
                context.SaveChanges();
                return Json(new { response = $"Ýstemiþ olduðunuz silme isteði baþarýyla gerçekleþtirilmiþtir." });
            }
            else
            {
                return Json(new { response = "Geçersiz ID: Bu ID sistemde bulunmamaktadýr." });
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
