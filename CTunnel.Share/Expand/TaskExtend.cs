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
            Func<Exception, Task>? catchFunc = null,
            Func<Task>? finallyFunc = null
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
                        Log.Write(ex.Message, LogType.Error);
                        if (catchFunc != null)
                        {
                            await catchFunc.Invoke(ex);
                        }
                    }
                    finally
                    {
                        if (finallyFunc != null)
                        {
                            await finallyFunc();
                        }
                    }
                })
                .ConfigureAwait(false);
        }

        public static async Task NewTaskAsBeginFunc<T>(
            Func<Task<T>> beginFunc,
            Func<T, Task> func,
            Func<Exception, Task>? catchFunc = null,
            Func<T, Task>? finallyFunc = null
        )
        {
            var obj = await beginFunc();
            _ = Task.Run(async () =>
                {
                    try
                    {
                        await func.Invoke(obj);
                    }
                    catch (Exception ex)
                    {
                        Log.Write(ex.Message, LogType.Error);
                        if (catchFunc != null)
                        {
                            await catchFunc.Invoke(ex);
                        }
                    }
                    finally
                    {
                        if (finallyFunc != null)
                        {
                            await finallyFunc(obj);
                        }
                    }
                })
                .ConfigureAwait(false);
        }
    }
}
