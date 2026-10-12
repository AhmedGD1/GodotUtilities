namespace GodotUtilities.Logic;

public sealed class SimpleStateMachine<T> where T : Enum
{
    public delegate void StateChangedEventHandler(T previousState, T currentState);
    
    public event StateChangedEventHandler StateChanged;

    private readonly Dictionary<T, State<T>> states = [];
    private readonly List<Transition<T>> transitions = [];

    public State<T> CurrentState { get; private set; }
    public State<T> PreviousState { get; private set; }
    
    public double StateElapsed { get; private set; }

    public bool Locked { get; set; } 

    public IReadOnlyDictionary<T, State<T>> States => states;
    public IReadOnlyList<Transition<T>> Transitions => transitions;

    #region Tick

    public void Update(double delta)
    {
        if (CurrentState is null)
            return;

        StateElapsed += delta;

        CurrentState.Update?.Invoke(delta);

        EvaluateTransitions();
    }

    #endregion

    #region Transition Register And Evaluation

    public Transition<T> AddTransition(T from, T to, Func<bool> condition) => AddTransition(from, to, condition, isGlobal: false);

    public Transition<T> AddGlobalTransition(T to, Func<bool> condition) => AddTransition(default, to, condition, isGlobal: true);

    private Transition<T> AddTransition(T from, T to, Func<bool> condition, bool isGlobal)
    {
        ArgumentNullException.ThrowIfNull(condition);

        if (!states.ContainsKey(from) && !isGlobal)
            throw new ArgumentException($"No state registered for id '{from}'", nameof(from));

        if (!states.ContainsKey(to))
            throw new ArgumentException($"No state registered for id '{to}'", nameof(to));

        var transition = new Transition<T>(from, to, condition, isGlobal);
        transitions.Add(transition);
        return transition;
    }

    private void EvaluateTransitions()
    {
        if (Locked || CurrentState is null)
            return;

        var currentId = CurrentState.Id;

        foreach (var transition in transitions)
        {
            if (!EqualityComparer<T>.Default.Equals(transition.From, currentId) && !transition.IsGlobal)
                continue;

            if (transition.Condition())
            {
                ChangeState(transition.To);
                return;
            }
        }
    }

    #endregion

    #region State Manipultaion

    public State<T> AddState(T id)
    {
        if (states.ContainsKey(id))
            throw new ArgumentException($"State with id '{id}' registered already");

        var state = new State<T>(id);
        states[id] = state;
        return state;
    }

    public void ChangeState(T id)
    {
        if (Locked)
            return;
    
        if (!states.ContainsKey(id))
            throw new ArgumentException($"No state registered for id '{id}'", nameof(id));

        if (EqualityComparer<T>.Default.Equals(id, CurrentState.Id))
            return;

        SetState(id);
    }

    public void SetInitialState(T id)
    {
        if (CurrentState is not null)
            throw new InvalidOperationException($"{nameof(SetInitialState)}() can't be called more than once.");

        if (!states.ContainsKey(id))
            throw new ArgumentException($"No state registered for id '{id}'", nameof(id));

        SetState(id);
    }

    private void SetState(T id)
    {
        CurrentState?.Exit?.Invoke();
        PreviousState = CurrentState;

        StateElapsed = 0.0;

        CurrentState = states[id];
        CurrentState.Enter?.Invoke();

        if (PreviousState is not null)
            StateChanged?.Invoke(PreviousState.Id, CurrentState.Id);
    }

    #endregion

    #region Queries

    public T GetCurrentId() => CurrentState is not null ? CurrentState.Id : default;

    public T GetPreviousId() => PreviousState is not null ? PreviousState.Id : default;

    public bool IsInState(T id)
    {
        return CurrentState is not null && EqualityComparer<T>.Default.Equals(CurrentState.Id, id);
    }

    #endregion
}

public sealed class State<T>(T id) where T : Enum
{
    public T Id { get; } = id;
    
    internal Action<double> Update { get; private set; }
    internal Action Enter { get; private set; }
    internal Action Exit { get; private set; }

    public State<T> OnUpdate(Action<double> action)
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

public sealed record Transition<T>(T From, T To, Func<bool> Condition, bool IsGlobal);

