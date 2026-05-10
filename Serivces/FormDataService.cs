using System;
using System.Collections.Generic;

namespace BlankFiller.Services;

/// <summary>
/// Единое хранилище данных для всех бланков.
/// Связывает одинаковые поля между разными страницами.
/// </summary>
public class FormDataService
{
    private readonly Dictionary<string, string> _data = new();

    /// <summary>
    /// Получить значение поля по ключу
    /// </summary>
    public string Get(string key)
    {
        return _data.TryGetValue(key, out var value) ? value : "";
    }

    /// <summary>
    /// Установить значение поля. Возвращает true, если значение изменилось.
    /// </summary>
    public bool Set(string key, string value)
    {
        if (_data.TryGetValue(key, out var existing) && existing == value)
            return false;

        _data[key] = value;
        DataChanged?.Invoke(key, value);
        return true;
    }

    /// <summary>
    /// Событие: данные изменились. Подписчики обновляют свои поля.
    /// </summary>
    public event Action<string, string>? DataChanged;
}