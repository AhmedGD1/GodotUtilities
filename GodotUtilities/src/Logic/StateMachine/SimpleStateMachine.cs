namespace GodotUtilities.Logic;

/// <summary>
/// A lightweight state machine driven by an enum. States are registered with optional
/// enter, update, and exit callbacks, and conditional transitions are evaluated after each update.
/// </summary>
/// <remarks>
/// Avoid calling <see cref="ChangeState(T)"/> or <see cref="TryChangeState(T)"/> from enter or exit callbacks.
/// </remarks>
/// <typeparam name="T">The enum type that identifies states.</typeparam>
public sealed class SimpleStateMachine<T> where T : Enum
{
    /// <summary>
    /// Represents the method that handles <see cref="StateChanged"/>.
    /// </summary>
    /// <param name="previousState">The state that was exited.</param>
    /// <param name="currentState">The state that was entered.</param>
    public delegate void StateChangedEventHandler(T previousState, T currentState);
    
    /// <summary>
    /// Raised after the state changes, including when <see cref="ChangeState(T)"/> restarts the current state.
    /// It is not raised for the initial state.
    /// </summary>
    public event StateChangedEventHandler StateChanged;

    /// <summary>The active state, or <c>null</c> if no initial state has been set.</summary>
    public State<T> CurrentState { get; private set; }

    /// <summary>The state that was active before the current one, or <c>null</c> if there hasn't been a change yet.</summary>
    public State<T> PreviousState { get; private set; }
    
    /// <summary>Time in seconds spent in the current state. Reset on every state change.</summary>
    public double StateElapsed { get; private set; }

    /// <summary>Total time in seconds the machine has been updated since its initial state was set.</summary>
    public double TotalElapsed { get; private set; }

    /// <summary>
    /// If <c>true</c>, the state cannot change automatically: transitions are not taken and
    /// <see cref="TryChangeState(T)"/> returns <c>false</c>. <see cref="ChangeState(T)"/> ignores it.
    /// </summary>
    public bool Locked { get; set; } 

    /// <summary>All registered states, keyed by their id.</summary>
    public IReadOnlyDictionary<T, State<T>> States => _states;

    /// <summary>All registered transitions, in the order they are evaluated.</summary>
    public IReadOnlyList<Transition<T>> Transitions => _transitions;

    private readonly Dictionary<T, State<T>> _states = [];
    private readonly List<Transition<T>> _transitions = [];

    #region Tick

    /// <summary>
    /// Advances the state machine: updates the elapsed times, runs the current state's
    /// update callback, then evaluates transitions. Does nothing if no initial state is set.
    /// </summary>
    /// <param name="delta">Elapsed time in seconds since the last update.</param>
    public void Update(double delta)
    {
        if (CurrentState is null)
            return;

        TotalElapsed += delta;
        StateElapsed += delta;

        CurrentState.Update?.Invoke(delta);

        EvaluateTransitions();
    }

    #endregion

    #region Transition Register And Evaluation

    /// <summary>
    /// Adds a transition that is taken when <paramref name="condition"/> returns <c>true</c>
    /// while the machine is in <paramref name="from"/>.
    /// </summary>
    /// <param name="from">The state the transition leaves.</param>
    /// <param name="to">The state the transition enters.</param>
    /// <param name="condition">Evaluated on each update while in <paramref name="from"/>.</param>
    /// <returns>The created transition.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="condition"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="from"/> or <paramref name="to"/> is not a registered state.
    /// </exception>
    public Transition<T> AddTransition(T from, T to, Func<bool> condition) => AddTransition(from, to, condition, isGlobal: false);

    /// <summary>
    /// Adds a transition that can be taken from any state when <paramref name="condition"/>
    /// returns <c>true</c>.
    /// </summary>
    /// <param name="to">The state the transition enters.</param>
    /// <param name="condition">Evaluated on every update.</param>
    /// <returns>The created transition.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="condition"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="to"/> is not a registered state.</exception>
    public Transition<T> AddGlobalTransition(T to, Func<bool> condition) => AddTransition(default, to, condition, isGlobal: true);

    /// <summary>
    /// Adds a transition that is taken once <paramref name="from"/> has been active for
    /// at least <paramref name="duration"/> seconds.
    /// </summary>
    /// <param name="from">The state the transition leaves.</param>
    /// <param name="to">The state the transition enters.</param>
    /// <param name="duration">Time in seconds to spend in <paramref name="from"/> before transitioning.</param>
    /// <returns>The created transition.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="from"/> or <paramref name="to"/> is not a registered state.
    /// </exception>
    public Transition<T> AddTimeoutTransition(T from, T to, double duration) => AddTransition(from, to, () => StateElapsed >= duration);

    private Transition<T> AddTransition(T from, T to, Func<bool> condition, bool isGlobal)
    {
        ArgumentNullException.ThrowIfNull(condition);

        if (!_states.ContainsKey(from) && !isGlobal)
            throw new ArgumentException($"No state registered for id '{from}'", nameof(from));

        if (!_states.ContainsKey(to))
            throw new ArgumentException($"No state registered for id '{to}'", nameof(to));

        var transition = new Transition<T>(from, to, condition, isGlobal);
        _transitions.Add(transition);
        return transition;
    }

    private void EvaluateTransitions()
    {
        if (CurrentState is null)
            return;

        var currentId = CurrentState.Id;

        foreach (var transition in _transitions)
        {
            if (!EqualityComparer<T>.Default.Equals(transition.From, currentId) && !transition.IsGlobal)
                continue;

            if (EqualityComparer<T>.Default.Equals(transition.To, currentId))
                continue;

            if (!CanChangeState(transition.To))
                continue;

            if (transition.Condition() && TryChangeState(transition.To))
                return;
        }
    }

    #endregion

    #region State Manipultaion

    /// <summary>
    /// Registers a new state. Use the returned state to set its callbacks and timing.
    /// </summary>
    /// <param name="id">The id of the new state.</param>
    /// <returns>The created state.</returns>
    /// <exception cref="ArgumentException">A state with this id is already registered.</exception>
    public State<T> AddState(T id)
    {
        if (_states.ContainsKey(id))
            throw new ArgumentException($"State with id '{id}' registered already");

        var state = new State<T>(id);
        _states[id] = state;
        return state;
    }

    /// <summary>
    /// Switches to the given state only if <see cref="CanChangeState(T)"/> allows it, running the exit
    /// callback of the old state and the enter callback of the new one. Never throws.
    /// </summary>
    /// <param name="id">The id of the state to switch to.</param>
    /// <returns>
    /// <c>true</c> if the state changed; <c>false</c> if the change was refused (see <see cref="CanChangeState(T)"/>).
    /// </returns>
    public bool TryChangeState(T id)
    {
        if (!CanChangeState(id))
            return false;

        SetState(id);
        return true;
    }

    /// <summary>
    /// Switches to the given state unconditionally, running the exit callback of the old state and the
    /// enter callback of the new one. Ignores <see cref="Locked"/>, the current state's minimum duration
    /// and the target's cooldown. Changing to the current state restarts it (exit, reset
    /// <see cref="StateElapsed"/>, enter) and raises <see cref="StateChanged"/> with identical ids.
    /// </summary>
    /// <param name="id">The id of the state to switch to.</param>
    /// <exception cref="InvalidOperationException">No initial state has been set.</exception>
    /// <exception cref="ArgumentException">No state is registered for <paramref name="id"/>.</exception>
    public void ChangeState(T id)
    {
        if (CurrentState is null)
            throw new InvalidOperationException($"{nameof(SetInitialState)}() must be called before {nameof(ChangeState)}().");

        if (!_states.ContainsKey(id))
            throw new ArgumentException($"No state registered for id '{id}'", nameof(id));

        SetState(id);
    }

    /// <summary>
    /// Sets the starting state and runs its enter callback. Can only be called once.
    /// <see cref="StateChanged"/> is not raised.
    /// </summary>
    /// <param name="id">The id of the starting state.</param>
    /// <exception cref="InvalidOperationException">An initial state was already set.</exception>
    /// <exception cref="ArgumentException">No state is registered for <paramref name="id"/>.</exception>
    public void SetInitialState(T id)
    {
        if (CurrentState is not null)
            throw new InvalidOperationException($"{nameof(SetInitialState)}() can't be called more than once.");

        if (!_states.ContainsKey(id))
            throw new ArgumentException($"No state registered for id '{id}'", nameof(id));

        SetState(id);
    }

    private void SetState(T id)
    {
        if (CurrentState is not null)
        {
            CurrentState.Exit?.Invoke();
            CurrentState.CapturedElapsed = TotalElapsed;
        }

        PreviousState = CurrentState;
        StateElapsed = 0.0;

        CurrentState = _states[id];
        CurrentState.Enter?.Invoke();

        if (PreviousState is not null)
            StateChanged?.Invoke(PreviousState.Id, CurrentState.Id);
    }

    #endregion

    #region Queries

    /// <summary>
    /// Checks whether the machine can switch to the given state right now. This requires the machine
    /// to be unlocked and started, the current state's minimum duration to have passed,
    /// the target to be registered, the target's cooldown to have elapsed, and the target to be
    /// a different state from the current one. <see cref="TryChangeState(T)"/> and transition
    /// evaluation use it; <see cref="ChangeState(T)"/> does not.
    /// </summary>
    /// <param name="to">The id of the target state.</param>
    /// <returns><c>true</c> if a change to <paramref name="to"/> is allowed; otherwise <c>false</c>.</returns>
    public bool CanChangeState(T to)
    {
        return !Locked 
            && CurrentState is not null 
            && CurrentState.CanExit(StateElapsed)
            && _states.TryGetValue(to, out var nextState) 
            && nextState.CanEnter(TotalElapsed)
            && !IsInState(to);
    }

    /// <summary>Gets the id of the current state.</summary>
    /// <returns>The current state's id, or <c>default</c> if no initial state is set.</returns>
    public T GetCurrentId() => CurrentState is not null ? CurrentState.Id : default;

    /// <summary>Gets the id of the previous state.</summary>
    /// <returns>The previous state's id, or <c>default</c> if there hasn't been a state change yet.</returns>
    public T GetPreviousId() => PreviousState is not null ? PreviousState.Id : default;

    /// <summary>Checks whether the machine is currently in the given state.</summary>
    /// <param name="id">The state id to check.</param>
    /// <returns><c>true</c> if <paramref name="id"/> is the current state; otherwise <c>false</c>.</returns>
    public bool IsInState(T id)
    {
        return CurrentState is not null && EqualityComparer<T>.Default.Equals(CurrentState.Id, id);
    }

    /// <summary>Gets a registered state by id.</summary>
    /// <param name="id">The state id to look up.</param>
    /// <returns>The state, or <c>null</c> if none is registered for <paramref name="id"/>.</returns>
    public State<T> GetStateOrNull(T id) => _states.TryGetValue(id, out var state) ? state : null;

    #endregion
}