namespace Xg3.Auth.Internal;

internal interface ISystemClock
{
    DateTimeOffset UtcNow { get; }
}

internal sealed class SystemClock : ISystemClock
{
    public static SystemClock Instance { get; } = new();

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
