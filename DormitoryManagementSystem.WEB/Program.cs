using DormitoryManagementSystem.DAL.Context;
using Microsoft.EntityFrameworkCore;
using Mscc.GenerativeAI;

var builder = WebApplication.CreateBuilder(args);
var apiKey = "AIzaSyB7jaLJB5nipqk0-gQ2wIlr7u9Vi881a4s";


// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<GoogleAI>(new GoogleAI(apiKey));


builder.Services.AddDbContext<MyDbContext>(option => option.UseSqlServer(builder.Configuration.GetConnectionString("Mysql"), b => b.MigrationsAssembly("DormitoryManagementSystem.WEB")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
