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
            dormitories = context.Dormitories.Where(x=>x.statusDeletedDormitory==false).ToList();
            foreach (var item in dormitories)
            {
                item.OccupancyRate = item.DormitoryCurrentCapacity*100/item.DormitoryCapacity;
            }
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

            request.UserMessage += $": NOT! Sen bir yapay zeka asistanısın ve yalnızca veritabanındaki verilere dayanarak cevap vermekle yükümlüsün. Ancak, verilen soruları tekrar etme; direkt cevap ver. Şimdi sana veritabanındaki verileri veriyorum. Bilgileri dikkate alarak soruları yanıtla: " +
$"Yurt bilgileri: {dormitoryInfo}. " +
$"Yurtların odaları hakkındaki bilgiler: {roomInfo}. " +
$"Ve son olarak yurtların odalarında kalan öğrencilerin bilgileri: {studentInfo}. " +
$"Kullanıcıların istediği bilgilere bu verilerden ulaşabiliyorsan düzgün bir şekilde açıklayarak anlat." +
$"Sana öğrenciler,yurtlar,odalar ve bunların ekleme,silme,güncelleme işlemleri ile ilgili sorulan ve söylenen her şey sistemle ilgili.Hepsine sistemden ve buprojeden bir cevap bulup yanıtlandırmak ve veri döndürmek zorundasın!" +
$"Eğer silmek istediği şey verdiğim verilerde mevcut ise o verinin sadece ve sadece id'sini response olarak dön ama response'da sadece id'si yazsın."+
$"ÖNCELİKLE UNUTMA, SENİN BİRİNCİ VAZİFEN ORTADA DELETE İŞLEMİ YOKSA HİÇBİR VERİNİN ID'SİNİ KİMLİĞİNİ RESPONSE OLARAK VERME. Ama delete varsa sadece id vereceksin." +
$"AYRICA ASLA NULL RESPONSE DÖNME; HEP BİR CEVABIN OLSUN, EN KÖTÜ BİLMİYORSAN DA \"Bilmiyorum\" de. " +
$"Eğer ki kullanıcı senden delete - silme işlemi isterse, örneğin 'Berkay Çekmez olan Muhammed Fatih Safitürk yurdundaki öğrenciyi sil' 'Ömer isimli öğrenciyi sil' derse veya '1. kat 1. odayı sil' derse ya da 'şu isimli yurdu sil' derse, lütfen önce veritabanındaki verilere bak ve eşleşen veri olup olmadığını kontrol et. " +
$"Eğer eşleşen veri yoksa, \"Silmek istediğiniz veri sistemde bulunmamaktadır.\" şeklinde yanıt ver. ";


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
                student.statusDeletedStudent = true;
                context.Update(student);
                context.SaveChanges();
                return Json(new { response = $"Ýstemiþ olduðunuz silme isteði baþarýyla gerçekleþtirilmiþtir." });
            }
            else if (roomIds.Contains(id))
            {
                Room room = context.Rooms.FirstOrDefault(x => x.RoomID == id);
                room.statusDeletedRoom = true;
                context.Update(room);
                context.SaveChanges();
                return Json(new { response = $"Ýstemiþ olduðunuz silme isteði baþarýyla gerçekleþtirilmiþtir." });
            }
            else if (dormitoryIds.Contains(id))
            {
                Dormitory dormitory = context.Dormitories.FirstOrDefault(x => x.DormitoryID == id);
                dormitory.statusDeletedDormitory = true;
                context.Update(dormitory);
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
