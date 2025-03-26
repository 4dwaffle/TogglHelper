namespace TogglHelper;

public sealed class ConsoleColorScope : IDisposable
{
    private readonly ConsoleColor previousColor;
    public ConsoleColorScope(ConsoleColor color) => (previousColor, Console.ForegroundColor) = (Console.ForegroundColor, color);
    public void Dispose() => Console.ForegroundColor = previousColor;
    public static ConsoleColorScope Red => new(ConsoleColor.Red);
    public static ConsoleColorScope Green => new(ConsoleColor.Green);
    public static ConsoleColorScope Yellow => new(ConsoleColor.Yellow);
}