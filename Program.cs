using Microsoft.AspNetCore.Authentication.Cookies;
using VEMS.Areas.StudentPortal.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "VEMS.AdminPortal.Session";
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "VEMS.StudentPortal.Auth";
        options.LoginPath = "/studentportal/login";
        options.AccessDeniedPath = "/studentportal/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
builder.Services.AddScoped<IStudentLoginRepository, StudentLoginRepository>();

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

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Portal area routes. Keep these before the default public route.
app.MapAreaControllerRoute(
    name: "student-portal",
    areaName: "StudentPortal",
    pattern: "studentportal/{controller=Dashboard}/{action=Index}/{id?}");
app.MapAreaControllerRoute(
    name: "teacher-portal",
    areaName: "TeacherPortal",
    pattern: "teacherportal/{controller=Dashboard}/{action=Index}/{id?}");
app.MapAreaControllerRoute(
    name: "admin-portal",
    areaName: "AdminPortal",
    pattern: "adminportal/{controller=Dashboard}/{action=Index}/{id?}");

// Preserve the previously added management portal route so existing links keep working.
app.MapAreaControllerRoute(
    name: "management-portal",
    areaName: "ManagementPortal",
    pattern: "ManagementPortal/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
