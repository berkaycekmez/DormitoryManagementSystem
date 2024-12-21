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
                $"Yurt Ad�: {d.DormitoryName}, Adres: {d.Address}, Id: {d.DormitoryID}, Telefon: {d.Phone}, Kapasite: {d.DormitoryCapacity}, Mevcut Kapasite: {d.DormitoryCurrentCapacity}, Doluluk Oran�: {d.OccupancyRate}%"));

            string roomInfo = string.Join("; ", rooms.Select(r =>
                $"Oda No: {r.Number}, Kat: {r.Floor}, Id: {r.RoomID}, Kapasite: {r.Capacity}, Mevcut ��renci Say�s�: {r.CurrentStudentNumber}, Yurt: {r.Dormitory.DormitoryName}"));

            string studentInfo = string.Join("; ", students.Select(s =>
                $"��renci Ad�: {s.FirstName} {s.LastName}, Id: {s.StudentId}, Telefon: {s.Phone}, Oda No: {s.Room.Number}, Yurt: {s.Room.Dormitory.DormitoryName}"));

            request.UserMessage += $": NOT! Sen bir yapay zeka asistan�s�n ve yaln�zca veritaban�ndaki verilere dayanarak cevap vermekle y�k�ml�s�n. Ancak, verilen sorular� tekrar etme; direkt cevap ver. �imdi sana veritaban�ndaki verileri veriyorum. Bilgileri dikkate alarak sorular� yan�tla: " +
    $"Yurt bilgileri: {dormitoryInfo}. " +
    $"Yurtlar�n odalar� hakk�ndaki bilgiler: {roomInfo}. " +
    $"Ve son olarak yurtlar�n odalar�nda kalan ��rencilerin bilgileri: {studentInfo}. " +
    $"�NCEL�KLE UNUTMA, SEN�N B�R�NC� VAZ�FEN UPDATE VEYA DELETE ��LEM� YOKSA, H��B�R VER�N�N ID'S�N� K�ML���N� RESPONSE OLARAK VERME. " +
    $"AYRICA ASLA SORULARI CEVAPSIZ BIRAKMA; HEP B�R CEVABIN OLSUN, EN K�T� B�LM�YORSAN DA \"Bilmiyorum\" de. " +
    $"E�er ki sana spesifik olarak bir yurtla, odayla veya ��renciyle ilgili bir soru sorulursa, bu verileri tara ve en uygun �ekilde yan�tla. " +
    $"E�er ki sana sorulan soru veritaban�ndaki bilgilerden alakas�zsa veya yurt, oda ve ��renci ile ilgili de�ilse, yaln�zca yurtlar, odalar ve ��renciler hakk�nda konu�abilece�ini belirt. " +
    $"E�er gelen soru �ok genel veya tan�d�k bir selamla�ma gibi ise, kibarca \"Bu konuda yard�mc� olamam; yaln�zca sistemdeki yurtlar, odalar ve ��rencilerle ilgili sorular� yan�tlayabilirim.\" �eklinde yan�t ver. " +
    $"E�er kullan�c� senden delete veya update i�lemi isterse, �rne�in 'Berkay �ekmez olan Muhammed Fatih Safit�rk yurdundaki ��renciyi sil' derse veya '1. kat 1. oday� sil' derse ya da '�u isimli yurdu sil' derse, i�lem yap�lacak ��enin t�r�, i�lemin t�r� ve silinecek ��enin ID'sini parantez i�inde d�nd�r. �rne�in: \"Student - Update - (silinecek ��enin idsi)\". Neyi silmek istedi�ini anla (Student, Room ya da Dormitory olabilir), sonras�nda Update mi Delete mi oldu�unu belirle ve ard�ndan ID'yi parantez i�inde ver. Unutma, ID parantez i�inde olacak ve response �u �ekilde olmal�: \"Student - Update - (silinecek ��enin idsi)\". ��renci bilgisi d�nd�rme; sadece Student, Room veya Dormitory bilgilerini d�nd�r. Update veya Delete ile ilgili veri d�nerken ID'yi parantez i�inde vermeyi unutma. e�er silmek istedi�i �ey bir ��renci ise \"Student - Delete - (silinecek ��enin idsi)\" tarz�nda d�n e�er silmek iste�i yurt ise \"Dormitory - Update - (silinecek ��enin idsi)\" e�er bir oda ise \"Room - Update - (silinecek ��enin idsi)\" �eklinde";


            var model = _googleAI.GenerativeModel(Model.GeminiPro);
            var response = await model.GenerateContent(request.UserMessage);

            // Yan�t� kontrol et
            string responseText = FormatResponse(response.Text);

            // E�er yan�t "Update - (id)" veya "Delete - (id)" format�nda ise i�lemi yap
            if (responseText.StartsWith("Student -") || responseText.StartsWith("Dormitory -") || responseText.StartsWith("Room -"))
            {
                // ID'yi �ek
                string id = ExtractId(responseText);

                // ��lem t�r�n� belirle
                string operationType = responseText.StartsWith("Update -") ? "Update" : "Delete";

                if (operationType == "Delete") 
                {
                    if (responseText.StartsWith("Student -"))
                    {
                        Student student = context.Students.FirstOrDefault(x => x.StudentId == Guid.Parse(id));
                        context.Students.Remove(student);
                        context.SaveChanges();
                        return Json(new { response = $"�stemi� oldu�unuz {operationType} iste�i ba�ar�yla ger�ekle�tirilmi�tir." });
                    }
                    else if (responseText.StartsWith("Room -"))
                    {
                        Room room = context.Rooms.FirstOrDefault(x => x.RoomID == Guid.Parse(id));
                        context.Rooms.Remove(room);
                        context.SaveChanges();
                        return Json(new { response = $"�stemi� oldu�unuz {operationType} iste�i ba�ar�yla ger�ekle�tirilmi�tir." });
                    }
                    else
                    {
                        Dormitory dormitory = context.Dormitories.FirstOrDefault(x => x.DormitoryID == Guid.Parse(id));
                        context.Dormitories.Remove(dormitory);
                        context.SaveChanges();
                        return Json(new { response = $"�stemi� oldu�unuz {operationType} iste�i ba�ar�yla ger�ekle�tirilmi�tir." });
                    }                    
                }
                else
                {
                    return Json(new { response = $"�stemi� oldu�unuz {operationType} iste�i ba�ar�yla ger�ekle�tirilmemi�tir." });
                }

                
            }

            // Yan�t� JSON format�nda d�nd�r�n
            return Json(new { response = responseText });
        }

        // ID'yi ��kartmak i�in yard�mc� metot
        private string ExtractId(string input)
        {
            int startIndex = input.IndexOf('(') + 1; // A��lan parantezin i�indeki ilk karakterin konumu
            int endIndex = input.IndexOf(')'); // Kapanan parantezin konumu

            if (startIndex > 0 && endIndex > startIndex)
            {
                return input.Substring(startIndex, endIndex - startIndex); // Parantez i�indeki de�eri d�nd�r
            }

            return string.Empty; // E�er parantez yoksa bo� d�nd�r
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
