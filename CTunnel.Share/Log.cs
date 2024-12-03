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
            Console.ResetColor();
            Console.Write(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "：");
            Console.ForegroundColor = color;
            Console.Write(message);
            Console.WriteLine();
            Console.ResetColor();
        }

        public static void WriteLogo()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(
                @"
   ____ _____                       _ 
  / ___|_   _|   _ _ __  _ __   ___| |
 | |     | || | | | '_ \| '_ \ / _ \ |
 | |___  | || |_| | | | | | | |  __/ |
  \____| |_| \__,_|_| |_|_| |_|\___|_|
"
            );
            Console.ResetColor();
        }
    }
}
