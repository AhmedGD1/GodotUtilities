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
}

