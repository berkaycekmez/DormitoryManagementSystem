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
                $"Yurt Adý: {d.DormitoryName}, Adres: {d.Address}, Id: {d.DormitoryID}, Telefon: {d.Phone}, Kapasite: {d.DormitoryCapacity}, Mevcut Kapasite: {d.DormitoryCurrentCapacity}, Doluluk Oraný: {d.OccupancyRate}%"));

            string roomInfo = string.Join("; ", rooms.Select(r =>
                $"Oda No: {r.Number}, Kat: {r.Floor}, Id: {r.RoomID}, Kapasite: {r.Capacity}, Mevcut Öðrenci Sayýsý: {r.CurrentStudentNumber}, Yurt: {r.Dormitory.DormitoryName}"));

            string studentInfo = string.Join("; ", students.Select(s =>
                $"Öðrenci Adý: {s.FirstName} {s.LastName}, Id: {s.StudentId}, Telefon: {s.Phone}, Oda No: {s.Room.Number}, Yurt: {s.Room.Dormitory.DormitoryName}"));

            request.UserMessage += $": NOT! Sen bir yapay zeka asistanýsýn ve yalnýzca veritabanýndaki verilere dayanarak cevap vermekle yükümlüsün. Þimdi sana veritabanýndaki verileri veriyorum. Bilgileri dikkate alarak sorularý yanýtla: " +
                $"Yurt bilgileri: {dormitoryInfo}. " +
                $"Yurtlarýn odalarý hakkýndaki bilgiler: {roomInfo}. " +
                $"Ve son olarak yurtlarýn odalarýnda kalan öðrencilerin bilgileri: {studentInfo}. " +
                $"ÖNCELÝKLE UNUTMA SENÝN BÝRÝNCÝ VAZÝFEN UPDATE VEYA DELETE ÝÞLEMÝ YOKSA SAKIN HÝÇBÝR VERÝNÝN IDSÝNÝ KÝMLÝÐÝNÝ RESPONSE OLARAK VERME"+
                $"Eðer ki sana spesifik olarak bir yurtla, odayla veya öðrenciyle ilgili bir soru sorulursa bu verileri tara ve cevaplarý açýk bir þekilde ver. " +
                $"Eðer ki sana sorulan soru veritabanýndaki bilgilerden alakasýz ise, yalnýzca yurtlar, odalar ve öðrenciler ile ilgili bilgiler hakkýnda konuþabileceðini söyle. " +
                $"Eðer gelen soru çok genel veya tanýdýk bir selamlaþma gibi ise, kibarca \"Bu konuda yardýmcý olamam; yalnýzca sistemdeki yurtlar, odalar ve öðrencilerle ilgili sorularý yanýtlayabilirim.\" þeklinde yanýt ver." +
                $"Bir de senden son bir isteðim var; eðer ki kullanýcý senden delete veya update iþlemi isterse mesela Ýsmi Berkay Çekmez olan Muhammed Fatih Safitürk yurdundaki öðrenciyi sil derse veya Muhammed Fatih Safitürk yurdundaki 1. kat 1. odayý sil derse ya da direkt þu isimli yurdu sil derse bana cevap olarak iþlem yapýlacak öðenin türü, iþlemin türü ve silinecek öðenin id'sini ver ama sadece update veya delete iþlemi varsa id vereceksin diðer durumlarda idyi response verme mesela þu studnetin bilgilerini ver veya room dormitory fark etmez o durumlarda id hariç diðer bilgileri response ver. Örnek veriyorum silme iþlemiyse cevabý þu formatta ver 'Student - Update - (silinecek öðenin idsi)' zaten verebileceðin formatlar belli neyi silmek istediðini anla Student Room ya da Dormitory olabilir sonrasýnda Update mi Delete mi sonrasýnda da id yi ver parantez içinde, unutma id parantez içinde olucak response de þu þekilde 'Student - Update - (silinecek öðenin idsi)'.";

            var model = _googleAI.GenerativeModel(Model.GeminiPro);
            var response = await model.GenerateContent(request.UserMessage);

            // Yanýtý kontrol et
            string responseText = FormatResponse(response.Text);

            // Eðer yanýt "Update - (id)" veya "Delete - (id)" formatýnda ise iþlemi yap
            if (responseText.StartsWith("Student -") || responseText.StartsWith("Dormitory -") || responseText.StartsWith("Room -"))
            {
                // ID'yi çek
                string id = ExtractId(responseText);

                // Ýþlem türünü belirle
                string operationType = responseText.StartsWith("Update -") ? "Update" : "Delete";

                if (operationType == "Delete") 
                {
                    if (responseText.StartsWith("Student -"))
                    {
                        Student student = context.Students.FirstOrDefault(x => x.StudentId == Guid.Parse(id));
                        context.Students.Remove(student);
                        context.SaveChanges();
                        return Json(new { response = $"Ýstemiþ olduðunuz {operationType} isteði baþarýyla gerçekleþtirilmiþtir." });
                    }
                    else if (responseText.StartsWith("Room -"))
                    {
                        Room room = context.Rooms.FirstOrDefault(x => x.RoomID == Guid.Parse(id));
                        context.Rooms.Remove(room);
                        context.SaveChanges();
                        return Json(new { response = $"Ýstemiþ olduðunuz {operationType} isteði baþarýyla gerçekleþtirilmiþtir." });
                    }
                    else
                    {
                        Dormitory dormitory = context.Dormitories.FirstOrDefault(x => x.DormitoryID == Guid.Parse(id));
                        context.Dormitories.Remove(dormitory);
                        context.SaveChanges();
                        return Json(new { response = $"Ýstemiþ olduðunuz {operationType} isteði baþarýyla gerçekleþtirilmiþtir." });
                    }                    
                }
                else
                {
                    return Json(new { response = $"Ýstemiþ olduðunuz {operationType} isteði baþarýyla gerçekleþtirilmemiþtir." });
                }

                
            }

            // Yanýtý JSON formatýnda döndürün
            return Json(new { response = responseText });
        }

        // ID'yi çýkartmak için yardýmcý metot
        private string ExtractId(string input)
        {
            int startIndex = input.IndexOf('(') + 1; // Açýlan parantezin içindeki ilk karakterin konumu
            int endIndex = input.IndexOf(')'); // Kapanan parantezin konumu

            if (startIndex > 0 && endIndex > startIndex)
            {
                return input.Substring(startIndex, endIndex - startIndex); // Parantez içindeki deðeri döndür
            }

            return string.Empty; // Eðer parantez yoksa boþ döndür
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
