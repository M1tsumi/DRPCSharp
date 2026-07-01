using System;
using System.Threading.Tasks;
using DRPCSharp.Core;
using DRPCSharp.Model;
using DRPCSharp.Transport;
using Microsoft.Extensions.Logging;

namespace DRPCSharp.Examples
{
    /// <summary>
    /// Basic example showing how to connect to Discord and set a simple presence.
    /// </summary>
    public class BasicExample
    {
        public static async Task RunAsync(ILogger logger)
        {
            logger.LogInformation("Running Basic Example...");

            Console.WriteLine("=== Basic Example ===");
            Console.WriteLine("This example shows basic connection and presence setting.");
            Console.WriteLine("Make sure Discord is running and you have a valid Application ID.");
            Console.WriteLine();

            // Get application ID from user
            Console.Write("Enter your Discord Application ID: ");
            var appId = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(appId))
            {
                logger.LogWarning("No Application ID provided. Using demo ID.");
                appId = "1234567890123456789"; // Demo ID
            }

            try
            {
                // Create client with logging
                await using var client = DrpcSharpClientFactory.Create(appId, logger: logger as ILogger<DrpcSharpClient>);
                
                // Set up event handlers
                client.ConnectionStateChanged += (sender, args) =>
                {
                    logger.LogInformation("Connection state changed: {PreviousState} -> {CurrentState}", args.PreviousState, args.CurrentState);
                };

                client.ErrorOccurred += (sender, args) =>
                {
                    logger.LogError(args.Exception, "An error occurred in the transport.");
                };

                client.PresenceUpdated += (sender, args) =>
                {
                    logger.LogInformation("Presence updated: {Details}", args.Snapshot.Details);
                };

                // Connect to Discord
                logger.LogInformation("Connecting to Discord...");
                await client.ConnectAsync();

                // Set basic presence
                logger.LogInformation("Setting basic presence...");
                await client.SetPresenceAsync(new PresenceSnapshot
                {
                    Details = "DRPCSharp Basic Example",
                    State = "Learning the library"
                });

                Console.WriteLine("Presence set! Check your Discord profile.");
                Console.WriteLine("Press any key to disconnect...");
                Console.ReadKey();

                // Disconnect
                logger.LogInformation("Disconnecting...");
                await client.DisconnectAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during the basic example.");
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}