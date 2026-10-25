using Godot;

namespace GodotUtilities;

public static class AnimatedSprite2DExtension
{
    public static SignalAwaiter WaitToFinish(this AnimatedSprite2D sprite)
    {
        return sprite.ToSignal(sprite, AnimatedSprite2D.SignalName.AnimationFinished);
    }
    
    /// <summary>
    /// Plays the animation with key name as long as it exist.
    /// </summary>
    /// <param name="sprite"></param>
    /// <param name="animName"></param>
    /// <param name="customSpeed"></param>
    /// <param name="fromEnd"></param>
    /// <returns></returns>
    public static bool PlayIfExist(this AnimatedSprite2D sprite, StringName animName, float customSpeed = 1, bool fromEnd = false)
    {
        if (!sprite.SpriteFrames.HasAnimation(animName))
            return false;

        sprite.Play(animName, customSpeed, fromEnd);
        return true;
    }

    /// <summary>
    /// Pass frames resource and play the animation with key name.
    /// </summary>
    /// <param name="sprite"></param>
    /// <param name="animName"></param>
    /// <param name="frames"></param>
    /// <param name="customSpeed"></param>
    /// <param name="fromEnd"></param>
    public static void PlayFrames(this AnimatedSprite2D sprite, StringName animName, SpriteFrames frames, float customSpeed = 1, bool fromEnd = false)
    {
        if (sprite.SpriteFrames != frames)
            sprite.SpriteFrames = frames;

        sprite.Play(animName, customSpeed, fromEnd);
    }
}
