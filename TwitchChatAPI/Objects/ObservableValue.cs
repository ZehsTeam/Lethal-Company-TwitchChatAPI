using System;

namespace TwitchChatAPI.Objects;

internal class ObservableValue<T>
{
    public T Value
    {
        get => GetValue();
        set => SetValue(value);
    }

    protected T _value;

    public event Action<T> OnValueChanged;

    protected Func<T> CustomValueGetter;
    protected Action<T> CustomValueSetter;

    public ObservableValue(T initialValue = default)
    {
        _value = initialValue;
    }

    private T GetValue()
    {
        if (CustomValueGetter != null)
        {
            _value = CustomValueGetter.Invoke();
        }

        return _value;
    }

    private void SetValue(T value)
    {
        if (!Equals(_value, value))
        {
            _value = value;
            CustomValueSetter?.Invoke(value);
            OnValueChanged?.Invoke(value);
        }
    }
}
