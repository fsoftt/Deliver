namespace Deliver.Testing;

/// <summary>Asserting on eventually consistent state: poll until the condition holds or time runs out.</summary>
public static class Eventually
{
    public static async Task<T> GetAsync<T>(Func<Task<T?>> probe, Func<T, bool>? until = null, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(20));
        T? last = default;
        while (DateTime.UtcNow < deadline)
        {
            last = await probe();
            if (last is not null && (until is null || until(last)))
                return last;

            await Task.Delay(200);
        }

        throw new TimeoutException($"Condition not met in time. Last value: {last}");
    }

    public static async Task UntilAsync(Func<Task<bool>> condition, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(20));
        while (!await condition())
        {
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException("Condition not met in time.");

            await Task.Delay(200);
        }
    }
}
