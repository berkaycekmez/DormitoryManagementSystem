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

            request.UserMessage += $": NOT! Sen bir yapay zeka asistanýsýn ve yalnýzca veritabanýndaki verilere dayanarak cevap vermekle yükümlüsün. Ancak, verilen sorularý tekrar etme; direkt cevap ver. Þimdi sana veritabanýndaki verileri veriyorum. Bilgileri dikkate alarak sorularý yanýtla: " +
    $"Yurt bilgileri: {dormitoryInfo}. " +
    $"Yurtlarýn odalarý hakkýndaki bilgiler: {roomInfo}. " +
    $"Ve son olarak yurtlarýn odalarýnda kalan öðrencilerin bilgileri: {studentInfo}. " +
    $"ÖNCELÝKLE UNUTMA, SENÝN BÝRÝNCÝ VAZÝFEN UPDATE VEYA DELETE ÝÞLEMÝ YOKSA, HÝÇBÝR VERÝNÝN ID'SÝNÝ KÝMLÝÐÝNÝ RESPONSE OLARAK VERME. " +
    $"AYRICA ASLA SORULARI CEVAPSIZ BIRAKMA; HEP BÝR CEVABIN OLSUN, EN KÖTÜ BÝLMÝYORSAN DA \"Bilmiyorum\" de. " +
    $"Eðer ki sana spesifik olarak bir yurtla, odayla veya öðrenciyle ilgili bir soru sorulursa, bu verileri tara ve en uygun þekilde yanýtla. " +
    $"Eðer ki sana sorulan soru veritabanýndaki bilgilerden alakasýzsa veya yurt, oda ve öðrenci ile ilgili deðilse, yalnýzca yurtlar, odalar ve öðrenciler hakkýnda konuþabileceðini belirt. " +
    $"Eðer gelen soru çok genel veya tanýdýk bir selamlaþma gibi ise, kibarca \"Bu konuda yardýmcý olamam; yalnýzca sistemdeki yurtlar, odalar ve öðrencilerle ilgili sorularý yanýtlayabilirim.\" þeklinde yanýt ver. " +
    $"Eðer kullanýcý senden delete veya update iþlemi isterse, örneðin 'Berkay Çekmez olan Muhammed Fatih Safitürk yurdundaki öðrenciyi sil' derse veya '1. kat 1. odayý sil' derse ya da 'þu isimli yurdu sil' derse, iþlem yapýlacak öðenin türü, iþlemin türü ve silinecek öðenin ID'sini parantez içinde döndür. Örneðin: \"Student - Update - (silinecek öðenin idsi)\". Neyi silmek istediðini anla (Student, Room ya da Dormitory olabilir), sonrasýnda Update mi Delete mi olduðunu belirle ve ardýndan ID'yi parantez içinde ver. Unutma, ID parantez içinde olacak ve response þu þekilde olmalý: \"Student - Update - (silinecek öðenin idsi)\". Öðrenci bilgisi döndürme; sadece Student, Room veya Dormitory bilgilerini döndür. Update veya Delete ile ilgili veri dönerken ID'yi parantez içinde vermeyi unutma. eðer silmek istediði þey bir öðrenci ise \"Student - Delete - (silinecek öðenin idsi)\" tarzýnda dön eðer silmek isteði yurt ise \"Dormitory - Update - (silinecek öðenin idsi)\" eðer bir oda ise \"Room - Update - (silinecek öðenin idsi)\" þeklinde";


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
