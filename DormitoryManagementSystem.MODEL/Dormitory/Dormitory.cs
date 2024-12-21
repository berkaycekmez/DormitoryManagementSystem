using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DormitoryManagementSystem.MODEL
{
    public class Dormitory
    {
        public Dormitory()
        {
            Rooms = new List<Room>();
            Students = new List<Student>();
        }
        public Guid DormitoryID { get; set; }
        public string? DormitoryName { get; set; }
        public string? DormitoryPhotoUrl { get; set; }
        public string? Address { get; set; }
        [RegularExpression(@"^(\+90|0)?5\d{9}$", ErrorMessage = "Lütfen geçerli bir telefon numarası giriniz.")]
        public string? Phone { get; set; }
        public int DormitoryCapacity { get; set; }
        public int DormitoryCurrentCapacity { get; set; }
        public double OccupancyRate { get; set; }


        public List<Room> Rooms { get; set; }
        public List<Student> Students { get; set; }
    }

}
