using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DRPCSharp.Examples
{
    /// <summary>
    /// Main program that runs all examples.
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            // Set up logging
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("DRPCSharp", LogLevel.Debug)
                    .AddConsole();
            });
            var logger = loggerFactory.CreateLogger<Program>();

            logger.LogInformation("DRPCSharp Examples starting up...");

            Console.WriteLine("DRPCSharp Examples");
            Console.WriteLine("==================");
            Console.WriteLine();
            Console.WriteLine("This application demonstrates various ways to use DRPCSharp.");
            Console.WriteLine("Make sure Discord is running and you have a valid Application ID.");
            Console.WriteLine();

            while (true)
            {
                ShowMenu();
                
                var choice = Console.ReadLine();
                Console.WriteLine();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            await BasicExample.RunAsync(loggerFactory.CreateLogger<BasicExample>());
                            break;
                            
                        case "2":
                            await GameExample.RunAsync(loggerFactory.CreateLogger<GameExample>());
                            break;
                            
                        case "3":
                            await MediaExample.RunAsync(loggerFactory.CreateLogger<MediaExample>());
                            break;
                            
                        case "4":
                            await ErrorHandlingExample.RunAsync(loggerFactory.CreateLogger<ErrorHandlingExample>());
                            break;
                            
                        case "5":
                            await TestingExample.RunAsync(loggerFactory.CreateLogger<TestingExample>());
                            TestingExample.ShowTestPattern();
                            break;
                            
                        case "6":
                        case "q":
                        case "quit":
                        case "exit":
                            logger.LogInformation("Exiting application.");
                            Console.WriteLine("Goodbye!");
                            return;
                            
                        default:
                            Console.WriteLine("Invalid choice. Please try again.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while running an example.");
                    Console.WriteLine($"Error running example: {ex.Message}");
                }

                Console.WriteLine();
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                Console.Clear();
            }
        }

        static void ShowMenu()
        {
            Console.WriteLine("Choose an example to run:");
            Console.WriteLine();
            Console.WriteLine("1. Basic Example - Simple connection and presence");
            Console.WriteLine("2. Game Example - Multiplayer game simulation");
            Console.WriteLine("3. Media Example - Music player simulation");
            Console.WriteLine("4. Error Handling - Connection errors and validation");
            Console.WriteLine("5. Testing Example - Unit testing with InMemoryTransport");
            Console.WriteLine("6. Quit");
            Console.WriteLine();
            Console.Write("Enter your choice (1-6): ");
        }
    }
}