// Program.cs  
using DormitoryManagementSystem.DAL.Context;
using DormitoryManagementSystem.MODEL.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Mscc.GenerativeAI;

var builder = WebApplication.CreateBuilder(args);
var apiKey = "AIzaSyCGtGtfwE3YJqMhp0X95QZSXK_3ZhZae6Y";
// Add services to the container.  
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<GoogleAI>(new GoogleAI());
// DB Context  
builder.Services.AddDbContext<MyDbContext>(option =>
    option.UseSqlServer(builder.Configuration.GetConnectionString("Mysql"),
    b => b.MigrationsAssembly("DormitoryManagementSystem.WEB")));

// Authorization policy'leri ekleyin  
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole",
        policy => policy.RequireRole("Admin"));
});

// Identity servisleri  
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<MyDbContext>()
.AddDefaultTokenProviders();

// Cookie ayarlarý  
builder.Services.AddAuthentication()
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    });

var app = builder.Build();

// Rolleri ve Admin kullanýcýsýný oluþtur  
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // Rolleri oluþtur  
    var roles = new[] { "Admin", "User" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Admin kullanýcýsýný oluþtur  
    string adminEmail = "admin@example.com";
    string adminPassword = "Admin123!";

    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var admin = new ApplicationUser
        {
            UserName = "admin",
            Email = adminEmail,
            EmailConfirmed = true
        };

        await userManager.CreateAsync(admin, adminPassword);
        await userManager.AddToRoleAsync(admin, "Admin");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();