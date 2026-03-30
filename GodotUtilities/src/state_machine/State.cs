using System.Collections.Generic;
using System;
using Godot;

namespace Utilities.FSM;

public partial class State<T> where T : Enum
{
    public T Id                            { get; private set; }
    public T TimeoutId                     { get; private set; }

    public Action<float> Update            { get; private set; }
    public Action Enter                    { get; private set; }
    public Action Exit                     { get; private set; }
    public Action TimeoutCallback          { get; private set; }

    public float MinTime                   { get; private set; }
    public float Timeout                   { get; private set; }

    public List<Transition<T>> Transitions { get; private set; }

    public State(T id)
    {
        Id = id;
        Timeout = -1f;

        Transitions = new();
    }

    public State<T> OnUpdate(Action<float> callback)
    {
        Update = callback;
        return this;
    }

    public State<T> OnEnter(Action callback)
    {
        Enter = callback;
        return this;
    }

    public State<T> OnExit(Action callback)
    {
        Exit = callback;
        return this;
    }

    /// <summary>
    /// Separated timeout call. Can be usedfull than using Exit() method sometimes
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    public State<T> OnTimeout(Action callback)
    {
        TimeoutCallback = callback;
        return this;
    }
    //---------------------------------------------------

    /// <summary>
    /// Call it to make state exit automatically after a specific duration
    /// </summary>
    /// <param name="duration"> how long we will stay in this state </param>
    /// <param name="to"> where are we going after timeout ?</param>
    /// <returns></returns>
    public State<T> TimeoutAfter(float duration, T to)
    {
        Timeout = Mathf.Max(0f, duration);
        TimeoutId = to;
        return this;
    }
    //-------------------------------------------------

    /// <summary>
    /// How long you must stay in that state before leaving again
    /// </summary>
    /// <param name="duration"></param>
    /// <returns></returns>
    public State<T> MinDuration(float duration)
    {
        if (duration <= 0f)
            GD.PushWarning($"Invalid Minimum Time duration. Value: -> {duration} <- should be greater than zero");

        MinTime = Mathf.Max(0f, duration);
        return this;
    }

    /// <summary>
    /// internal method for transitions addition, call stateMachine.AddTransition() instead
    /// </summary>
    /// <param name="to"></param>
    /// <returns></returns>
    internal Transition<T> AddTransition(T to, int insertionCounter)
    {
        var transition = new Transition<T>(Id, to);
        Transitions.Add(transition);
        transition.InsertionIndex = insertionCounter;
        return transition;
    }   
}

