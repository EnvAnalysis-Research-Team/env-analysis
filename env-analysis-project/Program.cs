using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using env_analysis_project.Data;
using Microsoft.AspNetCore.Identity;
using env_analysis_project.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<env_analysis_projectContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("env_analysis_projectContext") ?? throw new InvalidOperationException("Connection string 'env_analysis_projectContext' not found.")));

// Register Identity (use AddIdentity which does not require Identity.UI package)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<env_analysis_projectContext>()
.AddDefaultTokenProviders();

// Add services to the container.
builder.Services.AddControllersWithViews();
    
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

app.MapControllerRoute(
    name: "Dashboard",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
