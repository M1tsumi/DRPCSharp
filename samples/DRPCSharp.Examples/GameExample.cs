using System;
using System.Threading;
using System.Threading.Tasks;
using DRPCSharp.Core;
using DRPCSharp.Model;
using DRPCSharp.Transport;
using Microsoft.Extensions.Logging;

namespace DRPCSharp.Examples
{
    /// <summary>
    /// Advanced example showing game development scenarios with parties, assets, and timers.
    /// </summary>
    public class GameExample
    {
        private static readonly Random _random = new();
        private static int _playerScore = 0;
        private static int _enemyScore = 0;
        private static int _playerCount = 1;
        private static readonly int _maxPlayers = 10;
        private static ILogger _logger = null!;

        public static async Task RunAsync(ILogger logger)
        {
            _logger = logger;
            _logger.LogInformation("Running Game Example...");

            Console.WriteLine("=== Game Example ===");
            Console.WriteLine("This example simulates a multiplayer game with rich presence.");
            Console.WriteLine("Make sure you have uploaded game assets to your Discord application.");
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

                // Start game simulation
                _logger.LogInformation("Starting game simulation...");
                Console.WriteLine("Commands: 'score' (add score), 'join' (player joins), 'leave' (player leaves), 'quit' to exit");
                Console.WriteLine();

                var cts = new CancellationTokenSource();
                var gameTask = GameLoopAsync(client, cts.Token);

                // Handle user input
                while (!cts.Token.IsCancellationRequested)
                {
                    var input = Console.ReadLine()?.ToLowerInvariant();
                    
                    switch (input)
                    {
                        case "score":
                            AddScore();
                            break;
                            
                        case "join":
                            PlayerJoins();
                            break;
                            
                        case "leave":
                            PlayerLeaves();
                            break;
                            
                        case "quit":
                        case "exit":
                            cts.Cancel();
                            break;
                            
                        default:
                            Console.WriteLine("Unknown command. Try: score, join, leave, quit");
                            break;
                    }
                }

                await gameTask;
                _logger.LogInformation("Game simulation ended.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during the game example.");
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static async Task GameLoopAsync(DrpcSharpClient client, CancellationToken cancellationToken)
        {
            var matchId = $"match_{Guid.NewGuid():N}";
            var startTime = DateTimeOffset.UtcNow;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Update presence with current game state
                    var presence = new PresenceSnapshot
                    {
                        Details = $"Capture the Flag - Score: {_playerScore}-{_enemyScore}",
                        State = $"In Match ({_playerCount}/{_maxPlayers} players)",
                        Party = new PresenceParty
                        {
                            Id = matchId,
                            Size = _playerCount,
                            MaxSize = _maxPlayers
                        },
                        Assets = new PresenceAssets
                        {
                            LargeImageKey = "ctf_map",
                            LargeImageText = "Desert Storm Map",
                            SmallImageKey = "red_team",
                            SmallImageText = $"Score: {_playerScore}"
                        },
                        Secrets = new PresenceSecrets
                        {
                            JoinSecret = $"join_{matchId}",
                            SpectateSecret = $"spectate_{matchId}"
                        },
                        StartedAt = startTime
                    };

                    _logger.LogInformation("Updating presence: {Details}", presence.Details);
                    await client.SetPresenceAsync(presence, cancellationToken);

                    // Simulate some game activity
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Normal cancellation
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating presence in game loop.");
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                }
            }
        }

        private static void AddScore()
        {
            if (_random.Next(0, 2) == 0)
            {
                _playerScore++;
                _logger.LogInformation("Player team scored! Score: {PlayerScore}-{EnemyScore}", _playerScore, _enemyScore);
            }
            else
            {
                _enemyScore++;
                _logger.LogInformation("Enemy team scored! Score: {PlayerScore}-{EnemyScore}", _playerScore, _enemyScore);
            }
        }

        private static void PlayerJoins()
        {
            if (_playerCount < _maxPlayers)
            {
                _playerCount++;
                _logger.LogInformation("Player joined. Total players: {PlayerCount}/{MaxPlayers}", _playerCount, _maxPlayers);
            }
            else
            {
                _logger.LogWarning("A player tried to join, but the server is full.");
            }
        }

        private static void PlayerLeaves()
        {
            if (_playerCount > 1)
            {
                _playerCount--;
                _logger.LogInformation("Player left. Total players: {PlayerCount}/{MaxPlayers}", _playerCount, _maxPlayers);
            }
            else
            {
                _logger.LogWarning("The last player tried to leave.");
            }
        }
    }
}