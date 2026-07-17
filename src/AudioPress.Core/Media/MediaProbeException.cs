namespace AudioPress.Core.Media;

public sealed class MediaProbeException : Exception
{
    public MediaProbeException(string message)
        : base(message)
    {
    }

    public MediaProbeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

