using Godot;

namespace GodotUtilities;

public static class AnimationPlayerExtensions
{
    private static readonly StringName ANIM_RESET = "RESET";

    public static void ResetAndPlay(this AnimationPlayer animationPlayer, StringName animation, double customBlend = -1, float customSpeed = 1f, bool fromEnd = false)
    {
        animationPlayer.Play(ANIM_RESET);
        animationPlayer.Seek(0, true);
        animationPlayer.Play(animation, customBlend, customSpeed, fromEnd);
    }

    public static bool PlayIfExist(this AnimationPlayer animationPlayer, StringName animation, double customBlend = -1, float customSpeed = 1f, bool fromEnd = false)
    {
        if (!animationPlayer.HasAnimation(animation))
            return false;
        animationPlayer.Play(animation, customBlend, customSpeed, fromEnd);
        return true;
    }
}
