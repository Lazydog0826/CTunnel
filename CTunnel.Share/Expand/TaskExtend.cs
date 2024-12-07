namespace CTunnel.Share.Expand
{
    public static class TaskExtend
    {
        /// <summary>
        /// 开启一个新的异步任务
        /// </summary>
        /// <param name="func"></param>
        /// <param name="action"></param>
        /// <param name="name"></param>
        public static void NewTask(
            Func<Task> func,
            Func<Exception, Task>? action = null,
            string name = ""
        )
        {
            _ = Task.Run(async () =>
                {
                    try
                    {
                        await func.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Log.Write(ex.Message, LogType.Error, name);
                        if (action != null)
                        {
                            await action.Invoke(ex);
                        }
                    }
                })
                .ConfigureAwait(false);
        }
    }
}
