using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DormitoryManagementSystem.MODEL
{
    public class Student
    {
        public Guid StudentId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        [RegularExpression(@"^(\+90|0)?5\d{9}$", ErrorMessage = "Lütfen geçerli bir telefon numarası giriniz.")]
        public string Phone { get; set; }
        public string PhotoUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool statusDeletedStudent { get; set; }

        public Guid RoomId { get; set; }
        public Guid DormitoryId { get; set; }


        public Room Room { get; set; }
        public Dormitory Dormitory { get; set; }
    }
}
