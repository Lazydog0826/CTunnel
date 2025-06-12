using Spectre.Console;

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
            OutputMessageTypeEnum.Info => ("lime", "INFO"),
            OutputMessageTypeEnum.Error => ("red", "ERROR"),
            _
                => throw new ArgumentOutOfRangeException(
                    nameof(outputMessageType),
                    outputMessageType,
                    null
                ),
        };
        AnsiConsole.MarkupLine(
            $"[grey]{DateTime.Now:yyyy-MM-dd HH:mm:ss}[/] [{color}]{type}[/] - {msg}"
        );
    }
}

public enum OutputMessageTypeEnum
{
    Info,
    Error,
}
