using Godot;
using System;

namespace Utilities.FSM;

public partial class Transition<T> where T : Enum
{
    public T From                      { get; private set; }
    public T To                        { get; private set; }

    public Action OnTransition         { get; private set; }

    public Func<bool> Condition        { get; private set; }
    public Func<bool> Guard            { get; private set; }

    public string EventName            { get; private set; }

    public float OverrideMinTime       { get; private set; }

    public bool ForceInstantTransition { get; private set; }

    public int Priority                { get; private set; }
    public int InsertionIndex          { get; internal set; }

    public Transition(T from, T to)
    {
        From = from;
        To   = to;

        OverrideMinTime = -1f;
    }

    public Transition<T> When(Func<bool> condition)
    {
        Condition = condition;
        return this;
    }

    public Transition<T> IfOnly(Func<bool> guard)
    {
        Guard = guard;
        return this;
    }

    public Transition<T> OnEvent(string eventName)
    {
        EventName = eventName;
        return this;
    }

    public Transition<T> OnEvent<TEnum>(TEnum eventName) where TEnum : Enum
    {
        return OnEvent(eventName.ToString());
    }

    public Transition<T> Do(Action callback)
    {
        OnTransition = callback;
        return this;
    }

    /// <summary>
    /// Force Instant method ignores previous state min time
    /// </summary>
    /// <returns></returns>
    public Transition<T> ForceInstant()
    {
        ForceInstantTransition = true;
        return this;
    }

    /// <summary>
    /// How long you must stay in (From) state before going to (To) state
    /// </summary>
    /// <returns></returns>
    public Transition<T> OverrideMinDuration(float duration)
    {
        if (duration <= 0f)
            GD.PushWarning($"Invalid Minimum Time duration. Value: -> {duration} <- should be greater than zero");

        OverrideMinTime = Mathf.Max(0f, duration);
        return this;
    }

    public Transition<T> SetPriority(int priority)
    {
        if (priority < 0)
            GD.PushWarning($"Invalid priority: {priority}, value should be zero or greater");
        Priority = priority;
        return this;
    }

    internal static int Compare(Transition<T> a, Transition<T> b)
    {
        int priorityCompare = b.Priority.CompareTo(a.Priority);
        return priorityCompare != 0 ? priorityCompare : a.InsertionIndex.CompareTo(b.InsertionIndex);
    }
}
