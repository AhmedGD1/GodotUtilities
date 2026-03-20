using System.Collections.Generic;
using System;
using Godot;

namespace Utilities.Events;

public static class EventBus
{
    private static readonly Dictionary<Type, List<Delegate>> listeners = new();
    private static readonly Dictionary<Type, object> cachedInstances   = new();

    #region Typed API

    public static void AddListener<T>(Action<T> listener, Node owner = null)
    {
        if (!listeners.TryGetValue(typeof(T), out var list))
            listeners[typeof(T)] = list = new();
        list.Add(listener);

        if (owner != null)
            owner.TreeExiting += () => RemoveListener(listener);
    }

    public static Action<T> AddListener<T>(Action listener, Node owner = null)
    {
        void wrapper(T _) => listener();
        AddListener((Action<T>)wrapper, owner);
        return wrapper;
    }

    public static void AddListenerOnce<T>(Action<T> listener, Node owner = null)
    {
        Action<T> wrapper = null!;
        wrapper = (T evt) =>
        {
            RemoveListener(wrapper);
            listener(evt);
        };
        AddListener(wrapper, owner);
    }

    public static void AddListenerOnce<T>(Action listener, Node owner = null) where T : new()
    {
        Action<T> wrapper = null!;
        wrapper = (T _) =>
        {
            RemoveListener(wrapper);
            listener();
        };
        AddListener(wrapper, owner);
    }

    public static void RemoveListener<T>(Action<T> listener)
    {
        if (listeners.TryGetValue(typeof(T), out var list))
            list.Remove(listener);
    }

    public static void Trigger<T>(T evt)
    {
        if (!listeners.TryGetValue(typeof(T), out var list))
            return;

        for (int i = list.Count - 1; i >= 0; i--)
        {
            try
            {
                if      (list[i] is Action<T>      action) action(evt);
                else if (list[i] is Action<object> objAct) objAct(evt!);
            }
            catch (Exception e)
            {
                GD.PrintErr($"[EventBus] Listener threw during Trigger<{typeof(T).Name}>: {e.Message}");
            }
        }
    }

    public static void Trigger<T>() where T : new()
    {
        if (!cachedInstances.TryGetValue(typeof(T), out var evt))
            cachedInstances[typeof(T)] = evt = new T();
        Trigger((T)evt);
    }

    #endregion

    #region Utilities

    public static void Clear()    => listeners.Clear();
    public static void Clear<T>() => listeners.Remove(typeof(T));

    #endregion

    /// <summary>
    /// Infrastructure for <see cref="EventSubscriber.WireEvents"/>.
    /// Not intended for direct use.
    /// </summary>
    public static class Internal
    {
        public static void AddListener(Type eventType, Action<object> listener, Node owner = null)
        {
            if (!listeners.TryGetValue(eventType, out var list))
                listeners[eventType] = list = new();
            list.Add(listener);

            if (owner != null)
                owner.TreeExiting += () => RemoveListener(eventType, listener);
        }

        public static void RemoveListener(Type eventType, Action<object> listener)
        {
            if (listeners.TryGetValue(eventType, out var list))
                list.Remove(listener);
        }
    }
}

