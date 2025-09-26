using econsys.DocGenServiceSTA.Services;
using econsys.DocGenServiceSTA.Services.Interfaces;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<IDocGenInitializerService, DocGenInitializerService>();
builder.Services.AddTransient<IDocGeneratorService, DocGeneratorService>();

//TODO: 
//1. Basic Authentication - API Key for now
//2. Issue - For Non SFDT, Variables in Header/Footer not getting resolved

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Host.UseSerilog();


var app = builder.Build();

//Register Syncfusion license
var syncfusionLicenseKey = app.Configuration.GetValue<string>("Syncfusion:LicenseKey");
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(syncfusionLicenseKey);


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
