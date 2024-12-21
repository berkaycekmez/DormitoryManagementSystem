using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DormitoryManagementSystem.WEB.Migrations
{
    /// <inheritdoc />
    public partial class firstmig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckInDate",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "CheckOutDate",
                table: "Students");

            migrationBuilder.AddColumn<int>(
                name: "CurrentCapacity",
                table: "Rooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DormitoryCurrentCapacity",
                table: "Dormitories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DormitoryPhotoUrl",
                table: "Dormitories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "OccupancyRate",
                table: "Dormitories",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentCapacity",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "DormitoryCurrentCapacity",
                table: "Dormitories");

            migrationBuilder.DropColumn(
                name: "DormitoryPhotoUrl",
                table: "Dormitories");

            migrationBuilder.DropColumn(
                name: "OccupancyRate",
                table: "Dormitories");

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckInDate",
                table: "Students",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckOutDate",
                table: "Students",
                type: "datetime2",
                nullable: true);
        }
    }
}
