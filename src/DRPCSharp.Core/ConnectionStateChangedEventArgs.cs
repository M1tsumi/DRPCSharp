namespace DRPCSharp.Core;

public sealed class ConnectionStateChangedEventArgs : EventArgs
{
    public ConnectionStateChangedEventArgs(ConnectionState previousState, ConnectionState currentState)
    {
        PreviousState = previousState;
        CurrentState = currentState;
    }

    public ConnectionState PreviousState { get; }

    public ConnectionState CurrentState { get; }
}