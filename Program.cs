using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YouTubePlaylistWatcher_desktop;
using YouTubePlaylistWatcher_desktop.Models;
using YouTubePlaylistWatcher_desktop.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<IAppDbContext, AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("postgresConnection")));

builder.Services.AddLogging();

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddSingleton<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddScoped<IYouTubeServiceWrapper, YouTubeServiceWrapper>();

var app = builder.Build();


using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;
var youTubeServiceWrapper = services.GetRequiredService<IYouTubeServiceWrapper>();
MemoryStorage.UserId = await youTubeServiceWrapper.GetCurrentChannelAsync();

await youTubeServiceWrapper.GetPlaylistAsync("");
await youTubeServiceWrapper.ListPlaylistsAsync();