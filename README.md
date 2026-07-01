# DRPCSharp

A modern C# library for Discord Rich Presence that's built for real-world applications.

## What is it?

DRPCSharp is a Discord Rich Presence library designed to be straightforward, reliable, and easy to integrate into your .NET applications. Whether you're building a game, a media player, or any application where users want to share their activity, this library gets you connected to Discord without the usual headaches.

## Why use DRPCSharp?

- **Simple to use**: Get connected in just a few lines of code
- **Built for modern .NET**: Full async/await support throughout
- **Type-safe**: Strong typing helps catch errors at compile time
- **Testable**: Includes test doubles for unit testing
- **Reliable**: Handles connection issues and reconnection automatically

## Quick Start

```csharp
using DRPCSharp.Model;
using DRPCSharp.Transport;

// Create client with your Discord Application ID
await using var client = DrpcSharpClientFactory.Create("1234567890123456789");

// Connect to Discord
await client.ConnectAsync();

// Set your presence
await client.SetPresenceAsync(new PresenceSnapshot
{
    Details = "Developing with DRPCSharp",
    State = "Writing code"
});
```

That's it - your presence is now live on Discord.

## Real Examples

### Game Development
```csharp
await client.SetPresenceAsync(new PresenceSnapshot
{
    Details = "Capture the Flag",
    State = "Score: 15 - 12",
    Party = new PresenceParty
    {
        Id = "match_12345",
        Size = 8,
        MaxSize = 10
    },
    Assets = new PresenceAssets
    {
        LargeImageKey = "ctf_map",
        LargeImageText = "Desert Storm Map",
        SmallImageKey = "red_team",
        SmallImageText = "Red Team"
    },
    StartedAt = DateTimeOffset.UtcNow
});
```

### Media Application
```csharp
await client.SetPresenceAsync(new PresenceSnapshot
{
    Details = "Bohemian Rhapsody",
    State = "Queen - A Night at the Opera",
    Assets = new PresenceAssets
    {
        LargeImageKey = "album_art",
        LargeImageText = "A Night at the Opera (1975)"
    },
    StartedAt = DateTimeOffset.UtcNow,
    Buttons = new[]
    {
        new PresenceButton("Listen on Spotify", "https://open.spotify.com/track/..."),
        new PresenceButton("View Lyrics", "https://genius.com/...")
    }
});
```

### Productivity Tool
```csharp
await client.SetPresenceAsync(new PresenceSnapshot
{
    Details = "Working on DRPCSharp",
    State = "Implementing features",
    Assets = new PresenceAssets
    {
        LargeImageKey = "vscode_icon",
        LargeImageText = "Visual Studio Code"
    },
    Buttons = new[]
    {
        new PresenceButton("View Project", "https://github.com/yourusername/yourproject")
    }
});
```

## Handling Connection Events

```csharp
// Monitor connection state
client.ConnectionStateChanged += (sender, args) =>
{
    Console.WriteLine($"Connection: {args.PreviousState} -> {args.CurrentState}");
};

// Handle errors
client.ErrorOccurred += (sender, args) =>
{
    Console.WriteLine($"Error: {args.Exception?.Message}");
    if (args.IsRecoverable)
    {
        Console.WriteLine("Attempting to recover...");
    }
};

// Track presence updates
client.PresenceUpdated += (sender, args) =>
{
    Console.WriteLine($"Updated: {args.Snapshot.Details}");
};
```

## Architecture

The library is organized into clear layers:

- **DRPCSharp.Core**: Main client, connection management, and events
- **DRPCSharp.Model**: Data structures and validation
- **DRPCSharp.Protocol**: Internal Discord communication protocol
- **DRPCSharp.Transport**: IPC transport and connection handling

This separation makes the code easier to understand, test, and maintain.

## Installation

Add the NuGet package to your project:

```xml
<PackageReference Include="DRPCSharp" Version="1.0.0" />
```

Or install individual components if you need more control:

```xml
<PackageReference Include="DRPCSharp.Core" Version="1.0.0" />
<PackageReference Include="DRPCSharp.Model" Version="1.0.0" />
<PackageReference Include="DRPCSharp.Transport" Version="1.0.0" />
```

## Setting up Discord Application

Before using the library, you need a Discord application:

1. Go to [Discord Developer Portal](https://discord.com/developers/applications)
2. Click "New Application" and give it a name
3. Navigate to "Rich Presence" → "Art Assets"
4. Upload your images (recommended: 1024×1024 PNG)
5. Note your Application ID from the General Information page

**Asset Guidelines:**
- Size: 1024×1024 pixels recommended
- Format: PNG with transparency support
- Size Limit: 512KB maximum per asset
- Naming: Use descriptive keys like `app_logo`, `main_character`
- You can upload up to 300 assets per application

## Validation

The library validates your data before sending to Discord:

```csharp
try
{
    await client.SetPresenceAsync(new PresenceSnapshot
    {
        Details = "My Game",
        Buttons = new[]
        {
            new PresenceButton("Invalid URL", "not-a-url") // This will throw!
        }
    });
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Validation error: {ex.Message}");
}
```

**Built-in Validation Rules:**
- Buttons: Maximum 2 buttons, must have valid URLs
- Party: Size must be ≤ MaxSize, both must be positive
- Timestamps: End time must be after start time
- Secrets: Optional but must not be whitespace when provided
- Assets: Image keys must reference uploaded Discord assets

## Testing

The library includes an InMemoryTransport for unit testing:

```csharp
// Perfect for unit testing your presence logic
var transport = new InMemoryTransport();
var client = new DrpcSharpClient(transport);

// Your test code here
await client.ConnectAsync();
await client.SetPresenceAsync(new PresenceSnapshot { Details = "Test" });

// Verify the transport received the correct data
Assert.True(transport.Connected);
Assert.NotNull(transport.LastPresenceUpdate);
```

## Performance

The library is designed for minimal overhead:
- Connection reuse when possible
- Lazy initialization of resources
- Async I/O throughout
- Minimal allocations during normal operation
- Configurable timeouts to prevent hanging

**Best Practices:**
```csharp
// Reuse client instances when possible
await using var client = DrpcSharpClientFactory.Create(appId);

// Update presence only when data actually changes
if (newPresence != currentPresence)
{
    await client.SetPresenceAsync(newPresence);
}

// Use appropriate update intervals (not too frequent)
// Recommended: Update every 15-30 seconds for dynamic content
```

## Troubleshooting

### Connection Issues
- Ensure Discord is running
- Check if Discord's Game Activity is enabled
- Verify your Application ID is correct
- Check firewall settings for named pipes

### Images Not Showing
- Verify asset names match exactly (case-sensitive)
- Ensure images are uploaded to your Discord application
- Check image size and format requirements
- Wait a few seconds for Discord to cache new assets

### Presence Not Updating
- Check for validation errors in your data
- Ensure you're connected before setting presence
- Verify Discord isn't rate-limiting your updates
- Check Discord's Game Activity settings

### Debug Mode
Enable detailed logging to troubleshoot issues:

```csharp
var options = new DiscordIpcTransportOptions
{
    EnableDebugLogging = true,
    LogLevel = LogLevel.Debug
};
```

## Development

### Running Tests
```bash
dotnet test DRPCSharp.slnx
```

### Building from Source
```bash
dotnet build DRPCSharp.slnx
```

## Roadmap

- Discord Social SDK integration
- Advanced retry policies and circuit breakers
- Performance metrics and monitoring
- Unity and Unreal Engine plugins

## License

MIT License - see LICENSE file for details.

## Support

- **Issues**: [GitHub Issues](https://github.com/yourusername/DRPCSharp/issues)
- **Documentation**: Check the docs folder for detailed API documentation

---

Built for developers who want Discord Rich Presence without the complexity.