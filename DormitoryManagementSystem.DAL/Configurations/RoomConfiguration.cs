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
    public class RoomConfiguration : IEntityTypeConfiguration<Room>
    {
        public void Configure(EntityTypeBuilder<Room> builder)
        {
            builder.HasOne(x=>x.Dormitory).WithMany(y=>y.Rooms).HasForeignKey(x=>x.DormitoryID).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
