using Utilities.Events;
using System;
using Godot;

namespace Utilities.Logic;

/// <summary>
/// A physics-driven velocity component for <see cref="CharacterBody2D"/> nodes.
/// Handles horizontal movement, jumping, coyote time, jump buffering, apex hanging,
/// gravity scaling, gravity direction flipping, and explosion knockback.
/// </summary>
/// <remarks>
/// Attach this node as a child of a <see cref="CharacterBody2D"/>.
/// The owner must be a <see cref="CharacterBody2D"/> or an exception will be thrown in <see cref="_Ready"/>.
///
/// Consumers drive movement by calling <see cref="Move"/>, <see cref="Accelerate"/>, or <see cref="Decelerate"/>
/// each physics frame. Jumping is handled via <see cref="TryJump"/> or <see cref="TryJumpBuffered"/>.
/// State changes are broadcast through <see cref="EventBus"/> using the nested record structs.
/// </remarks>
[GlobalClass]
public partial class VelocityComponent : Node
{
    /// <summary>Defines the active gravity direction.</summary>
    public enum GravityState
    {
        /// <summary>Standard gravity pulling downward. <see cref="CharacterBody2D.UpDirection"/> is <see cref="Vector2.Up"/>.</summary>
        Floor,
        /// <summary>Inverted gravity pulling upward. <see cref="CharacterBody2D.UpDirection"/> is <see cref="Vector2.Down"/>.</summary>
        Ceiling
    }

    /// <summary>Controls how explosion force attenuates with distance from the origin.</summary>
    public enum FalloffCurve
    {
        /// <summary>Force decreases linearly from center to edge. Consistent feel across the radius.</summary>
        Linear,
        /// <summary>Force drops off quickly near the edge. Strong near-field, weak at range. Recommended default.</summary>
        Quadratic,
        /// <summary>Very strong near-field with a long gradual tail. Good for environmental hazards.</summary>
        InverseSquare
    }

    /// <summary>Base gravity constant in pixels per second squared. Equivalent to ~9.8 m/s² scaled for pixel units.</summary>
    public const float DEFAULT_GRAVITY = 980f;

    /// <summary>Minimum downward speed in pixels/s before a character is considered falling.</summary>
    public const float FALL_THRESHOLD = 0.1f;

    /// <summary>Fired when the character jumps. Carries the total jump count used in the current airborne sequence.</summary>
    /// <param name="JumpsUsed">Number of jumps consumed since last landing.</param>
    public record struct Jumped(int JumpsUsed);

    /// <summary>Fired on the frame the character starts falling, from any cause.</summary>
    public record struct Fell();

    /// <summary>Fired on the frame the character transitions from grounded to falling without jumping.</summary>
    public record struct FellOffEdge();

    /// <summary>Fired on the frame the character lands after being airborne.</summary>
    public record struct Landed();

    /// <summary>Fired when gravity direction changes via <see cref="SwitchGravity"/> or <see cref="SetGravityState"/>.</summary>
    public record struct GravitySwitched();

    /// <summary>Fired when the controller's <see cref="CharacterBody2D.MotionMode"/> changes.</summary>
    public record struct MotionModeChanged();

    /// <summary>Fired when the jump apex is detected — vertical speed transitions from positive to negative.</summary>
    public record struct ApexReached();

    [Export]
    private GravityState State
    {
        get => gravityState;
        set => SetGravityState(value);
    }

    /// <summary>Maximum horizontal speed in pixels per second.</summary>
    [Export(PropertyHint.Range, "10, 600")]
    private float maxSpeed = 100f;

    /// <summary>
    /// Mass in kilograms. Affects <see cref="AddForce"/> and <see cref="AddImpulse"/> scaling.
    /// Higher mass requires more force to achieve the same velocity change.
    /// </summary>
    [Export(PropertyHint.Range, "0.1, 100")]
    private float mass = 1f;

    [ExportGroup("Control")]

    /// <summary>
    /// Ground acceleration weight. Higher values reach <see cref="maxSpeed"/> faster.
    /// Used by <see cref="MathUtil.ExponentialLerp"/> as the interpolation rate.
    /// </summary>
    [Export(PropertyHint.Range, "1, 300")]
    private float acceleration = 50;

    /// <summary>Ground deceleration weight. Higher values stop faster when no input is given.</summary>
    [Export(PropertyHint.Range, "1, 320")]
    private float deceleration = 60;

    [ExportSubgroup("Air Control")]

    /// <summary>Acceleration weight while airborne. Typically lower than <see cref="acceleration"/> for reduced air control.</summary>
    [Export(PropertyHint.Range, "0.1, 100")]
    private float airAcceleration = 30f;

    /// <summary>Deceleration weight while airborne.</summary>
    [Export(PropertyHint.Range, "0, 100")]
    private float airDeceleration = 30f;

    [ExportGroup("Jump")]

    /// <summary>
    /// Target jump height in pixels. Used with <see cref="DEFAULT_GRAVITY"/> and <see cref="gravityScale"/>
    /// to derive jump speed via <c>sqrt(2gh)</c>.
    /// </summary>
    [Export(PropertyHint.Range, "5, 200")]
    private float jumpHeight = 40f;

    /// <summary>Maximum number of jumps allowed before landing. Set to 2 for double jump.</summary>
    [Export(PropertyHint.Range, "1, 10")]
    private int maxJumps = 1;

    /// <summary>Duration in seconds after leaving a ledge during which the character can still jump.</summary>
    [Export(PropertyHint.Range, "0.05, 0.5")]
    private float coyoteTime = 0.15f;

    /// <summary>
    /// Duration in seconds a jump input is buffered before landing.
    /// If the character lands within this window, the jump fires automatically.
    /// </summary>
    [Export(PropertyHint.Range, "0.05, 0.5")]
    private float jumpBufferTime = 0.15f;

    [ExportSubgroup("Apex")]

    /// <summary>When true, applies reduced gravity and increased air control at the jump apex.</summary>
    [Export]
    private bool enableApexHanging = true;

    /// <summary>
    /// Multiplier applied to <see cref="airAcceleration"/> and <see cref="airDeceleration"/> during apex hang.
    /// Values above 1.0 give the player more horizontal control at the peak of the jump.
    /// </summary>
    [Export(PropertyHint.Range, "1.1, 2")]
    private float apexHorizontalBoost = 1.4f;

    /// <summary>Duration in seconds of the apex hang window.</summary>
    [Export(PropertyHint.Range, "0.02, 0.3")]
    private float apexHangDuration = 0.05f;

    /// <summary>
    /// Gravity multiplier applied during apex hang. Values below 1.0 reduce gravity for a floaty peak.
    /// 0.3 means 30% of normal gravity at the apex.
    /// </summary>
    [Export(PropertyHint.Range, "0.1, 0.9")]
    private float apexGravityReduction = 0.3f;

    [ExportGroup("Gravity")]

    /// <summary>
    /// Global gravity scale applied on top of <see cref="DEFAULT_GRAVITY"/>.
    /// Use to make a character feel heavier or lighter without changing jump height math.
    /// </summary>
    [Export(PropertyHint.Range, "0.1, 20")]
    private float gravityScale = 1f;

    /// <summary>
    /// Additional gravity multiplier applied only when falling. Values above 1.0 make falls snappier.
    /// Does not affect the rising portion of a jump.
    /// </summary>
    [Export(PropertyHint.Range, "0.1, 20")]
    private float fallGravityMultiplier = 1f;

    /// <summary>Terminal fall velocity in pixels per second. Fall speed is clamped to this value each frame.</summary>
    [Export(PropertyHint.Range, "50, 1000")]
    private float maxFallSpeed = 300f;


    private CharacterBody2D controller;
    private Vector2 velocity;

    private Cooldown coyoteTimer     = new();
    private Cooldown jumpBufferTimer = new();

    private float airAccelerationBase;
    private float airDecelerationBase;
    private float weightFactor = 1f;

    private float prevVerticalSpeed;
    private float apexHangTimer;
    private int   jumpsUsed;

    private bool useGravity = true;
    private bool floatingMode;
    private bool wasGrounded;
    private bool wasFalling;
    private bool isGrounded;
    private bool isFalling;
    private bool reachedApex;

    private GravityState gravityState = GravityState.Floor;

    #region Properties

    /// <summary>Returns the number of jumps remaining before landing is required.</summary>
    public int JumpsRemaining => Mathf.Max(0, maxJumps - jumpsUsed);

    /// <summary>Returns the current mass value.</summary>
    public float Mass => mass;

    /// <summary>Returns the configured maximum horizontal speed.</summary>
    public float MaxSpeed => maxSpeed;

    /// <summary>
    /// Returns current movement speed. In floating mode returns full vector magnitude.
    /// In standard mode returns absolute horizontal speed only.
    /// </summary>
    public float CurrentSpeed  => floatingMode ? velocity.Length() : Mathf.Abs(velocity.X);

    /// <summary>Returns vertical speed projected onto <see cref="CharacterBody2D.UpDirection"/>. Positive = rising, negative = falling.</summary>
    public float VerticalSpeed => velocity.Dot(controller.UpDirection);

    /// <summary>Returns the current velocity vector in world space.</summary>
    public Vector2 Velocity => velocity;

    /// <summary>Returns the active gravity state.</summary>
    public GravityState CurrentGravityState => gravityState;

    /// <summary>Returns the active fall speed (0 when player is not falling).</summary>
    public float FallSpeed => isFalling ? velocity.Dot(-controller.UpDirection) : 0f;

    /// <summary>The active fall gravity multiplier. Values above 1.0 make falls snappier.</summary>
    public float FallGravityMultiplier => fallGravityMultiplier;

    /// <summary>Terminal fall speed in pixels per second. Fall velocity is clamped to this each frame.</summary>
    public float MaxFallSpeed => maxFallSpeed;

    /// <summary>
    /// Movement acceleration scale factor. Applied on top of ground and air acceleration weights.
    /// Use to temporarily slow or boost a character without permanently changing their acceleration stats.
    /// </summary>
    public float WeightFactor => weightFactor;

    #endregion

    public override void _Ready()
    {
        controller = GetOwnerOrNull<CharacterBody2D>()
            ?? throw new Exception("[VelocityComponent] Invalid Controller, Make sure that owner is a CharacterBody2D");

        airAccelerationBase = airAcceleration;
        airDecelerationBase = airDeceleration;
    }

    public override void _PhysicsProcess(double delta)
    {
        coyoteTimer.Tick(delta);
        jumpBufferTimer.Tick(delta);

        wasGrounded = isGrounded;
        wasFalling  = isFalling;

        ApplyGravity(delta);
        MoveAndSlide();

        Vector2 gravityDir = -controller.UpDirection;
        float fallSpeed    = velocity.Dot(gravityDir);

        isGrounded = controller.IsOnFloor();
        isFalling = !isGrounded && fallSpeed > FALL_THRESHOLD;

        CheckApex();
        CheckStates();
    }

    #region Internal

    private void MoveAndSlide()
    {
        controller.Velocity = velocity;
        controller.MoveAndSlide();
        velocity = controller.Velocity;
    }

    private void CheckStates()
    {
        if (!wasFalling && isFalling)
        {
            EventBus.Trigger<Fell>();   

            if (!reachedApex)
            {
                EventBus.Trigger<FellOffEdge>();
                AcquireCoyote();
                ConsumeJump();
            }
        }

        if (!wasGrounded && isGrounded)
        {
            EventBus.Trigger<Landed>();
            ResetJumps();
        }
    }

    private void CheckApex()
    {
        float verticalSpeed = velocity.Dot(controller.UpDirection);

        bool wasGoingDown = prevVerticalSpeed < 0f;
        bool wasGoingUp   = prevVerticalSpeed > 0f;
        bool nowGoingUp   = verticalSpeed     > 0f;
        bool nowGoingDown = verticalSpeed     <= 0f;

        if (!reachedApex && wasGoingUp && nowGoingDown)
        {
            reachedApex = true;
            OnApex();
        }

        bool reset = (reachedApex && wasGoingDown && nowGoingUp) || isGrounded;

        if (reset)
            reachedApex = false;

        prevVerticalSpeed = verticalSpeed;
    }

    private void OnApex()
    {
        if (enableApexHanging)
            apexHangTimer = apexHangDuration;
        EventBus.Trigger<ApexReached>();
    }

    #endregion

    #region Movement

    /// <summary>
    /// Accelerates toward <paramref name="dir"/> at a custom <paramref name="speed"/> cap.
    /// Uses ground or air acceleration depending on current state.
    /// </summary>
    /// <param name="dir">Desired movement direction. Does not need to be normalized.</param>
    /// <param name="dt">Physics delta time in seconds.</param>
    /// <param name="speed">Target speed cap in pixels per second.</param>
    public void AccelerateWithSpeed(Vector2 dir, float dt, float speed) =>
        ApplyMovement(dir, dt, speed, (isGrounded || floatingMode) ? acceleration : airAcceleration);

    /// <summary>
    /// Accelerates toward <paramref name="dir"/> at a fraction of <see cref="maxSpeed"/>.
    /// Useful for movement abilities that cap at a percentage of full speed.
    /// </summary>
    /// <param name="dir">Desired movement direction.</param>
    /// <param name="dt">Physics delta time in seconds.</param>
    /// <param name="scale">Speed scale from 0.0 to 1.0.</param>
    public void AccelerateScaled(Vector2 dir, float dt, float scale) =>
        AccelerateWithSpeed(dir, dt, maxSpeed * scale);

    /// <summary>Accelerates toward <paramref name="dir"/> at full <see cref="maxSpeed"/>.</summary>
    /// <param name="dir">Desired movement direction.</param>
    /// <param name="dt">Physics delta time in seconds.</param>
    public void Accelerate(Vector2 dir, float dt) =>
        AccelerateWithSpeed(dir, dt, maxSpeed);

    /// <summary>Decelerates toward zero using ground or air deceleration depending on current state.</summary>
    /// <param name="dt">Physics delta time in seconds.</param>
    public void Decelerate(float dt) =>
        ApplyMovement(Vector2.Zero, dt, maxSpeed, (isGrounded || floatingMode) ? deceleration : airDeceleration);

    /// <summary>
    /// Convenience method that calls <see cref="Accelerate"/> if <paramref name="dir"/> is non-zero,
    /// otherwise calls <see cref="Decelerate"/>. Suitable as a single per-frame movement call.
    /// </summary>
    /// <param name="dir">Input direction. Pass <see cref="Vector2.Zero"/> to decelerate.</param>
    /// <param name="dt">Physics delta time in seconds.</param>
    public void Move(Vector2 dir, float dt)
    {
        if (dir.IsZeroApprox()) Decelerate(dt);
        else                    Accelerate(dir, dt);
    }

    /// <summary>
    /// Applies exponential lerp movement toward <paramref name="dir"/> at <paramref name="speed"/>.
    /// Vertical velocity is preserved unless in floating mode.
    /// </summary>
    /// <param name="dir">Desired direction. Will be normalized if non-zero.</param>
    /// <param name="dt">Physics delta time in seconds.</param>
    /// <param name="speed">Target speed in pixels per second.</param>
    /// <param name="weight">Lerp rate. Higher values reach the target speed faster.</param>
    public void ApplyMovement(Vector2 dir, float dt, float speed, float weight)
    {
        Vector2 desired     = dir.NormalizeIfNotZero() * speed;
        Vector2 newVelocity = MathUtil.ExponentialLerp(velocity, desired, dt, weight * weightFactor);

        if (!floatingMode)
            newVelocity.Y = velocity.Y;
        velocity = newVelocity;
    }

    #endregion

    #region Jump

    /// <summary>Returns true if a jump is available, either from remaining jumps or an active coyote window.</summary>
    public bool CanJump()         => jumpsUsed < maxJumps || HasCoyote();

    /// <summary>Returns true if a jump is available and a buffered jump input is pending.</summary>
    public bool CanJumpBuffered() => CanJump() && HasBufferedJump();

    /// <summary>Attempts to jump. Returns true if the jump was performed.</summary>
    public bool TryJump()         => CanJump()         && InvokeJump();

    /// <summary>Attempts to jump only if a buffered input is pending. Returns true if the jump was performed.</summary>
    public bool TryJumpBuffered() => CanJumpBuffered() && InvokeJump();

    /// <summary>Cuts the current jump arc at 50% vertical speed, producing a short hop. No effect when grounded.</summary>
    public void CutJump()         => CutJump(0.5f);

    private bool InvokeJump() { Jump(); return true; }

    /// <summary>
    /// Cuts the current jump arc by reducing upward velocity by <paramref name="ratio"/>.
    /// Call on jump button release to implement variable jump height.
    /// </summary>
    /// <param name="ratio">Fraction of current vertical speed to remove. 0.5 halves it, 1.0 kills it entirely.</param>
    public void CutJump(float ratio)
    {
        if (isGrounded) return;

        float vertical = velocity.Dot(controller.UpDirection);
        float cut      = vertical * ratio;

        velocity -= controller.UpDirection * cut;
    }


    /// <summary>
    /// Executes a jump unconditionally to a custom <paramref name="height"/>.
    /// Sets vertical velocity to the exact speed required to reach that height.
    /// Fires <see cref="Jumped"/>. Use <see cref="TryJump"/> for guarded jumps.
    /// </summary>
    /// <param name="height">Target apex height in pixels.</param>
    public void Jump(float height)
    {
        apexHangTimer = 0f;

        float g         = GetJumpGravity();
        float jumpSpeed = GetJumpVelocity(g, height);

        float currentVertical = velocity.Dot(controller.UpDirection);
        velocity += controller.UpDirection * (jumpSpeed - currentVertical);

        bool hadCoyote = HasCoyote();

        ConsumeBufferedJump();
        ConsumeCoyote();

        // FellOffEdge already consumed the ground jump slot via ConsumeJump(),
        // so coyote jumps don't increment — they spend the pre-consumed slot.
        if (!hadCoyote)
            jumpsUsed++;
        EventBus.Trigger(new Jumped(jumpsUsed));
    }

    /// <summary>
    /// Executes a jump unconditionally using the configured <see cref="jumpHeight"/>.
    /// Fires <see cref="Jumped"/>. Use <see cref="TryJump"/> for guarded jumps.
    /// </summary>
    public void Jump() => Jump(jumpHeight);

    /// <summary>Returns true if a coyote time window is currently active.</summary>
    public bool HasCoyote()                   => !coyoteTimer.IsReady;

    /// <summary>Returns true if a buffered jump input is currently pending.</summary>
    public bool HasBufferedJump()             => !jumpBufferTimer.IsReady;

    /// <summary>Cancels the active coyote window immediately.</summary>
    public void ConsumeCoyote()               => coyoteTimer.Stop();

    /// <summary>Cancels the buffered jump input immediately.</summary>
    public void ConsumeBufferedJump()         => jumpBufferTimer.Stop();

    /// <summary>Starts a coyote window using the default <see cref="coyoteTime"/> duration.</summary>
    public void AcquireCoyote()               => coyoteTimer.Start(coyoteTime);

    /// <summary>Buffers a jump input for the default <see cref="jumpBufferTime"/> duration.</summary>
    public void BufferJump()                  => jumpBufferTimer.Start(jumpBufferTime);

    /// <summary>Starts a coyote window with a custom duration.</summary>
    /// <param name="duration">Window duration in seconds.</param>
    public void AcquireCoyote(float duration) => coyoteTimer.Start(duration);

    /// <summary>Buffers a jump input for a custom duration.</summary>
    /// <param name="duration">Buffer duration in seconds.</param>
    public void BufferJump(float duration)    => jumpBufferTimer.Start(duration);

    /// <summary>Computes the initial vertical speed required to reach height <paramref name="h"/> under gravity <paramref name="g"/>.</summary>
    private static float GetJumpVelocity(float g, float h) => Mathf.Sqrt(2f * g * h);

    #endregion

    #region Explosion

    /// <summary>
    /// Applies an explosion impulse to the character based on distance from <paramref name="origin"/>.
    /// Characters outside <paramref name="radius"/> are unaffected.
    /// Downward velocity is cleared before the impulse so the knockback always reads at full strength.
    /// </summary>
    /// <param name="origin">World position of the explosion center.</param>
    /// <param name="force">Peak impulse magnitude in pixels per second (before mass division).</param>
    /// <param name="radius">Effective radius in pixels. No effect beyond this distance.</param>
    /// <param name="falloff">Curve controlling force attenuation with distance. Defaults to <see cref="FalloffCurve.Quadratic"/>.</param>
    /// <param name="upwardBias">
    /// Blends the knockback direction toward <see cref="CharacterBody2D.UpDirection"/>.
    /// 0 = purely directional, 0.4 = 40% upward blend. Produces a launch feel rather than a flat shove.
    /// </param>
    public void ApplyExplosion(Vector2 origin, float force, float radius,
        FalloffCurve falloff = FalloffCurve.Quadratic,
        float upwardBias     = 0.4f)
    {
        Vector2 diff     = controller.GlobalPosition - origin;
        float   distance = diff.Length();

        if (distance > radius) return;

        float   t         = Mathf.Clamp(distance / radius, 0f, 1f);
        float   strength  = ComputeFalloff(t, falloff);
        Vector2 direction = distance > 0.01f ? diff.Normalized() : controller.UpDirection;

        if (upwardBias > 0f)
            direction = (direction + controller.UpDirection * upwardBias).Normalized();

        float verticalSpeed = velocity.Dot(controller.UpDirection);
        if (verticalSpeed < 0f)
            velocity -= controller.UpDirection * verticalSpeed;

        AddImpulse(direction * force * strength);
    }

    /// <summary>Applies an explosion with default radius of 200px.</summary>
    /// <param name="origin">World position of the explosion center.</param>
    /// <param name="force">Peak impulse magnitude.</param>
    public void ApplyExplosion(Vector2 origin, float force) => ApplyExplosion(origin, force, 200f);

    /// <summary>Applies an explosion with default force of 1200 and radius of 200px.</summary>
    /// <param name="origin">World position of the explosion center.</param>
    public void ApplyExplosion(Vector2 origin)              => ApplyExplosion(origin, 1200f, 200f);

    private static float ComputeFalloff(float t, FalloffCurve curve) => curve switch
    {
        FalloffCurve.Linear        => 1f - t,
        FalloffCurve.Quadratic     => (1f - t) * (1f - t),
        FalloffCurve.InverseSquare => 1f / (1f + t * t * 10f),
        _                          => 1f - t
    };

    #endregion

    #region Gravity

    private void ApplyGravity(double delta)
    {
        if (isGrounded || !useGravity || floatingMode)
            return;

        float   dt             = (float)delta;
        Vector2 floorDirection = -controller.UpDirection;

        float gravity = GetFallGravity();
        gravity = TickApexHang(gravity, dt);

        velocity += gravity * dt * floorDirection;

        float fallSpeed = velocity.Dot(floorDirection);
        if (fallSpeed > maxFallSpeed)
            velocity -= floorDirection * (fallSpeed - maxFallSpeed);
    }

    private float TickApexHang(float gravity, float dt)
    {
        if (apexHangTimer > 0f)
        {
            apexHangTimer -= dt;
            UpdateApexAirControl(apexHorizontalBoost);

            return gravity * apexGravityReduction;
        }

        UpdateApexAirControl();
        return gravity;
    }

    private void UpdateApexAirControl(float boost = 1f)
    {
        airAcceleration = airAccelerationBase * boost;
        airDeceleration = airDecelerationBase * boost;
    }

    #endregion

    #region Settings

    /// <summary>
    /// Switches the controller's motion mode. Floating mode disables floor snapping and
    /// enables full 2D velocity control. Fires <see cref="MotionModeChanged"/> if the mode changed.
    /// </summary>
    /// <param name="mode">The new motion mode to apply.</param>
    public void SetMotionMode(CharacterBody2D.MotionModeEnum mode)
    {
        var oldMode           = controller.MotionMode;
        controller.MotionMode = mode;
        floatingMode          = mode == CharacterBody2D.MotionModeEnum.Floating;

        if (oldMode != controller.MotionMode)
            EventBus.Trigger<MotionModeChanged>();
    }

    /// <summary>Toggles gravity between <see cref="GravityState.Floor"/> and <see cref="GravityState.Ceiling"/>.</summary>
    public void SwitchGravity() =>
        SetGravityState(gravityState == GravityState.Floor ? GravityState.Ceiling : GravityState.Floor);

    /// <summary>
    /// Sets the gravity direction explicitly. Updates <see cref="CharacterBody2D.UpDirection"/> accordingly.
    /// Fires <see cref="GravitySwitched"/> if the state changed.
    /// </summary>
    /// <param name="state">The target gravity state.</param>
    /// <exception cref="Exception">Thrown if an unrecognised <see cref="GravityState"/> value is passed.</exception>
    public void SetGravityState(GravityState state)
    {
        controller.UpDirection = state switch
        {
            GravityState.Floor   => Vector2.Up,
            GravityState.Ceiling => Vector2.Down,
            _                    => throw new ArgumentOutOfRangeException(
                                    nameof(state), state, "Invalid GravityState value.")
        };

        var oldState = gravityState;
        gravityState = state;

        if (oldState != gravityState)
            EventBus.Trigger<GravitySwitched>();
    }

    #endregion

    #region Utilities

    /// <summary>Applies a continuous force scaled by <see cref="mass"/> and delta time. Use for sustained pushes.</summary>
    /// <param name="force">Force vector in world space.</param>
    /// <param name="dt">Physics delta time in seconds.</param>
    public void AddForce(Vector2 force, float dt)   => velocity += force   / mass * dt;

    /// <summary>Applies an instantaneous impulse scaled by <see cref="mass"/>. Use for one-shot velocity changes.</summary>
    /// <param name="impulse">Impulse vector in world space.</param>
    public void AddImpulse(Vector2 impulse)         => velocity += impulse / mass;

    /// <summary>Directly sets the full velocity vector, bypassing acceleration.</summary>
    public void SetVelocity(Vector2 value)          => velocity   = value;

    /// <summary>Directly sets the horizontal velocity component.</summary>
    public void SetVelocityX(float value)           => velocity.X = value;

    /// <summary>Directly sets the vertical velocity component in screen space. Prefer direction-safe methods for gravity-flipped characters.</summary>
    public void SetVelocityY(float value)           => velocity.Y = value;

    /// <summary>Clamps mass to a minimum of 0.01 to prevent division by zero in force calculations.</summary>
    public void SetMass(float value)                => mass = Mathf.Max(0.01f, value);

    /// <summary>Sets the maximum allowed jump count. Minimum value is 1.</summary>
    public void SetMaxJumps(int value)              => maxJumps  = Mathf.Max(1, value);

    /// <summary>Resets jump count to zero. Called automatically on landing.</summary>
    public void ResetJumps()                        => jumpsUsed = 0;

    /// <summary>Restores <paramref name="count"/> jumps by reducing <c>jumpsUsed</c>. Clamps to zero.</summary>
    /// <param name="count">Number of jumps to restore.</param>
    public void AddJump(int count)                  => jumpsUsed = Mathf.Max(0, jumpsUsed - count);

    /// <summary>Restores one jump.</summary>
    public void AddJump()                           => AddJump(1);
    
    /// <summary>Consumes <paramref name="count"/> of jumps by increasing <c>jumpsUsed</c>. Clamps to <c>maxJumps</c>.</summary>
    /// <param name="count">Number of jumps to consume.</param>
    public void ConsumeJump(int count) => jumpsUsed = Mathf.Min(maxJumps, jumpsUsed + count);

    /// <summary>Consumes one jump.</summary>
    public void ConsumeJump()                       => ConsumeJump(1);

    /// <summary>Re-enables gravity after <see cref="DisableGravity"/> was called.</summary>
    /// <remarks>Has no effect while the controller is in floating motion mode,
    /// since floating mode bypasses gravity accumulation entirely.</remarks>
    public void EnableGravity()                     => useGravity = true;

    /// <summary>Disables gravity accumulation. Velocity retains its current vertical component until manually changed.</summary>
    public void DisableGravity()                    => useGravity = false;

    /// <summary>Returns true if the character is currently on the floor.</summary>
    public bool IsGrounded()                        => isGrounded;

    /// <summary>Returns true if the character is airborne and moving in the gravity direction.</summary>
    public bool IsFalling()                         => isFalling;

    /// <summary>Returns true if the character is airborne and moving against the gravity direction.</summary>
    public bool IsRising()                          => !isGrounded && velocity.Dot(controller.UpDirection) > FALL_THRESHOLD;

    /// <summary>Returns true if gravity accumulation is currently active.</summary>
    public bool IsGravityActive()                   => useGravity;

    /// <summary>Returns true if the character is currently touching a wall.</summary>
    public bool IsOnWall()                          => controller.IsOnWall();

    /// <summary>Returns true if the character is currently touching a ceiling (or floor when gravity is flipped).</summary>
    public bool IsOnCeiling()                       => controller.IsOnCeiling();

    // Utilities region — add above each bare method
    /// <summary>
    /// Sets the fall gravity multiplier. Values above 1.0 make falls snappier.
    /// Clamped to a minimum of 0 to prevent upward gravity inversion.
    /// </summary>
    /// <param name="value">The new multiplier. Clamped to [0, ∞).</param>
    public void SetFallGravityMultiplier(float value) => fallGravityMultiplier = Mathf.Max(0, value);

    /// <summary>
    /// Sets the terminal fall speed in pixels per second.
    /// Clamped to a minimum of 0. Set to a high value to effectively disable the cap.
    /// </summary>
    /// <param name="value">The new cap in pixels per second. Clamped to [0, ∞).</param>
    public void SetMaxFallSpeed(float value)          => maxFallSpeed = Mathf.Max(0, value);

    /// <summary>
    /// Sets the weight factor applied to all acceleration and deceleration calculations.
    /// Use values below 1.0 to slow movement (e.g. in mud or water) and above 1.0 to boost it.
    /// Clamped to a minimum of 0.
    /// </summary>
    /// <param name="value">The new weight factor. Clamped to [0, ∞).</param>
    public void SetWeightFactor(float value)          => weightFactor = Mathf.Max(0f, value);

    /// <summary>
    /// Returns the gravity used when computing jump velocity — <see cref="DEFAULT_GRAVITY"/>
    /// scaled by <see cref="gravityScale"/>. Does not include <see cref="fallGravityMultiplier"/>.
    /// </summary>
    /// <returns>Jump gravity in pixels per second squared.</returns>
    public float GetJumpGravity()                     => DEFAULT_GRAVITY * gravityScale;

    /// <summary>
    /// Returns the gravity used when computing fall velocity — <see cref="DEFAULT_GRAVITY"/>
    /// scaled by <see cref="gravityScale"/>. Does include <see cref="fallGravityMultiplier"/>.
    /// </summary>
    /// <returns>fall gravity in pixels per second squared.</returns>
    public float GetFallGravity()
    {
        return DEFAULT_GRAVITY * gravityScale * fallGravityMultiplier;
    }

    /// <summary>
    /// Returns the gravity currently acting on the character based on their fall state.
    /// </summary>
    /// <returns>Effective gravity in pixels per second squared.</returns>
    public float GetCurrentGravity()
    {
        return isFalling ? GetFallGravity() : GetJumpGravity();
    }

    #endregion


}

