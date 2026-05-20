using Microsoft.AspNetCore.Authentication.Cookies;
using Scalar.AspNetCore;    // <-- Scalar UI -->
using VEMS.Areas.AdminPortal.Services;
using VEMS.Areas.AdminPortal.Services.Examination;
using VEMS.Areas.AdminPortal.Services.Fee;
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
builder.Services.AddScoped<IStudentProfileRepository, StudentProfileRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IStudentsLoginRepository, StudentsLoginRepository>();
builder.Services.AddScoped<IConfigurationsRepository, ConfigurationsRepository>();
builder.Services.AddScoped<IFeeLookupRepository, FeeLookupRepository>();
builder.Services.AddScoped<IFeeHeadRepository, FeeHeadRepository>();
builder.Services.AddScoped<IFeeStructureRepository, FeeStructureRepository>();
builder.Services.AddScoped<IFeeConcessionRepository, FeeConcessionRepository>();
builder.Services.AddScoped<IFeeChallanRepository, FeeChallanRepository>();
builder.Services.AddScoped<IFeePaymentRepository, FeePaymentRepository>();
builder.Services.AddScoped<IExaminationBrowseRepository, ExaminationBrowseRepository>();

// <-- Scalar / OpenAPI: AddControllersWithViews above already registers controllers + API explorer. -->
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// NOT DEVELOPMENT: Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


// DEVELOPMENT: Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // <-- Scalar UI -->
    // 1. Generate the Swagger JSON file
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "openapi/{documentName}.json";
    });

    // 2. Map the Scalar UI endpoint
    app.MapScalarApiReference(options => 
    {
        options.Authentication = new ScalarAuthenticationOptions
        {
            // Use the plural property with a new list
            PreferredSecuritySchemes = new List<string> { "Cookie" }
        };
        // This allows Scalar to send your "VEMS.StudentPortal.Auth" cookie 
        // when you click "Test Request"
    });
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

app.MapControllers();   // <-- Scalar UI -->

app.Run();
