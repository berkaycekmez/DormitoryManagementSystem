using DormitoryManagementSystem.MODEL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DormitoryManagementSystem.DAL.Configurations
{
    public class StudentConfiguration : IEntityTypeConfiguration<Student>
    {
        public void Configure(EntityTypeBuilder<Student> builder)
        {
            builder.HasOne(x => x.Dormitory).WithMany(y => y.Students).HasForeignKey(x => x.DormitoryId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.Room).WithMany(y => y.Students).HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.ClientSetNull);

        }
    }
}
