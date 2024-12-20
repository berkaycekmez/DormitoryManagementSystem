using DormitoryManagementSystem.DAL.Context;
using DormitoryManagementSystem.MODEL;
using Microsoft.AspNetCore.Mvc;

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
            students = context.Students.ToList();
            return View(students);
        }
    }
}
