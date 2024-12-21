using DormitoryManagementSystem.MODEL;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DormitoryManagementSystem.MODEL.User;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace DormitoryManagementSystem.DAL.Context
{
    public class MyDbContext: IdentityDbContext<ApplicationUser>
    {
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
        public DbSet<Dormitory> Dormitories { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Student> Students { get; set; }
    }
}
