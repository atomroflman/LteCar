public class CarConfigurationService {
    public event Action OnConfigurationChanged;

    private CarConfiguration _configuration;
    public CarConfiguration Configuration {
        get { return _configuration;}
        set {
            if (Object.ReferenceEquals(_configuration, value))
                return;
            var newConfig = MergeObjects(_configuration, value);
            if (!CheckForChanges(newConfig, _configuration))
                return;
            _configuration = value;
            if (OnConfigurationChanged)
                OnConfigurationChanged();
        }
    }

    public static T MergeObjects<T>(T current, T next) 
        where T : new()
    {
        var t = typeof(T);
        var res = new T();
        foreach (var prop in t.GetProperties()) {
            var currentValue = prop.GetValue(current, null);
            var nextValue = prop.GetValue(next, null);
            if (Object.ReferenceEquals(currentValue, nextValue))
                continue;
            prop.SetValue(res, nextValue ?? currentValue);
        }
    }
}