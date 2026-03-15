using System;
using System.Threading.Tasks;
using Godot;

namespace Utilities;

public static class AnimatedSprite2DExtensions
{
    public static void PlayIfNotAlready(this AnimatedSprite2D animSprite, string animName)
    {
        if (animSprite.Animation != animName)
            animSprite.Play(animName);
    }

    /// <summary>
    /// A quick shortcut method to connect an action with animation finished signal (one shot)
    /// </summary>
    /// <param name="animSprite"></param>
    /// <param name="action"></param>
    public static void OnAnimationFinished(this AnimatedSprite2D animSprite, Action action)
    {
        StringName signalName = AnimatedSprite2D.SignalName.AnimationFinished;

        if (animSprite.IsConnected(signalName, Callable.From(action)))
            return;
        
        animSprite.Connect(signalName, Callable.From(action), (uint)GodotObject.ConnectFlags.OneShot);
    }

    public static async Task WaitToFinish(this AnimatedSprite2D animatedSprite)
    {
        await animatedSprite.ToSignal(animatedSprite, AnimatedSprite2D.SignalName.AnimationFinished);
    }
}

