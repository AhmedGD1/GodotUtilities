namespace GodotUtilities;

public partial class SimpleStateMachine<T> where T : Enum
{
    public delegate void StateChangedEventHandler(T previousState, T currentState);
    
    public event StateChangedEventHandler StateChanged;

    private readonly Dictionary<T, State<T>> states = [];

    public bool Locked { get; set; }

    public State<T> CurrentState { get; private set; }
    public State<T> PreviousState { get; private set; }
    
    public double StateElapsed { get; private set; }

    public void Update(double delta)
    {
        if (CurrentState is null)
            return;

        StateElapsed += delta;

        CurrentState.Update?.Invoke((float)delta);
    }

    public State<T> AddState(T id)
    {
        if (states.ContainsKey(id))
            throw new Exception($"State with id '{id}' registered already");

        var state = new State<T>(id);
        states[id] = state;
        return state;
    }

    public void ChangeState(T toId)
    {
        if (Locked)
            return;
    
        if (!states.TryGetValue(toId, out var newState))
            throw new ArgumentException($"No state registered for id '{toId}'", nameof(toId));

        CurrentState?.Exit?.Invoke();

        PreviousState = CurrentState;
        CurrentState = newState;

        CurrentState.Enter?.Invoke();
        StateElapsed = 0.0;

        if (PreviousState != null)
            StateChanged?.Invoke(PreviousState.Id, CurrentState.Id);
    }

    public void SetInitialState(T id)
    {
        if (!states.TryGetValue(id, out var state))
            throw new ArgumentException($"No state registered for id '{id}'", nameof(id));

        CurrentState = state;
        CurrentState.Enter?.Invoke();
    }

    #region Queries

    public T GetCurrentId() => CurrentState != null ? CurrentState.Id : default;
    public T GetPreviousId() => PreviousState != null ? PreviousState.Id : default;

    #endregion
}

public class State<T>(T id) where T : Enum
{
    public T Id { get; private set; } = id;
    
    internal Action<float> Update { get; private set; }
    internal Action Enter { get; private set; }
    internal Action Exit { get; private set; }

    public State<T> OnUpdate(Action<float> action)
    {
        Update = action;
        return this;
    }

    public State<T> OnEnter(Action action)
    {
        Enter = action;
        return this;
    }
    
    public State<T> OnExit(Action action)
    {
        Exit = action;
        return this;
    }
}
