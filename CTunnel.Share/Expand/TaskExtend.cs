namespace CTunnel.Share.Expand;

public static class TaskExtend
{
    /// <summary>
    /// 开启一个新的异步任务
    /// </summary>
    /// <param name="func"></param>
    /// <param name="catchFunc"></param>
    /// <param name="finallyFunc"></param>
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
                if (catchFunc != null)
                {
                    await catchFunc.Invoke(ex);
                }
            }
            finally
            {
                finallyFunc?.Invoke();
            }
        });
    }
}
