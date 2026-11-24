using JarvisCoreEngine.Interfaces;
using JarvisCoreEngine.Models;
using JarvisCoreEngine.Services;
using JarvisCoreEngine.Utilities;

var builder = WebApplication.CreateBuilder(args);

// ===== CONFIG BINDING =====
var configuration = builder.Configuration;

var appSettings = new AppSettings();
configuration.GetSection("AppSettings").Bind(appSettings);

var loggingSettings = new LoggingSettings();
configuration.GetSection("LoggingSettings").Bind(loggingSettings);

var workspaceSettings = new WorkspaceSettings();
configuration.GetSection("WorkspaceSettings").Bind(workspaceSettings);

var httpSettings = new HttpSettings();
configuration.GetSection("HttpSettings").Bind(httpSettings);

var aiSettings = new AiSettings();
configuration.GetSection("AiSettings").Bind(aiSettings);

var memorySettings = new MemorySettings();
configuration.GetSection("MemorySettings").Bind(memorySettings);

// ===== REGISTER SETTINGS =====
builder.Services.AddSingleton(appSettings);
builder.Services.AddSingleton(loggingSettings);
builder.Services.AddSingleton(workspaceSettings);
builder.Services.AddSingleton(httpSettings);
builder.Services.AddSingleton(aiSettings);
builder.Services.AddSingleton(memorySettings);

// ===== CORE SERVICES =====
builder.Services.AddSingleton<IAppLogger, Logger>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<HttpService>();
builder.Services.AddSingleton<FileService>();
builder.Services.AddSingleton<MemoryService>();
builder.Services.AddSingleton<AiService>();

// ===== ASP.NET CORE =====
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS (for browser/mobile/web access)
builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// ===== MIDDLEWARE =====
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine("🚀 JARVIS API is online.");
app.Run();