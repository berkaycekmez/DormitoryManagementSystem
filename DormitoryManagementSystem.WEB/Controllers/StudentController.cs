using DormitoryManagementSystem.DAL.Context;
using DormitoryManagementSystem.MODEL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DormitoryManagementSystem.WEB.Controllers
{
    public class StudentController : Controller
    {
        MyDbContext context;
        public StudentController(MyDbContext _context)
        {
            context = _context;
        }
        public IActionResult Index()
        {
            IEnumerable<Student> students = new List<Student>();
            students = context.Students.Include(x => x.Room).ThenInclude(y=>y.Dormitory).ToList();
            return View(students);
        }
    }
}
