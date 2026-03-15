using System.Threading.Tasks;
using System;
using Godot;

namespace Utilities;

public static class TimerUtil
{
    /// <summary>
    /// Quick wrapper for GetTree().CreateTimer() method
    /// </summary>
    /// <param name="seconds"></param>
    /// <param name="callback"></param>
    /// <param name="context"></param>
    public static async void Wait(float seconds, Action callback, Node context)
    {
        await context.GetTree().ToSignal(context.GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
        callback?.Invoke();
    }

    /// <summary>
    /// Same as <code>TimerUtil.Wait()</code> but for async chaining
    /// </summary>
    /// <param name="seconds"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public static async Task WaitAsync(float seconds, Node context)
    {
        await context.GetTree().ToSignal(context.GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    }
}

