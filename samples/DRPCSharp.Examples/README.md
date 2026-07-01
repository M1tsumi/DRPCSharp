# DRPCSharp Examples

This project contains practical examples demonstrating how to use DRPCSharp in different scenarios.

## Examples Included

### 1. Basic Example (`BasicExample.cs`)
Shows the simplest possible usage:
- Creating a client
- Connecting to Discord
- Setting basic presence
- Handling events

### 2. Game Example (`GameExample.cs`)
Demonstrates game development scenarios:
- Multiplayer party information
- Dynamic score updates
- Player join/leave simulation
- Game session management

### 3. Media Example (`MediaExample.cs`)
Shows media player integration:
- Music track information
- Album art display
- External links (Spotify, lyrics)
- Track progression simulation

### 4. Error Handling Example (`ErrorHandlingExample.cs`)
Demonstrates robust error handling:
- Connection state monitoring
- Transport error recovery
- Validation error handling
- Reconnection scenarios

### 5. Testing Example (`TestingExample.cs`)
Shows unit testing patterns:
- Using InMemoryTransport
- Verifying presence updates
- Testing validation rules
- Mock client behavior

## Running the Examples

1. Make sure Discord is running on your system
2. Get a Discord Application ID from the [Discord Developer Portal](https://discord.com/developers/applications)
3. Build and run the project:

```bash
cd samples/DRPCSharp.Examples
dotnet run
```

4. Follow the on-screen menu to choose an example

## Setting up Discord Application

Before running examples:

1. Create a new application in the Discord Developer Portal
2. Go to "Rich Presence" → "Art Assets"
3. Upload some test images (recommended: 1024×1024 PNG)
4. Use asset names like:
   - `app_logo` - for basic examples
   - `ctf_map` - for game examples
   - `album_art` - for media examples
   - `example_icon` - for error handling examples

## Using the Examples in Your Project

Each example is self-contained and demonstrates specific patterns. You can copy the relevant parts into your own application:

- **Basic connection**: Use `BasicExample.cs` as a starting point
- **Game integration**: Reference `GameExample.cs` for multiplayer features
- **Media apps**: Use `MediaExample.cs` for music/video applications
- **Production code**: Study `ErrorHandlingExample.cs` for robust implementations
- **Unit tests**: Follow patterns in `TestingExample.cs` for test coverage

## Tips

- Start with the Basic Example to understand the fundamentals
- The Error Handling Example shows best practices for production code
- Use the Testing Example patterns to write unit tests for your presence logic
- All examples handle connection failures gracefully - study these patterns
- Examples include realistic data validation and error scenarios