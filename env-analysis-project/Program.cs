using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using env_analysis_project.Data;
using env_analysis_project.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ======================================
// Đăng ký DbContext
// ======================================
builder.Services.AddDbContext<env_analysis_projectContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("env_analysis_projectContext")
        ?? throw new InvalidOperationException("Connection string 'env_analysis_projectContext' not found."))
);

// ======================================
// Cấu hình Identity
// ======================================
// Nếu bạn đã cài package `Microsoft.AspNetCore.Identity.UI`, có thể dùng AddDefaultIdentity.
// Nếu bạn tự làm UI đăng nhập, chỉ cần AddIdentity (như bên dưới).

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Cấu hình các tùy chọn đăng nhập
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<env_analysis_projectContext>()
.AddDefaultTokenProviders();

// ======================================
// Thêm MVC Controller + View
// ======================================
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ======================================
// Cấu hình Middleware Pipeline
// ======================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Quan trọng: phải có Authentication trước Authorization
app.UseAuthentication();
app.UseAuthorization();

// ======================================
// Cấu hình Route
// ======================================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.MapControllerRoute(
    name: "account-management",
    pattern: "{controller=UserManagementController}/{action=Index}"
);
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ======================================
// Chạy ứng dụng
// ======================================
app.Run();
