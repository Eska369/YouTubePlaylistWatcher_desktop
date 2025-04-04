﻿using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Logging;

namespace YouTubePlaylistWatcher_desktop.Services;

public interface IGoogleAuthService
{
    Task Authenticate();
}

public class GoogleAuthService : IGoogleAuthService
{
    public UserCredential UserCredential { get; set; }
    private readonly ILogger<GoogleAuthService> _logger;

    public GoogleAuthService(ILogger<GoogleAuthService> logger)
    {
        _logger = logger;
    }
    public async Task Authenticate()
    {
        try
        {
            _logger.LogInformation("Authenticating Google API");

            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                (await GoogleClientSecrets.FromFileAsync("client_secrets.json")).Secrets,
                [YouTubeService.Scope.YoutubeReadonly],
                Environment.UserName,
                CancellationToken.None,
                new FileDataStore("YouTube.Auth.ReadOnly.Store")
            );
            _logger.LogInformation("Google API authenticated successfully");
            UserCredential = credential;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error during Google authentication");
        }
    }
}