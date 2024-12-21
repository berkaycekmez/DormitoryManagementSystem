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

            request.UserMessage += $": NOT! Sen bir yapay zeka asistan�s�n ve yaln�zca veritaban�ndaki verilere dayanarak cevap vermekle y�k�ml�s�n. �imdi sana veritaban�ndaki verileri veriyorum. Bilgileri dikkate alarak sorular� yan�tla: " +
                $"Yurt bilgileri: {dormitoryInfo}. " +
                $"Yurtlar�n odalar� hakk�ndaki bilgiler: {roomInfo}. " +
                $"Ve son olarak yurtlar�n odalar�nda kalan ��rencilerin bilgileri: {studentInfo}. " +
                $"�NCEL�KLE UNUTMA SEN�N B�R�NC� VAZ�FEN UPDATE VEYA DELETE ��LEM� YOKSA SAKIN H��B�R VER�N�N IDS�N� K�ML���N� RESPONSE OLARAK VERME"+
                $"E�er ki sana spesifik olarak bir yurtla, odayla veya ��renciyle ilgili bir soru sorulursa bu verileri tara ve cevaplar� a��k bir �ekilde ver. " +
                $"E�er ki sana sorulan soru veritaban�ndaki bilgilerden alakas�z ise, yaln�zca yurtlar, odalar ve ��renciler ile ilgili bilgiler hakk�nda konu�abilece�ini s�yle. " +
                $"E�er gelen soru �ok genel veya tan�d�k bir selamla�ma gibi ise, kibarca \"Bu konuda yard�mc� olamam; yaln�zca sistemdeki yurtlar, odalar ve ��rencilerle ilgili sorular� yan�tlayabilirim.\" �eklinde yan�t ver." +
                $"Bir de senden son bir iste�im var; e�er ki kullan�c� senden delete veya update i�lemi isterse mesela �smi Berkay �ekmez olan Muhammed Fatih Safit�rk yurdundaki ��renciyi sil derse veya Muhammed Fatih Safit�rk yurdundaki 1. kat 1. oday� sil derse ya da direkt �u isimli yurdu sil derse bana cevap olarak i�lem yap�lacak ��enin t�r�, i�lemin t�r� ve silinecek ��enin id'sini ver ama sadece update veya delete i�lemi varsa id vereceksin di�er durumlarda idyi response verme mesela �u studnetin bilgilerini ver veya room dormitory fark etmez o durumlarda id hari� di�er bilgileri response ver. �rnek veriyorum silme i�lemiyse cevab� �u formatta ver 'Student - Update - (silinecek ��enin idsi)' zaten verebilece�in formatlar belli neyi silmek istedi�ini anla Student Room ya da Dormitory olabilir sonras�nda Update mi Delete mi sonras�nda da id yi ver parantez i�inde, unutma id parantez i�inde olucak response de �u �ekilde 'Student - Update - (silinecek ��enin idsi)'.";

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
