namespace GodotUtilities.Logic;

/// <summary>
/// A conditional transition between states of a <see cref="SimpleStateMachine{T}"/>.
/// </summary>
/// <typeparam name="T">The enum type that identifies states.</typeparam>
/// <param name="From">The state the transition leaves. Ignored when <paramref name="IsGlobal"/> is <c>true</c>.</param>
/// <param name="To">The state the transition enters.</param>
/// <param name="Condition">Returns <c>true</c> when the transition should be taken.</param>
/// <param name="IsGlobal">Whether the transition can be taken from any state.</param>
public sealed record Transition<T>(T From, T To, Func<bool> Condition, bool IsGlobal);
