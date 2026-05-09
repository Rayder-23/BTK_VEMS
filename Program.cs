var builder = WebApplication.CreateBuilder(args);

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

// Area routes — /StudentPortal, /TeacherPortal, /ManagementPortal → Each area's Home/Index
app.MapAreaControllerRoute(
    name: "StudentPortal",
    areaName: "StudentPortal",
    pattern: "StudentPortal/{controller=Home}/{action=Index}/{id?}");
app.MapAreaControllerRoute(
    name: "TeacherPortal",
    areaName: "TeacherPortal",
    pattern: "TeacherPortal/{controller=Home}/{action=Index}/{id?}");
app.MapAreaControllerRoute(
    name: "ManagementPortal",
    areaName: "ManagementPortal",
    pattern: "ManagementPortal/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
