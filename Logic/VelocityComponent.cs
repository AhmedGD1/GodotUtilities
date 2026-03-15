using Godot;
using System;

namespace Utilities.Logic;

[GlobalClass]
public partial class VelocityComponent : Node
{
    [Signal] public delegate void MotionModeChangedEventHandler(CharacterBody2D.MotionModeEnum mode);
    [Signal] public delegate void GravitySwitchedEventHandler();
    [Signal] public delegate void JumpedEventHandler(int count);
    [Signal] public delegate void LandedEventHandler();
    [Signal] public delegate void FellEventHandler();

    private const float Gravity           = 980f;
    private const float CoyoteTime        = 0.15f;
    private const float JumpBufferingTime = 0.15f;

    [Export] private CharacterBody2D controller;

    [Export(PropertyHint.Range, "10, 1000")] private float maxSpeed = 100f;

    [ExportGroup("Control")]

    [Export(PropertyHint.Range, "1, 200")] private float acceleration = 40f;
    [Export(PropertyHint.Range, "1, 250")] private float deceleration = 60f;

    [ExportSubgroup("Air")]

    [Export(PropertyHint.Range, "1, 200")] private float airAcceleration = 15f;
    [Export(PropertyHint.Range, "0, 250")] private float airDeceleration = 10f;

    [ExportGroup("Jump")]

    [Export(PropertyHint.Range, "5, 200")] private float jumpHeight = 40f;
    [Export(PropertyHint.Range, "1, 10")] private int maxJumps      = 1;

    [Export] private bool autoGrantCoyote = true;

    [ExportGroup("Gravity")]

    [Export(PropertyHint.Range, "0.05, 10")] private float gravityScale      = 1f;
    [Export(PropertyHint.Range, "50, 1500")] private float maxFallSpeed      = 500f;
    [Export(PropertyHint.Range, "1, 2")] private float fallGravityMultiplier = 1f;

    private float CurrentAcceleration => (isFloatingMode || isGrounded) ? acceleration : airAcceleration;
    private float CurrentDeceleration => (isFloatingMode || isGrounded) ? deceleration : airDeceleration;
    private float CurrentGravity      => Gravity * gravityScale * (isFalling ? fallGravityMultiplier : 1f);
    private Vector2 GravityDirection  => -controller.UpDirection;

    private bool isFloatingMode;
    private bool isGrounded;
    private bool isFalling;

    private bool useGravity = true;

    private int jumpsLeft;

    private Cooldown coyoteTimer        = new(CoyoteTime);
    private Cooldown jumpBufferingTimer = new(JumpBufferingTime);

    public override void _Ready()
    {
        SetMotionMode(controller.MotionMode);
    }

    public override void _Process(double delta)
    {
        UpdateTimers(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        bool wasGrounded = isGrounded;

        if (useGravity && !isFloatingMode) 
            controller.ApplyGravity(CurrentGravity, delta, maxFallSpeed);
        controller.MoveAndSlide();

        isGrounded = controller.IsOnFloor();
        isFalling = controller.Velocity.Dot(GravityDirection) > 0.01f;

        if (isGrounded && jumpsLeft != maxJumps)
            ResetJumps();

        if (!wasGrounded && isGrounded)
        {
            EmitSignalLanded();
        }

        if (wasGrounded && isFalling)
        {
            EmitSignalFell();

            if (autoGrantCoyote)
                GetCoyote();
        }
    }

    #region Movement
    public void SetMotionMode(CharacterBody2D.MotionModeEnum mode)
    {
        if (controller.MotionMode != mode)
            EmitSignalMotionModeChanged(mode);

        controller.MotionMode = mode;
        isFloatingMode        = mode == CharacterBody2D.MotionModeEnum.Floating;
    }

    //------------------------------------
    // Acceleration
    //------------------------------------
    public void AccelerateWithSpeed(Vector2 direction, float dt, float speed)
    {
        ApplyMovement(direction, dt, speed, CurrentAcceleration);
    }

    public void AccelerateScaled(Vector2 direction, float dt, float scale)
    {
        ApplyMovement(direction, dt, maxSpeed * scale, CurrentAcceleration);
    }

    public void Accelerate(Vector2 direction, float dt)
    {
        ApplyMovement(direction, dt, maxSpeed, CurrentAcceleration);
    }

    //----------------------------------------
    // Deceleration
    //----------------------------------------
    public void Decelerate(float dt)
    {
        ApplyMovement(Vector2.Zero, dt, maxSpeed, CurrentDeceleration);
    }
    //---------------------------------------
    // Movement Implementation
    //---------------------------------------
    private void ApplyMovement(Vector2 direction, float dt, float speed, float weight)
    {
        Vector2 desired = direction.Normalized() * speed;
        Vector2 value = MathUtil.ExponentialLerp(controller.Velocity, desired, dt, weight);

        controller.Velocity = new Vector2(value.X, isFloatingMode ? value.Y : controller.Velocity.Y);
    }
    #endregion

    #region Jump

    public void SetMaxJumps(int value)
    {
        maxJumps = value;
    }

    public void ResetJumps()
    {
        jumpsLeft = maxJumps;
    }

    public void AddJump() => AddJump(1);

    public void AddJump(int count)
    {
        jumpsLeft = Mathf.Clamp(jumpsLeft + count, 0, maxJumps);
    }

    public bool CanJump() => CanJump(excludeBuffering: false);

    public bool CanJump(bool excludeBuffering)
    {
        bool multiJump = jumpsLeft > 0 && jumpsLeft != maxJumps;
        return (HasCoyote() || isGrounded || multiJump) && (excludeBuffering || HasBufferedJump());
    }

    public bool TryJump() => TryJump(excludeBuffering: false);

    public bool TryJump(bool excludeBuffering)
    {
        if (CanJump(excludeBuffering))
        {
            Jump();
            ConsumeBufferedJump();
            ConsumeCoyote();
            return true;
        }

        return false;
    }

    public void Jump()
    {
        float jumpVelocity = -Mathf.Sqrt(2f * Gravity * gravityScale * jumpHeight);
        controller.Velocity = new Vector2(controller.Velocity.X, jumpVelocity);

        jumpsLeft--;
        EmitSignalJumped(maxJumps - jumpsLeft);
    }

    public bool HasCoyote()               => !coyoteTimer.IsReady;
    public void GetCoyote()               => coyoteTimer.Start();
    public void GetCoyote(float duration) => coyoteTimer.Start(duration);
    public void ConsumeCoyote()           => coyoteTimer.Reset();

    public bool HasBufferedJump()          => !jumpBufferingTimer.IsReady;
    public void BufferJump()               => jumpBufferingTimer.Start();
    public void BufferJump(float duration) => jumpBufferingTimer.Start(duration);
    public void ConsumeBufferedJump()      => jumpBufferingTimer.Reset();

    private void UpdateTimers(double dt)
    {
        coyoteTimer.Tick(dt);
        jumpBufferingTimer.Tick(dt);
    } 

    #endregion

    #region Gravity Manipulation
    public void SwitchGravity()
    {
        if (!ValidateFloatingMode())
            return;
        controller.UpDirection *= -1f;
        EmitSignalGravitySwitched();
    }

    public void SetGravityActive(bool active)
    {
        if (ValidateFloatingMode())
            useGravity = active;
    }

    public bool IsGrounded() => isGrounded;
    public bool IsFalling()  => isFalling;
    #endregion

    private bool ValidateFloatingMode()
    {
        if (isFloatingMode)
        {
            GD.PushWarning("[VelocityComponent] Invalid Call, motion mode must be MotionMode.Grounded to work");
            return false;
        }
        return true;
    }
}
