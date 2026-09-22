using Microsoft.Extensions.Options;

namespace FunkArr.Tests.Shared;

public sealed class TestOptionsMonitor<T>(T value) : IOptionsMonitor<T>
{
    private readonly List<Action<T, string?>> _listeners = [];

    public T CurrentValue { get; set; } = value;

    public T Get(string? name) => CurrentValue;

    public IDisposable OnChange(Action<T, string?> listener)
    {
        _listeners.Add(listener);
        return new NoopDisposable();
    }

    public void Update(T newValue)
    {
        CurrentValue = newValue;
        foreach (var listener in _listeners)
        {
            listener(newValue, null);
        }
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
