# DRPCSharp

DRPCSharp is a small C# library for Discord Rich Presence. It is built around a simple goal: make the common path obvious, async-first, and easy to understand.

The library focuses on a few principles that keep the API practical and pleasant to use:

- a narrow public surface,
- immutable presence snapshots,
- explicit transport boundaries,
- implementation details that stay behind the scenes.

## Project Layout

The codebase is split into a few layers:

- `DRPCSharp.Core` contains the public client, connection state, and event surface.
- `DRPCSharp.Model` contains presence data, validation, and related value objects.
- `DRPCSharp.Protocol` contains the internal payload shape used to talk to Discord.
- `DRPCSharp.Transport` contains the IPC transport and test-friendly transport implementations.

The easiest way to get started is with `DrpcSharpClientFactory.Create(...)` from `DRPCSharp.Transport`. It gives you a ready-to-use client backed by the IPC transport.

## What This Repo Is For

This repository is the working area for the next version of the library. It is documented early so the code stays easy to follow as the surface grows.

The linked upstream reference is [M1tsumi/DRPCSharp](https://github.com/M1tsumi/DRPCSharp), but DRPCSharp is not trying to preserve that API. The goal is to keep the useful idea of a Discord presence client while making the structure more modern and easier to extend.

## What You Can Expect

- A small client surface that is easy to learn.
- Snapshot-based presence updates.
- Clear validation rules before data reaches the transport.
- A transport layer that is isolated from the user-facing model.
- Tests that cover the public behavior and the main validation rules.

## Near-term work

1. Add a thin sample project that shows connect, update, and disconnect end to end.
2. Expand inbound payload handling for Discord events and connection state.
3. Add integration tests against a real IPC pipe or a pipe simulator.
4. Finalize the public API boundary for any remaining protocol DTOs.
5. Polish package metadata, docs, and release ergonomics.

## Quick start

```csharp
using DRPCSharp.Model;
using DRPCSharp.Transport;

await using var client = DrpcSharpClientFactory.Create("your-application-id");

await client.ConnectAsync();
await client.SetPresenceAsync(new PresenceSnapshot
{
	Details = "Working in DRPCSharp",
	State = "Building the API"
});

// ... later ...

await client.DisconnectAsync();
```

## Notes

Documentation files other than this README are treated as local notes and are ignored by git.
