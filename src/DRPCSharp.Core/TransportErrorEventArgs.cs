namespace DRPCSharp.Core;

public sealed class TransportErrorEventArgs : EventArgs
{
    public TransportErrorEventArgs(TransportErrorOperation operation, string message, Exception exception, bool isRecoverable)
    {
        Operation = operation;
        Message = message;
        Exception = exception;
        IsRecoverable = isRecoverable;
    }

    public TransportErrorOperation Operation { get; }

    public string Message { get; }

    public Exception Exception { get; }

    public bool IsRecoverable { get; }
}