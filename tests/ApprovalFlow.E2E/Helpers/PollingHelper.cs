namespace ApprovalFlow.E2E.Helpers;

internal static class PollingHelper
{
    /// <summary>
    /// Repeatedly calls <paramref name="poll"/> until <paramref name="isDone"/> returns true
    /// or <paramref name="timeout"/> is exceeded (throws <see cref="TimeoutException"/>).
    /// </summary>
    public static async Task<T> WaitUntilAsync<T>(
        Func<CancellationToken, Task<T?>> poll,
        Func<T, bool> isDone,
        TimeSpan timeout,
        TimeSpan interval,
        CancellationToken externalCt = default)
        where T : class
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
        cts.CancelAfter(timeout);

        while (true)
        {
            T? result;
            try
            {
                result = await poll(cts.Token);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                throw new TimeoutException($"Polling timed out after {timeout.TotalSeconds}s.");
            }

            if (result is not null && isDone(result))
                return result;

            try
            {
                await Task.Delay(interval, cts.Token);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                throw new TimeoutException($"Polling timed out after {timeout.TotalSeconds}s.");
            }
        }
    }
}
