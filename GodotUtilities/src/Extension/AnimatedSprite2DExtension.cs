using Godot;

namespace GodotUtilities;

public static class AnimatedSprite2DExtensions
{
    public static SignalAwaiter WaitToFinish(this AnimatedSprite2D sprite)
    {
        return sprite.ToSignal(sprite, AnimatedSprite2D.SignalName.AnimationFinished);
    }
    
    public static bool TryPlay(this AnimatedSprite2D sprite, StringName animName)
    {
        if (!sprite.SpriteFrames.HasAnimation(animName))
            return false;

        sprite.Play(animName);
        return true;
    }

    public static void PlayFrames(this AnimatedSprite2D sprite, StringName animName, SpriteFrames frames)
    {
        if (sprite.SpriteFrames != frames)
            sprite.SpriteFrames = frames;

        sprite.Play(animName);
    }
}
