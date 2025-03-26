namespace TogglHelper.Extensions;

public static class TimeSpanExtensions
{
    public static TimeSpan Abs(this TimeSpan timeSpan) => timeSpan < TimeSpan.Zero ? timeSpan.Negate() : timeSpan;
}