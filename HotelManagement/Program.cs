using HotelManagement.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using HotelManagement.Services;
using Umbraco.Cms.Web.Common.PublishedModels;
using HotelManagement.Services.Interface;
using HotelManagement.Services.Interfaces;

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


builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddDeliveryApi()
    .AddComposers()
    .Build();

WebApplication app = builder.Build();

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
