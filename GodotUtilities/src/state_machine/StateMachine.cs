using System.Collections.Generic;
using System;
using Godot;

namespace Utilities.FSM;

public partial class StateMachine<T> where T : Enum
{
    public event Action<T> StateTimeout;
    public event Action<T, T> StateChanged;

    public bool HasPreviousState => !previousId.Equals(default(T));
    public float StateTime       => stateTime;

    public T CurrentStateId      => currentState != null ? currentState.Id : default;
    public T PreviousId          => previousId;

    private readonly Dictionary<T, State<T>> states        = new();
    private readonly List<Transition<T>> globalTransitions = new();
    private readonly List<Transition<T>> cachedTransitions = new();

    private State<T> currentState;

    private T initialId;
    private T previousId;

    private float stateTime;

    private bool initialized;
    private bool locked;
    private bool paused;

    private int transitionCounter = 0;

    private bool ValidateId(T id)
    {
        if (!states.ContainsKey(id))
        {
            GD.PushError("Invalid State Id");
            return false;
        }
        return true;
    }

    private void TransitionTo(T id, bool bypassExit = false, bool respectLocked = true)
    {   
        if (!ValidateId(id))
            return;
        
        if (locked && respectLocked)
            return;
    
        if (!bypassExit && currentState != null)
            currentState.Exit?.Invoke();
    
        if (currentState != null)
            previousId = currentState.Id;

        currentState = states[id];
        stateTime = 0f;
        currentState.Enter?.Invoke();

        StateChanged?.Invoke(previousId, currentState.Id);

        UpdateCachedTransitions();
    }

    #region Initialization
    public void Start()
    {
        if (!initialized)
        {
            GD.PushError("invalid initial id, call SetInitialState() first");
            return;
        }
        TransitionTo(initialId, bypassExit: true);
    }

    public void SetInitialState(T id)
    {
        if (!states.ContainsKey(id))
        {
            GD.PushError($"invalid state id: {id}");
            return;
        }
        initialId = id;
        initialized = true;
    }

    public void Reset()
    {
        TransitionTo(initialId, respectLocked: false);
    }
    #endregion

    #region S & T Update
    public void UpdateStates(double dt) => UpdateStates((float)dt);
    
    public void UpdateStates(float dt)
    {
        if (currentState == null || paused)
            return;
        
        currentState.Update?.Invoke(dt);
        stateTime += dt;

        if (locked)
            return;

        if (OnStateTimeout())
            return;

        ProcessEvents();
        UpdateTransitions(cachedTransitions);
    }

    private void UpdateTransitions(IEnumerable<Transition<T>> transitions)
    {
        foreach (var transition in transitions)
        {
            if (transition.Guard != null && !transition.Guard())
                continue;
            
            float requiredTime = transition.OverrideMinTime > 0f ? transition.OverrideMinTime : currentState.MinTime;
            bool minTimeExceeded = stateTime > requiredTime || transition.ForceInstantTransition;

            if (!minTimeExceeded)
                continue;
        
            if (transition.Condition?.Invoke() ?? false)
            {
                transition.OnTransition?.Invoke();
                TransitionTo(transition.To);
                return;
            }
        }
    }

    private bool OnStateTimeout()
    {
        if (currentState.Timeout == -1f || stateTime < currentState.Timeout)
            return false;
    
        var fromId = currentState.Id;
        var timeoutId = currentState.TimeoutId;

        if (!states.ContainsKey(timeoutId))
        {
            GD.PushError($"Invalid timeout state id: {timeoutId}");
            return false;
        }

        currentState.TimeoutCallback?.Invoke();
        TransitionTo(timeoutId);
        StateTimeout?.Invoke(fromId);

        return true;
    }

    private void UpdateCachedTransitions()
    {
        if (currentState == null) return;
        
        cachedTransitions.Clear();
        cachedTransitions.AddRange(currentState.Transitions);
        cachedTransitions.AddRange(globalTransitions);
        cachedTransitions.Sort(Transition<T>.Compare);
    }
    #endregion
    
    #region S & T Implementation
    public State<T> AddState(T id)
    {
        if (states.TryGetValue(id, out State<T> value))
            return value;

        var state = new State<T>(id);
        states[id] = state;

        return state;
    }

    public Transition<T> AddTransition(T from, T to)
    {
        var transition = states[from].AddTransition(to, transitionCounter++);
        UpdateCachedTransitions();
        return transition;
    }

    public Transition<T> AddGlobalTransition(T to)
    {
        var transition = new Transition<T>(default, to) { InsertionIndex = transitionCounter++ };
        globalTransitions.Add(transition);
        
        UpdateCachedTransitions();
        return transition;
    }
    #endregion

    #region Manual Control
    public bool CanTransitionTo(T id)
    {
        if (currentState == null) return false;

        return ValidateId(id) && stateTime > currentState.MinTime && !locked;
    }


    public bool TryTransitionTo(T id)
    {
        if (!CanTransitionTo(id)) return false;
        TransitionTo(id);
        return true;
    }

    public void ForceTransitionTo(T id)
    {
        TransitionTo(id, respectLocked: false);
    }
    #endregion

    #region Queries
    public bool IsCurrentState(T id) => currentState != null && currentState.Id.Equals(id);

    public void Pause() => paused = true;
    public void Resume() => paused = false;

    public void Lock() => locked = true;
    public void Unlock() => locked = false;

    public float GetRemainingTime() => currentState?.Timeout > 0f ? Mathf.Max(0f, currentState.Timeout - stateTime) : -1f;
    #endregion
    
}