using Spectre.Console;
using Spectre.Console.Json;

namespace CTunnel.Share;

public static class Output
{
    public static void Print(
        string msg,
        OutputMessageTypeEnum outputMessageType = OutputMessageTypeEnum.Info
    )
    {
        var (color, type) = outputMessageType switch
        {
            OutputMessageTypeEnum.Info => ("lime", "INFO "),
            OutputMessageTypeEnum.Error => ("red", "ERROR"),
            _ => throw new ArgumentOutOfRangeException(
                nameof(outputMessageType),
                outputMessageType,
                null
            ),
        };
        AnsiConsole.MarkupLine(
            $"[grey]{DateTime.Now:yyyy-MM-dd HH:mm:ss}[/] [{color}]{type}[/] - {msg}"
        );
    }

    public static void PrintConfig(string config)
    {
        var json = new JsonText(config);
        AnsiConsole.Write(new Panel(json).Collapse().RoundedBorder().BorderColor(Color.Aqua));
    }
}

public enum OutputMessageTypeEnum
{
    Info,
    Error,
}
