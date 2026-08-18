using Godot;

namespace GodotUtilities;

public static class GPUParticles2DExtension
{   
    public static void SetDirection(this GpuParticles2D particles, Vector2 direction)
    {
        if (particles.ProcessMaterial is not ParticleProcessMaterial material)
        {
            GD.PushWarning("[GPUParticles2D Extensions] particles doesn't have a process material.");
            return;
        }
        
        material.Direction = new Vector3(direction.X, direction.Y, 0f);
    }

    public static async void EmitTimeout(this GpuParticles2D particles, double duration)
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

    public static void EmitFresh(this GpuParticles2D particles)
    {
        particles.Emitting = true;
        particles.Restart();
    }
}
