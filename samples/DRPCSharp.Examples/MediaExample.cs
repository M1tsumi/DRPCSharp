using System;
using System.Threading.Tasks;
using DRPCSharp.Core;
using DRPCSharp.Model;
using DRPCSharp.Transport;
using Microsoft.Extensions.Logging;

namespace DRPCSharp.Examples
{
    /// <summary>
    /// Media player example showing how to update presence for music/video applications.
    /// </summary>
    public class MediaExample
    {
        private static readonly (string, string, string, string)[] _songs = new[]
        {
            ("Bohemian Rhapsody", "Queen", "A Night at the Opera", "4:55"),
            ("Stairway to Heaven", "Led Zeppelin", "Led Zeppelin IV", "8:02"),
            ("Hotel California", "Eagles", "Hotel California", "6:30"),
            ("Sweet Child O' Mine", "Guns N' Roses", "Appetite for Destruction", "5:56"),
            ("Imagine", "John Lennon", "Imagine", "3:07")
        };

        private static int _currentSongIndex = 0;
        private static ILogger _logger = null!;

        public static async Task RunAsync(ILogger logger)
        {
            _logger = logger;
            _logger.LogInformation("Running Media Example...");

            Console.WriteLine("=== Media Example ===");
            Console.WriteLine("This example simulates a music player with rich presence.");
            Console.WriteLine("Make sure you have uploaded album art assets to your Discord application.");
            Console.WriteLine();

            Console.Write("Enter your Discord Application ID: ");
            var appId = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(appId))
            {
                _logger.LogWarning("No Application ID provided. Using demo ID.");
                appId = "1234567890123456789";
            }

            try
            {
                await using var client = DrpcSharpClientFactory.Create(appId, logger: _logger as ILogger<DrpcSharpClient>);

                // Set up event handlers
                client.ConnectionStateChanged += (sender, args) =>
                {
                    _logger.LogInformation("Connection: {PreviousState} -> {CurrentState}", args.PreviousState, args.CurrentState);
                };

                client.ErrorOccurred += (sender, args) =>
                {
                    _logger.LogError(args.Exception, "An error occurred in the transport.");
                };

                // Connect
                _logger.LogInformation("Connecting to Discord...");
                await client.ConnectAsync();

                // Show media player simulation
                _logger.LogInformation("Media player simulation started.");
                Console.WriteLine("Commands: 'next' (next song), 'prev' (previous song), 'quit' to exit");
                Console.WriteLine();

                // Play first song
                await PlayCurrentSongAsync(client);

                // Handle user input
                while (true)
                {
                    var input = Console.ReadLine()?.ToLowerInvariant();
                    
                    switch (input)
                    {
                        case "next":
                            _currentSongIndex = (_currentSongIndex + 1) % _songs.Length;
                            await PlayCurrentSongAsync(client);
                            break;
                            
                        case "prev":
                        case "previous":
                            _currentSongIndex = _currentSongIndex == 0 ? _songs.Length - 1 : _currentSongIndex - 1;
                            await PlayCurrentSongAsync(client);
                            break;
                            
                        case "quit":
                        case "exit":
                            return;
                            
                        default:
                            Console.WriteLine("Unknown command. Try: next, prev, quit");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during the media example.");
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static async Task PlayCurrentSongAsync(DrpcSharpClient client)
        {
            var (title, artist, album, duration) = _songs[_currentSongIndex];
            var startTime = DateTimeOffset.UtcNow;

            _logger.LogInformation("Now playing: {Title} by {Artist}", title, artist);
            Console.WriteLine($"Now playing: {title} by {artist}");
            Console.WriteLine($"Album: {album} | Duration: {duration}");
            Console.WriteLine();

            var presence = new PresenceSnapshot
            {
                Details = title,
                State = $"{artist} - {album}",
                Assets = new PresenceAssets
                {
                    LargeImageKey = "album_art",
                    LargeImageText = album,
                    SmallImageKey = "play_button",
                    SmallImageText = "Playing"
                },
                StartedAt = startTime,
                Buttons = new[]
                {
                    new PresenceButton("Listen on Spotify", "https://open.spotify.com/search/" + Uri.EscapeDataString(title + " " + artist)),
                    new PresenceButton("View Lyrics", "https://genius.com/search?q=" + Uri.EscapeDataString(title + " " + artist))
                }
            };

            await client.SetPresenceAsync(presence);
        }
    }
}