using System.Collections.Generic;
using System;

namespace Utilities.FSM;

public partial class StateMachine<T>
{
    private readonly Dictionary<string, List<Action>> eventListeners = new();
    private readonly Queue<string> pendingEvents = new();

    #region Trigger Event
    public void TriggerEvent(string eventName)
    {
        pendingEvents.Enqueue(eventName);
    }

    public void TriggerEvent<TEnum>(TEnum eventName)  where TEnum : Enum => 
        TriggerEvent(eventName.ToString());
    #endregion
    
    #region On Event
    public void OnEvent(string eventName, Action callback)
    {
        if (!eventListeners.ContainsKey(eventName))
            eventListeners[eventName] = new();
        eventListeners[eventName].Add(callback);
    }

    public void OnEvent<TEnum>(TEnum eventName, Action callback) where TEnum : Enum =>
        OnEvent(eventName.ToString(), callback);
    #endregion

    #region Remove Event
    public void RemoveEvent(string eventName, Action callback)
    {
        if (eventListeners.TryGetValue(eventName, out var list))
            list.Remove(callback);
    }

    public void RemoveEvent<TEnum>(TEnum eventName, Action callback) where TEnum : Enum =>
        RemoveEvent(eventName.ToString(), callback);
    #endregion

    #region Events Update
    private void ProcessEvents()
    {
        while (pendingEvents.Count > 0)
        {
            string eventName = pendingEvents.Dequeue();

            if (eventListeners.TryGetValue(eventName, out var list))
                for (int i = list.Count - 1; i >= 0; i--)
                    list[i]?.Invoke();
            
            CheckEventTransitions(eventName);
        }
    }
    
    private void CheckEventTransitions(string eventName)
    {
        foreach (var transition in cachedTransitions)
        {
            if (transition.EventName != eventName) continue;
            if (transition.Guard != null && !transition.Guard()) continue;

            transition.OnTransition?.Invoke();
            TransitionTo(transition.To);
            return;
        }
    }
    #endregion
}

