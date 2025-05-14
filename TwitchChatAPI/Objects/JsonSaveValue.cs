namespace TwitchChatAPI.Objects;

internal class JsonSaveValue<T> : ObservableValue<T>
{
    public JsonSave JsonSave { get; private set; }
    public string Key { get; private set; }
    public T DefaultValue { get; private set; }
    public bool ReadFile { get; private set; }

    public bool HasValue => TryLoad(out T _);

    public JsonSaveValue(JsonSave jsonSave, string key, T defaultValue = default, bool readFile = false)
    {
        JsonSave = jsonSave;
        Key = key;
        DefaultValue = defaultValue;
        ReadFile = readFile;

        CustomValueGetter = Load;
        CustomValueSetter = Save;
    }

    private T Load()
    {
        return JsonSave.Load(Key, DefaultValue, ReadFile);
    }

    private bool TryLoad(out T value)
    {
        return JsonSave.TryLoad(Key, out value, ReadFile);
    }

    private void Save(T value)
    {
        if (!Equals(value, Value))
        {
            JsonSave.Save(Key, value);
        }
    }
}
