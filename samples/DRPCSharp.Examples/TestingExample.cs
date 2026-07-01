using System;
using System.Threading.Tasks;
using DRPCSharp.Core;
using DRPCSharp.Model;
using DRPCSharp.Transport;

namespace DRPCSharp.Examples
{
    /// <summary>
    /// Example showing how to use the InMemoryTransport for unit testing.
    /// </summary>
    public class TestingExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("=== Testing Example ===");
            Console.WriteLine("This example demonstrates how to use InMemoryTransport for unit testing.");
            Console.WriteLine("This is useful for testing your presence logic without connecting to Discord.");
            Console.WriteLine();

            try
            {
                // Create a mock transport for testing
                var transport = new InMemoryTransport();
                var client = new DrpcSharpClient(transport);

                Console.WriteLine("Testing connection lifecycle...");
                
                // Test connection
                Console.WriteLine("Connecting...");
                await client.ConnectAsync();
                Console.WriteLine($"Transport connected: {transport.Connected}");

                // Test presence update
                Console.WriteLine("Setting presence...");
                var testPresence = new PresenceSnapshot
                {
                    Details = "Test Application",
                    State = "Running unit tests",
                    Assets = new PresenceAssets
                    {
                        LargeImageKey = "test_icon",
                        LargeImageText = "Test Icon"
                    },
                    StartedAt = DateTimeOffset.UtcNow
                };

                await client.SetPresenceAsync(testPresence);
                Console.WriteLine($"Presence set. Transport received update: {transport.LastPresenceUpdate != null}");

                if (transport.LastPresenceUpdate != null)
                {
                    Console.WriteLine($"Last update details: {transport.LastPresenceUpdate.Details}");
                    Console.WriteLine($"Last update state: {transport.LastPresenceUpdate.State}");
                }

                // Test disconnection
                Console.WriteLine("Disconnecting...");
                await client.DisconnectAsync();
                Console.WriteLine($"Transport connected after disconnect: {transport.Connected}");

                Console.WriteLine();
                Console.WriteLine("Testing validation...");

                // Test invalid data
                try
                {
                    await client.SetPresenceAsync(new PresenceSnapshot
                    {
                        Details = "Test",
                        Buttons = new[]
                        {
                            new PresenceButton("Button 1", "https://example1.com"),
                            new PresenceButton("Button 2", "https://example2.com"),
                            new PresenceButton("Button 3", "https://example3.com") // Too many!
                        }
                    });
                    Console.WriteLine("ERROR: Invalid data should have thrown an exception!");
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"Expected validation error: {ex.Message}");
                }

                Console.WriteLine();
                Console.WriteLine("Testing example complete!");
                Console.WriteLine("In a real unit test, you would use assertions to verify behavior:");
                Console.WriteLine("  Assert.True(transport.Connected);");
                Console.WriteLine("  Assert.Equal(\"Test Application\", transport.LastPresenceUpdate.Details);");
                Console.WriteLine("  Assert.Throws<ArgumentException>(() => client.SetPresenceAsync(invalidPresence));");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Example of a real unit test pattern using DRPCSharp.
    /// </summary>
    public class GamePresenceServiceTests
    {
        public static void ShowTestPattern()
        {
            Console.WriteLine();
            Console.WriteLine("=== Unit Test Pattern Example ===");
            Console.WriteLine("Here's how you might structure unit tests for your application:");
            Console.WriteLine();

            var exampleTest = @"
[TestClass]
public class GamePresenceServiceTests
{
    private InMemoryTransport _transport;
    private DrpcSharpClient _client;
    private GamePresenceService _service;

    [TestInitialize]
    public void Setup()
    {
        _transport = new InMemoryTransport();
        _client = new DrpcSharpClient(_transport);
        _service = new GamePresenceService(_client);
    }

    [TestMethod]
    public async Task UpdatePresence_WithValidGameState_UpdatesDiscord()
    {
        // Arrange
        var gameState = new GameState
        {
            PlayerName = ""TestPlayer"",
            Score = 100,
            Level = 5
        };

        // Act
        await _service.UpdatePresenceAsync(gameState);

        // Assert
        Assert.IsTrue(_transport.Connected);
        Assert.IsNotNull(_transport.LastPresenceUpdate);
        StringAssert.Contains(_transport.LastPresenceUpdate.Details, ""TestPlayer"");
        Assert.AreEqual(""Level 5 - Score: 100"", _transport.LastPresenceUpdate.State);
    }

    [TestMethod]
    public async Task UpdatePresence_WhenDisconnected_ThrowsInvalidOperationException()
    {
        // Arrange - don't connect
        var gameState = new GameState { PlayerName = ""TestPlayer"" };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => _service.UpdatePresenceAsync(gameState));
    }
}

public class GamePresenceService
{
    private readonly DrpcSharpClient _client;

    public GamePresenceService(DrpcSharpClient client)
    {
        _client = client;
    }

    public async Task UpdatePresenceAsync(GameState gameState)
    {
        var presence = new PresenceSnapshot
        {
            Details = $""{gameState.PlayerName} is playing"",
            State = $""Level {gameState.Level} - Score: {gameState.Score}"",
            Assets = new PresenceAssets
            {
                LargeImageKey = ""game_icon"",
                LargeImageText = ""My Awesome Game""
            }
        };

        await _client.SetPresenceAsync(presence);
    }
}

public class GameState
{
    public string PlayerName { get; set; }
    public int Score { get; set; }
    public int Level { get; set; }
}
";

            Console.WriteLine(exampleTest);
            Console.WriteLine();
            Console.WriteLine("This pattern allows you to:");
            Console.WriteLine("1. Test your presence logic without connecting to Discord");
            Console.WriteLine("2. Verify the exact data that would be sent");
            Console.WriteLine("3. Test error conditions and edge cases");
            Console.WriteLine("4. Run tests quickly without external dependencies");
        }
    }
}