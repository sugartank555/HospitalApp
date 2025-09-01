using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HospitalApp.Data;
using HospitalApp.Models;          // ApplicationUser
using HospitalApp.Seed;            // DefaultSeeder

var builder = WebApplication.CreateBuilder(args);

// ===== DbContext =====
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ===== Identity + Roles (dùng ApplicationUser) =====
builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false; // bật true nếu dùng xác thực email
        options.Password.RequiredLength = 6;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// ===== MVC + Razor Pages (Identity UI cần RazorPages) =====
// Quan trọng: tắt implicit-required cho reference types không-nullable
builder.Services.AddControllersWithViews(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});
builder.Services.AddRazorPages();

var app = builder.Build();

// ===== Pipeline =====
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();   // phải trước Authorization
app.UseAuthorization();

// ===== Routes =====
// Area (Dashboard)
app.MapControllerRoute(
    name: "Areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Mặc định (User)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages(); // cho Identity Razor Pages (Login/Register, ...)

// ===== Seed Roles + Admin mặc định =====
using (var scope = app.Services.CreateScope())
{
    await DefaultSeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();
