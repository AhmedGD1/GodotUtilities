using Godot;

namespace GodotUtilities;

public static class AnimationPlayerExtension
{
    private static readonly StringName ANIM_RESET = "RESET";

    public static void ResetAndPlay(this AnimationPlayer player, StringName animation, double customBlend = -1, float customSpeed = 1f, bool fromEnd = false)
    {
        player.PlayReset();
        player.Play(animation, customBlend, customSpeed, fromEnd);
    }

    public static bool PlayIfExist(this AnimationPlayer player, StringName animation, double customBlend = -1, float customSpeed = 1f, bool fromEnd = false)
    {
        if (!player.HasAnimation(animation))
            return false;
        player.Play(animation, customBlend, customSpeed, fromEnd);
        return true;
    }

    public static void PlayReset(this AnimationPlayer player, double customBlend = -1, float customSpeed = 1f, bool fromEnd = false)
    {
        player.Play(ANIM_RESET, customBlend, customSpeed, fromEnd);
        player.Seek(0, true);
    }

    public static SignalAwaiter WaitToFinish(this AnimationPlayer player)
    {
        return player.ToSignal(player, AnimationMixer.SignalName.AnimationFinished);
    }
}
