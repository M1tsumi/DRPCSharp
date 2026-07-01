using DRPCSharp.Model;
using DRPCSharp.Protocol;

namespace DRPCSharp.Core;

public sealed class PresenceUpdatedEventArgs : EventArgs
{
    public PresenceUpdatedEventArgs(PresenceSnapshot snapshot, PresenceUpdateRequest request)
    {
        Snapshot = snapshot;
        Request = request;
    }

    public PresenceSnapshot Snapshot { get; }

    public PresenceUpdateRequest Request { get; }
}