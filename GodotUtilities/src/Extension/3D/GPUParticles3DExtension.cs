using Godot;

namespace GodotUtilities;

public static class GPUParticles3DExtension
{
    public static async void EmitTimeout(this GpuParticles3D particles, double duration)
    {
        if (particles.OneShot)
        {
            GD.PushWarning("Can't use timed emission with one shot particles");
            return;
        }

        EmitFresh(particles);
        await particles.GetTree().Wait(duration);

        if (GodotObject.IsInstanceValid(particles))
            particles.Emitting = false;
    }

    public static void EmitFresh(this GpuParticles3D particles)
    {
        particles.Emitting = true;
        particles.Restart();
    }
}
