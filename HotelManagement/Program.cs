using HotelManagement.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using HotelManagement.Services;
using Umbraco.Cms.Web.Common.PublishedModels;
using HotelManagement.Services.Interface;
using HotelManagement.Services.Interfaces;
using HotelManagement.Models.DTO;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// EF Core DbContext registration
builder.Services.AddDbContext<HotelContext>(options =>
	options.UseSqlServer(
		builder.Configuration.GetConnectionString("DefaultConnection")
	)
);

// Add MVC controllers with views for custom screens (e.g., Employees)
builder.Services.AddControllersWithViews();

// Application services
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IOtherTypeService, OtherTypeService>();
builder.Services.AddScoped<ITourTypeService, TourTypeService>();
builder.Services.AddScoped<ICurrencyService, CurrencyService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IExpenseTypeService, ExpenseTypeService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ILaundryService, LaundryService>();


builder.Services.Configure<PaginationSettings>(builder.Configuration.GetSection("Pagination"));

// Add CORS to allow frontend React app to call APIs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",      // Vite default port
                "http://localhost:5173",      // Vite alternative port
                "http://localhost:60713",     // HotelManagement HTTP port
                "https://localhost:44343"     // HotelManagement HTTPS port
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddDeliveryApi()
    .AddComposers()
    .Build();

WebApplication app = builder.Build();

// Enable CORS
app.UseCors("AllowReactApp");

await app.BootUmbracoAsync();

app.MapControllerRoute(
    name: "account",
    pattern: "Account/{action=Login}/{id?}",
    defaults: new { controller = "Account" });

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseInstallerEndpoints();
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();

        u.EndpointRouteBuilder.MapControllerRoute(
            name: "AccountLogin",
            pattern: "account/{action=login}",
            defaults: new { controller = "Account" });
    });

// Map conventional MVC routes for custom controllers
app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

await app.RunAsync();
