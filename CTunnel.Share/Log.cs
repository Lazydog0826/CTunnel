namespace CTunnel.Share
{
    public enum LogType
    {
        Default = 0,
        Success = 1,
        Error = 2,
        Important = 3,
    }

    public static class Log
    {
        public static void Write(string message, LogType logType = LogType.Default)
        {
            var color = logType switch
            {
                LogType.Default => ConsoleColor.White,
                LogType.Success => ConsoleColor.Green,
                LogType.Error => ConsoleColor.Red,
                LogType.Important => ConsoleColor.Cyan,
                _ => throw new Exception()
            };
            Console.ForegroundColor = color;
            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ": " + message);
            Console.ResetColor();
        }
    }
}
