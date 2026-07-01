using System;
using System.Threading.Tasks;
using DRPCSharp.Model;
using DRPCSharp.Transport;

namespace DRPCSharp.Examples
{
    /// <summary>
    /// Example showing error handling and recovery scenarios.
    /// </summary>
    public class ErrorHandlingExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("=== Error Handling Example ===");
            Console.WriteLine("This example demonstrates error handling and recovery.");
            Console.WriteLine("Try closing Discord while running to see error recovery in action.");
            Console.WriteLine();

            Console.Write("Enter your Discord Application ID: ");
            var appId = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(appId))
            {
                Console.WriteLine("No Application ID provided. Using demo ID.");
                appId = "1234567890123456789";
            }

            try
            {
                await using var client = DrpcSharpClientFactory.Create(appId);

                // Track connection state changes
                client.ConnectionStateChanged += (sender, args) =>
                {
                    Console.WriteLine($"[STATE] {args.PreviousState} -> {args.CurrentState}");
                    
                    if (args.CurrentState == ConnectionState.Connected)
                    {
                        Console.WriteLine("[INFO] Successfully connected to Discord!");
                    }
                    else if (args.CurrentState == ConnectionState.Disconnected && args.PreviousState == ConnectionState.Connected)
                    {
                        Console.WriteLine("[WARN] Lost connection to Discord. Will attempt to reconnect when you next update presence.");
                    }
                };

                // Handle transport errors
                client.ErrorOccurred += (sender, args) =>
                {
                    Console.WriteLine($"[ERROR] Transport error: {args.Exception?.Message}");
                    Console.WriteLine($"[ERROR] Operation: {args.Operation}");
                    Console.WriteLine($"[ERROR] Recoverable: {args.IsRecoverable}");
                    
                    if (!args.IsRecoverable)
                    {
                        Console.WriteLine("[ERROR] This error is not recoverable. Connection will be terminated.");
                    }
                    else
                    {
                        Console.WriteLine("[INFO] This error is recoverable. The library will attempt to recover.");
                    }
                };

                // Handle presence update events
                client.PresenceUpdated += (sender, args) =>
                {
                    Console.WriteLine($"[INFO] Presence updated successfully");
                    Console.WriteLine($"[INFO] Details: {args.Snapshot.Details}");
                    Console.WriteLine($"[INFO] State: {args.Snapshot.State}");
                };

                // Initial connection
                Console.WriteLine("Connecting to Discord...");
                await client.ConnectAsync();

                Console.WriteLine("Connected! Try these commands:");
                Console.WriteLine("  'update' - Update presence (will trigger reconnection if needed)");
                Console.WriteLine("  'invalid' - Try to send invalid data (will show validation errors)");
                Console.WriteLine("  'quit' - Exit");
                Console.WriteLine();
                Console.WriteLine("Tip: Try closing Discord to see error handling in action, then reopen it and use 'update' to reconnect.");
                Console.WriteLine();

                while (true)
                {
                    Console.Write("> ");
                    var input = Console.ReadLine()?.ToLowerInvariant();
                    
                    switch (input)
                    {
                        case "update":
                        case "u":
                            await UpdatePresenceAsync(client);
                            break;
                            
                        case "invalid":
                        case "i":
                            await SendInvalidDataAsync(client);
                            break;
                            
                        case "quit":
                        case "exit":
                        case "q":
                            return;
                            
                        default:
                            Console.WriteLine("Unknown command. Try: update, invalid, quit");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Unexpected error: {ex.Message}");
                Console.WriteLine($"[ERROR] Type: {ex.GetType().Name}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            }
        }

        private static async Task UpdatePresenceAsync(DrpcSharpClient client)
        {
            try
            {
                Console.WriteLine("[INFO] Attempting to update presence...");
                
                await client.SetPresenceAsync(new PresenceSnapshot
                {
                    Details = "Error Handling Example",
                    State = client.IsConnected ? "Connected and working" : "Disconnected but trying",
                    Assets = new PresenceAssets
                    {
                        LargeImageKey = "example_icon",
                        LargeImageText = "Error Handling Demo"
                    },
                    StartedAt = DateTimeOffset.UtcNow
                });
                
                Console.WriteLine("[INFO] Presence update completed.");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"[ERROR] Invalid operation: {ex.Message}");
                Console.WriteLine("[INFO] This usually means the client is not connected. Try reconnecting or check Discord.");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"[ERROR] Validation error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Unexpected error during update: {ex.Message}");
            }
        }

        private static async Task SendInvalidDataAsync(DrpcSharpClient client)
        {
            Console.WriteLine("[INFO] Attempting to send invalid data to show validation...");
            
            try
            {
                // Try various invalid scenarios
                var scenarios = new[]
                {
                    ("Too many buttons", () => new PresenceSnapshot
                    {
                        Details = "Test",
                        Buttons = new[]
                        {
                            new PresenceButton("Button 1", "https://example1.com"),
                            new PresenceButton("Button 2", "https://example2.com"),
                            new PresenceButton("Button 3", "https://example3.com") // Too many!
                        }
                    }),
                    ("Invalid URL", () => new PresenceSnapshot
                    {
                        Details = "Test",
                        Buttons = new[]
                        {
                            new PresenceButton("Invalid", "not-a-url") // Invalid URL
                        }
                    }),
                    ("Invalid party size", () => new PresenceSnapshot
                    {
                        Details = "Test",
                        Party = new PresenceParty
                        {
                            Id = "test",
                            Size = 5,
                            MaxSize = 3 // Size > MaxSize
                        }
                    }),
                    ("Invalid timestamps", () => new PresenceSnapshot
                    {
                        Details = "Test",
                        StartedAt = DateTimeOffset.UtcNow.AddHours(1), // Future start
                        EndsAt = DateTimeOffset.UtcNow // End before start
                    })
                };

                var (description, createPresence) = scenarios[_random.Next(scenarios.Length)];
                Console.WriteLine($"[INFO] Testing: {description}");
                
                await client.SetPresenceAsync(createPresence());
                Console.WriteLine("[ERROR] This should have failed validation!");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"[EXPECTED] Validation error caught: {ex.Message}");
                Console.WriteLine("[INFO] This is expected - the library prevents invalid data from being sent to Discord.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Unexpected error: {ex.Message}");
            }
        }

        private static readonly Random _random = new();
    }
}