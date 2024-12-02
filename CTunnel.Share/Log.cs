namespace CTunnel.Share
{
    public enum LogType
    {
        Success = 0,
        Error = 1,
        Important = 2
    }

    public static class Log
    {
        public static void Write(string message, LogType logType = LogType.Success)
        {
            var color = logType switch
            {
                LogType.Success => ConsoleColor.Green,
                LogType.Error => ConsoleColor.Red,
                LogType.Important => ConsoleColor.Cyan,
                _ => ConsoleColor.White
            };
            Console.ForegroundColor = color;
            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ": " + message);
            Console.ResetColor();
        }
    }
}
