namespace LteCar.Server.Services;

public class BiDictionary<TKey, TValue>
    where TKey : notnull
    where TValue : notnull
{
    private readonly Dictionary<TKey, TValue> _forward = new();
    private readonly Dictionary<TValue, TKey> _reverse = new();

    public void Add(TKey key, TValue value)
    {
        if (_forward.ContainsKey(key) || _reverse.ContainsKey(value))
            throw new ArgumentException("Duplicate key or value.");

        _forward.Add(key, value);
        _reverse.Add(value, key);
    }

    public bool TryGetByKey(TKey key, out TValue value) =>
        _forward.TryGetValue(key, out value!);

    public bool TryGetByValue(TValue value, out TKey key) =>
        _reverse.TryGetValue(value, out key!);

    public TValue this[TKey key] => _forward[key];
    public TKey this[TValue value] => _reverse[value];

    public bool RemoveByKey(TKey key)
    {
        if (_forward.TryGetValue(key, out var value))
        {
            _forward.Remove(key);
            _reverse.Remove(value);
            return true;
        }
        return false;
    }

    public bool RemoveByValue(TValue value)
    {
        if (_reverse.TryGetValue(value, out var key))
        {
            _reverse.Remove(value);
            _forward.Remove(key);
            return true;
        }
        return false;
    }
}