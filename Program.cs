using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YouTubePlaylistWatcher_desktop.Models;
using YouTubePlaylistWatcher_desktop.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<IAppDbContext, AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("postgresConnection")));

builder.Services.AddLogging();

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddSingleton<IGoogleAuthService, GoogleAuthService>();

var app = builder.Build();

using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;
var authService = services.GetRequiredService<IGoogleAuthService>();
await authService.Authenticate();