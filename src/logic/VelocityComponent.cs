using Utilities.Events;
using Godot;
using System;

namespace Utilities.Logic;

[GlobalClass]
public partial class VelocityComponent : Node
{
    public const float FALL_THRESHOLD = 0.01f;

    public record struct Jumped(int JumpsUsed);

    public record struct Fell();
    public record struct Landed();
    public record struct GravitySwitched();
    public record struct MotionModeSwitched();

    [Export(PropertyHint.Range, "10, 600")]
    private float maxSpeed = 100f;

    [Export(PropertyHint.Range, "0.1, 100")]
    private float mass = 1f;

    [ExportGroup("Control")]

    [Export(PropertyHint.Range, "1, 300")]
    private float acceleration = 50;

    [Export(PropertyHint.Range, "1, 320")]
    private float deceleration = 60;

    [ExportSubgroup("Air Control")]

    [Export(PropertyHint.Range, "0.1, 100")]
    private float airAcceleration = 30f;

    [Export(PropertyHint.Range, "0, 100")]
    private float airDeceleration = 30f;

    [ExportGroup("Jump")]

    [Export(PropertyHint.Range, "10, 120")]
    private float JumpHeight     { get => jumpHeight; set { jumpHeight = value; ForceUpdateJumpValues(); } }

    [Export(PropertyHint.Range, "0.05, 2")]
    private float JumpTimeToPeak { get => timeToPeak; set { timeToPeak = value; ForceUpdateJumpValues(); } }

    [Export(PropertyHint.Range, "0.05, 2")]
    private float JumpTimeToDrop { get => timeToDrop; set { timeToDrop = value; ForceUpdateJumpValues(); } }

    [Export(PropertyHint.Range, "1, 5")]
    private int maxJumps = 1;

    [Export(PropertyHint.Range, "0.05, 0.5")]
    private float coyoteTime = 0.15f;

    [Export(PropertyHint.Range, "0.05, 0.5")]
    private float jumpBufferTime = 0.15f;

    [ExportGroup("Gravity")]

    [Export(PropertyHint.Range, "20, 1000")]
    private float maxFallSpeed = 500f;
    
    [Export(PropertyHint.Range, "0.1, 10")]
    private float gravityMultiplier = 1f;

    public Vector2 Velocity => velocity;

    private CharacterBody2D controller;

    private Cooldown coyoteTimer;
    private Cooldown jumpBufferTimer;

    private Vector2 velocity;

    private float jumpHeight = 40f;
    private float timeToPeak = 0.15f;
    private float timeToDrop = 0.15f;

    private float jumpGravity;
    private float fallGravity;
    private float jumpVelocity;
    private float verticalSpeed;

    private bool gravityActive;
    private bool isGrounded;
    private bool isFalling;
    private bool jumpValuesUpdated;

    private int jumpsUsed;

    public override void _Ready()
    {
        controller = GetOwnerOrNull<CharacterBody2D>()
            ?? throw new System.Exception("[VelocityComponent] Owner must be a CharacterBody2D.");

        coyoteTimer     = new Cooldown(coyoteTime);
        jumpBufferTimer = new Cooldown(jumpBufferTime);

        TryUpdateJumpValues();
        SetGravityActive(controller.MotionMode == CharacterBody2D.MotionModeEnum.Grounded);
    }

    public override void _PhysicsProcess(double delta)
    {
        coyoteTimer.Tick(delta);
        jumpBufferTimer.Tick(delta);

        float currentGravity = isFalling ? fallGravity : jumpGravity;

        if (gravityActive)
            controller.ApplyGravity(currentGravity * gravityMultiplier, delta, maxFallSpeed);

        bool wasGrounded = isGrounded;

        controller.Velocity = velocity;
        controller.MoveAndSlide();
        
        velocity       = controller.Velocity;
        verticalSpeed  = velocity.Dot(controller.UpDirection);

        isGrounded     = controller.IsOnFloor();
        isFalling      = verticalSpeed < FALL_THRESHOLD;

        if (wasGrounded && isFalling)
        {
            EventBus.Trigger<Fell>();
            AcquireCoyote();
        }

        if (!wasGrounded && isGrounded)
        {
            EventBus.Trigger<Landed>();
            ResetJumps();
        }
    }

    #region Movement

    public void ApplyMovement(Vector2 dir, float dt, float speed, float weight)
    {
        Vector2 desired = dir.NormalizeIfNotZero() * speed;
        Vector2 lerped  = MathUtil.ExponentialLerp(velocity, desired, dt, weight);

        // only updates x axis if motion mode "is grounded mode" -> platformer
        // updates all axes if motion mode is "floating mode" -> air enemies or top-down movement
        velocity = controller.MotionMode switch
        {
            CharacterBody2D.MotionModeEnum.Floating => lerped,
            CharacterBody2D.MotionModeEnum.Grounded => velocity with { X = lerped.X },
            _                                       => lerped  
        };
    }

    public void Move(Vector2 dir, float dt)
    {
        if (dir.LengthSquared() < 0.01f) Decelerate(dt);
        else                             Accelerate(dir, dt);
    }

    public void AccelerateWithSpeed(Vector2 dir, float dt, float speed) =>
        ApplyMovement(dir, dt, speed, isGrounded ? acceleration : airAcceleration);

    public void AccelerateScaled(Vector2 dir, float dt, float scale) =>
        AccelerateWithSpeed(dir, dt, maxSpeed * scale);

    public void Accelerate(Vector2 dir, float dt) =>
        AccelerateWithSpeed(dir, dt, maxSpeed);

    public void Decelerate(float dt) =>
        ApplyMovement(Vector2.Zero, dt, maxSpeed, isGrounded ? deceleration : airDeceleration);

    #endregion

    #region Jump

    private bool InvokeJump()
    {
        Jump();
        return true;
    }

    public bool CanJump()         => HasCoyote() || jumpsUsed < maxJumps;
    public bool CanJumpBuffered() => CanJump() && HasBufferedJump();

    public bool TryJump()         => CanJump()         && InvokeJump();
    public bool TryJumpBuffered() => CanJumpBuffered() && InvokeJump();

    public void Jump()
    {
        TryUpdateJumpValues();

        velocity = velocity with { Y = jumpVelocity };
        
        ConsumeBufferJump();
        ConsumeCoyote();

        jumpsUsed++;
        EventBus.Trigger(new Jumped(jumpsUsed));
    }

    public void AcquireCoyote()     =>  coyoteTimer.Start();
    public void ConsumeCoyote()     =>  coyoteTimer.Stop();
    public bool HasCoyote()         => !coyoteTimer.IsReady;

    public void BufferJump()        =>  jumpBufferTimer.Start();
    public void ConsumeBufferJump() =>  jumpBufferTimer.Stop();
    public bool HasBufferedJump()   => !jumpBufferTimer.IsReady;

    #endregion

    #region Gravity

    public void SwitchGravity()
    {
        controller.UpDirection *= -1f;
        ForceUpdateJumpValues();
        EventBus.Trigger<GravitySwitched>();
    }

    public void SetGravityDirection(float dir)
    {
        float oldDir           = controller.UpDirection.Y;
        controller.UpDirection = new Vector2(0f, Mathf.Sign(dir));
        ForceUpdateJumpValues();

        if (controller.UpDirection.Y != oldDir)
            EventBus.Trigger<GravitySwitched>();
    }

    #endregion

    #region Motion Mode

    public void SwitchMotionMode()
    {
        controller.MotionMode = controller.MotionMode == CharacterBody2D.MotionModeEnum.Grounded 
            ? CharacterBody2D.MotionModeEnum.Floating
            : CharacterBody2D.MotionModeEnum.Grounded;
        
        EventBus.Trigger<MotionModeSwitched>();
        ForceUpdateJumpValues();
    }

    public void SetMotionMode(CharacterBody2D.MotionModeEnum mode)
    {
        var lastValue         = controller.MotionMode;
        controller.MotionMode = mode;

        if (controller.MotionMode != lastValue)
        {
            EventBus.Trigger<MotionModeSwitched>();
            ForceUpdateJumpValues();
        }
    }

    #endregion

    #region Calculations

    private void TryUpdateJumpValues()
    {
        if (jumpValuesUpdated)
            return;
        ForceUpdateJumpValues();
    }

    private void ForceUpdateJumpValues()
    {
        jumpGravity  = 2f * jumpHeight / (timeToPeak * timeToPeak);
        fallGravity  = 2f * jumpHeight / (timeToDrop * timeToDrop);

        jumpVelocity = Mathf.Sqrt(2f * jumpGravity * jumpHeight) * controller.UpDirection.Y;

        jumpValuesUpdated = true;
    }

    #endregion

    #region Utilities

    public void Stop()                            => velocity = Vector2.Zero;

    public void SetVelocity(Vector2 value)        => velocity = value;
    public void SetVelocity(float x, float y)     => velocity = new Vector2(x, y);

    public void SetVelocityX(float value)         => velocity.X = value;
    public void SetVelocityY(float value)         => velocity.Y = value;

    public void AddImpulse(Vector2 impulse)       => velocity += impulse / mass;
    public void AddForce(Vector2 force, float dt) => velocity += force / mass * dt;

    public void ResetJumps()                      => jumpsUsed = 0;
    public void SetMaxJumps(int value)            => maxJumps  = Mathf.Max(1, value);

    public void RestoreJump()                     => RestoreJump(1);
    public void ConsumeJump()                     => ConsumeJump(1);

    public void RestoreJump(int count)            => jumpsUsed = Mathf.Max(0,        jumpsUsed - count);
    public void ConsumeJump(int count)            => jumpsUsed = Mathf.Min(maxJumps, jumpsUsed + count);

    public float GetVerticalSpeed()              => verticalSpeed;
    public float GetFallSpeed()                  => verticalSpeed < 0f ? Mathf.Abs(verticalSpeed) : 0f;

    public bool IsGrounded()                     => isGrounded;
    public bool IsFalling()                      => isFalling;

    public void SetGravityActive(bool value)
    {
        if (controller.MotionMode == CharacterBody2D.MotionModeEnum.Floating)
            throw new Exception("[VelocityComponent] trying to change gravity active although gravity isn't required for floating mode");
        
        gravityActive = value;
    }

    #endregion
}

