using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DormitoryManagementSystem.MODEL
{
    public class Room
    {
        public Room()
        {
            Students = new List<Student>();
        
        }

        public Guid RoomID { get; set; }
        public int Number { get; set; }
        public int Capacity { get; set; }
        public int CurrentCapacity { get; set; }
        public int Floor { get; set; }
        public int CurrentStudentNumber { get; set; }

        public Guid DormitoryID { get; set; }
        public  Dormitory Dormitory { get; set; }
        public List<Student> Students { get; set; }

    }
}
