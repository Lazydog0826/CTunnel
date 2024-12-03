namespace CTunnel.Share.Expand
{
    public static class TaskExtend
    {
        public static void NewTask(Func<Task> func, Func<Exception, Task>? action = null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await func.Invoke();
                }
                catch (Exception ex)
                {
                    Log.Write(ex.Message, LogType.Error);
                    if (action != null)
                    {
                        await action.Invoke(ex);
                    }
                }
            });
        }
    }
}
