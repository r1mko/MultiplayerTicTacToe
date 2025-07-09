using System;
using System.Collections.Generic;

public static class EventManager
{
    private static readonly Dictionary<Type, List<Delegate>> m_Subscribers = new();

    public static void Subscribe<T>(Action<T> callback)
    {
        var type = typeof(T);
        if (!m_Subscribers.ContainsKey(type))
            m_Subscribers[type] = new List<Delegate>();
        m_Subscribers[type].Add(callback);
    }

    public static void Unsubscribe<T>(Action<T> callback)
    {
        var type = typeof(T);
        if (m_Subscribers.ContainsKey(type))
            m_Subscribers[type].Remove(callback);
    }

    public static  void Trigger<T>(T evt)
    {
        var type = typeof(T);
        if (m_Subscribers.ContainsKey(type))
            foreach (var callback in m_Subscribers[type])
                (callback as Action<T>)?.Invoke(evt);
    }

    public static void Subscribe(Type type, Delegate callback)
    {
        if (!m_Subscribers.ContainsKey(type))
            m_Subscribers[type] = new List<Delegate>();
        m_Subscribers[type].Add(callback);
    }

    public static void Unsubscribe(Type type, Delegate callback)
    {
        if (m_Subscribers.ContainsKey(type))
            m_Subscribers[type].Remove(callback);
    }
}
