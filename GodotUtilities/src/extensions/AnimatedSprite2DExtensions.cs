using System.Threading.Tasks;
using Godot;

namespace Utilities;

public static class AnimatedSprite2DExtensions
{
    public static bool PlayIfExist(this AnimatedSprite2D animatedSprite, string animName)
    {
        if (!animatedSprite.SpriteFrames.HasAnimation(animName))
            return false;

        animatedSprite.Play(animName);
        return true;
    }

    public static async Task WaitToFinish(this AnimatedSprite2D animatedSprite)
    {
        await animatedSprite.ToSignal(animatedSprite, AnimatedSprite2D.SignalName.AnimationFinished);
    }

    public static void OnAnimationFinished(this AnimatedSprite2D animatedSprite, Callable callable, bool oneShot = true)
    {
        if (!oneShot)
        {
            animatedSprite.Connect(AnimatedSprite2D.SignalName.AnimationFinished, callable);
            return;
        }

        animatedSprite.Connect(AnimatedSprite2D.SignalName.AnimationFinished, callable, (uint)GodotObject.ConnectFlags.OneShot);
    }
}

