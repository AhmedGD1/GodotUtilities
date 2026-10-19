using Godot;

namespace GodotUtilities;

/// <summary>
/// Fluent builder that arranges a native Godot <see cref="Godot.Tween"/>. It adds no behavior of its own:
/// every call maps to the matching Tween/Tweener call, and <see cref="Tween"/> exposes the result.
/// </summary>
/// <param name="node">Node that owns the tween (it is freed with this node).</param>
/// <param name="loops">Number of loops. 0 means infinite.</param>
public sealed class TweenBuilder(Node node, int loops = 1)
{
    private readonly Tween _tween = node.CreateTween().SetLoops(loops);

    private Tweener _currentTweener;
    private bool _parallelBlock;

    #region Properties

    /// <summary>The underlying Godot tween. Use it for anything the builder doesn't wrap (pause mode, speed scale, Finished signal...).</summary>
    public Tween Tween => _tween;

    /// <summary>Whether the underlying tween exists and is still valid.</summary>
    public bool IsValid => !_tween.IsNullOrInvalid();

    /// <summary>Whether the underlying tween is valid and currently running.</summary>
    public bool IsRunning => IsValid && _tween.IsRunning();

    #endregion

    #region Private

    private InvalidOperationException Unsupported(string op, string supported)
    {
        string current = _currentTweener?.GetType().Name ?? "none (add a tweener first)";
        return new InvalidOperationException($"{op} only works on {supported}, but the current tweener is: {current}.");
    }

    private PropertyTweener RequireProperty(string op)
    {
        return _currentTweener as PropertyTweener ?? throw Unsupported(op, nameof(PropertyTweener));
    }

    private void ParallelIfRequired()
    {
        if (_parallelBlock)
            _tween.Parallel();
    }

    #endregion

    #region Sequence Control

    /// <summary>Tweens a property. Becomes the current tweener.</summary>
    /// <param name="obj">The object that owns the property.</param>
    /// <param name="prop">Path of the property to tween.</param>
    /// <param name="finalVal">The value the property ends at.</param>
    /// <param name="duration">Duration in seconds.</param>
    public TweenBuilder Property(GodotObject obj, NodePath prop, Variant finalVal, double duration)    
    {
        ParallelIfRequired();

        _currentTweener = _tween.TweenProperty(obj, prop, finalVal, duration);
        return this;
    }

    /// <summary>Adds a standalone pause step. Use <see cref="SetDelay"/> to offset a single tweener instead.</summary>
    /// <param name="duration">Pause length in seconds.</param>
    public TweenBuilder Wait(double duration)
    {
        ParallelIfRequired();

        _currentTweener = _tween.TweenInterval(duration);
        return this;
    }

    /// <summary>Calls <paramref name="action"/> every frame with a value interpolated from <paramref name="from"/> to <paramref name="to"/>. Becomes the current tweener.</summary>
    /// <typeparam name="T">The type of the interpolated value.</typeparam>
    /// <param name="action">Method that receives the interpolated value.</param>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The final value.</param>
    /// <param name="duration">Duration in seconds.</param>
    public TweenBuilder Method<[MustBeVariant] T>(Action<T> action, T from, T to, double duration)
    {
        ParallelIfRequired();

        _currentTweener = _tween.TweenMethod(action, from, to, duration);
        return this;
    }

    /// <summary>Calls <paramref name="action"/> when the sequence reaches this step.</summary>
    /// <param name="action">The method to call.</param>
    public TweenBuilder Callback(Action action)
    {
        ParallelIfRequired();

        _currentTweener = _tween.TweenAction(action);
        return this;
    }

    /// <summary>Calls <paramref name="callable"/> when the sequence reaches this step.</summary>
    /// <param name="callable">The callable to invoke.</param>
    public TweenBuilder Callback(Callable callable)
    {
        ParallelIfRequired();

        _currentTweener = _tween.TweenCallback(callable);
        return this;
    }

    /// <summary>Pauses the sequence at this step until <paramref name="signal"/> is emitted. This does not use C# await.</summary>
    /// <param name="signal">The signal to wait for.</param>
    public TweenBuilder WaitForSignal(Signal signal)
    {
        ParallelIfRequired();
        
        _currentTweener = _tween.TweenAwait(signal);
        return this;
    }

    /// <summary>Makes only the next tweener run alongside the previous one. For several, use <see cref="BeginParallel"/>.</summary>
    public TweenBuilder JoinPrevious()
    {
        _tween.Parallel();
        return this;
    }

    /// <summary>
    /// Every tweener added after this call runs in parallel until <see cref="EndParallel"/>.
    /// Blocks cannot be nested: the first <see cref="EndParallel"/> ends parallel mode.
    /// </summary>
    public TweenBuilder BeginParallel()
    {
        _parallelBlock = true;
        return this;
    }

    /// <summary>Ends the parallel block started by <see cref="BeginParallel"/>. Later tweeners run sequentially again.</summary>
    public TweenBuilder EndParallel()
    {
        _parallelBlock = false;
        return this;
    }

    /// <summary>Makes the next tweener start after the previous step (Godot's default)</summary>
    public TweenBuilder Chain()
    {
        _tween.Chain();
        return this;
    }

    /// <summary>Runs another tween as a step of this one.</summary>
    /// <param name="tween">The tween to run.</param>
    public TweenBuilder SubTween(Tween tween)
    {
        ParallelIfRequired();

        _currentTweener = _tween.TweenSubtween(tween);
        return this;
    }

    /// <summary>Runs another tween as a step of this one.</summary>
    /// <param name="builder">The builder whose tween will run.</param>
    public TweenBuilder SubTween(TweenBuilder builder)
    {
        return SubTween(builder.Tween);
    }

    #endregion

    #region Common Configuration

    /// <summary>Sets the easing of the current tweener.</summary>
    /// <param name="ease">The ease type.</param>
    /// <exception cref="InvalidOperationException">The current tweener is not a Property or Method tweener.</exception>
    public TweenBuilder SetEase(Tween.EaseType ease)
    {
        switch (_currentTweener)
        {
            case MethodTweener t: t.SetEase(ease); break;
            case PropertyTweener t: t.SetEase(ease); break;
            default: throw Unsupported(nameof(SetEase), "PropertyTweener or MethodTweener");
        }
        return this;
    }

    /// <summary>Sets the transition type of the current tweener.</summary>
    /// <param name="transition">The transition type.</param>
    /// <exception cref="InvalidOperationException">The current tweener is not a Property or Method tweener.</exception>
    public TweenBuilder SetTrans(Tween.TransitionType transition)
    {
        switch (_currentTweener)
        {
            case MethodTweener t: t.SetTrans(transition); break;
            case PropertyTweener t: t.SetTrans(transition); break;
            default: throw Unsupported(nameof(SetTrans), "PropertyTweener or MethodTweener");
        }
        return this;
    }

    /// <summary>Delays the start of the current tweener.</summary>
    /// <param name="delay">Delay in seconds.</param>
    /// <exception cref="InvalidOperationException">The current tweener is not a Property, Method, Callback or Subtween tweener.</exception>
    public TweenBuilder SetDelay(double delay)
    {
        switch (_currentTweener)
        {
            case MethodTweener t: t.SetDelay(delay); break;
            case PropertyTweener t: t.SetDelay(delay); break;
            case CallbackTweener t: t.SetDelay(delay); break;
            case SubtweenTweener t: t.SetDelay(delay); break;
            default: throw Unsupported(nameof(SetDelay), "Property, Method, Callback or Subtween tweeners");
        }
        return this;
    }

    /// <summary>Uses a <see cref="Curve"/> to shape the interpolation of the current property tweener.</summary>
    /// <param name="curve">The curve to sample.</param>
    /// <exception cref="InvalidOperationException">The current tweener is not a Property tweener.</exception>
    public TweenBuilder SetCurveInterpolator(Curve curve)
    {
        RequireProperty(nameof(SetCurveInterpolator)).SetCurveInterpolator(curve);
        return this;
    }

    /// <summary>Sets the starting value of the current property tweener.</summary>
    /// <param name="value">The starting value.</param>
    /// <exception cref="InvalidOperationException">The current tweener is not a Property tweener.</exception>
    public TweenBuilder From(Variant value)
    {
        RequireProperty(nameof(From)).From(value);
        return this;
    }

    /// <summary>Starts the current property tweener from the property's current value.</summary>
    /// <exception cref="InvalidOperationException">The current tweener is not a Property tweener.</exception>
    public TweenBuilder FromCurrent()
    {
        RequireProperty(nameof(FromCurrent)).FromCurrent();
        return this;
    }

    /// <summary>Treats the final value of the current property tweener as an offset from the starting value.</summary>
    /// <exception cref="InvalidOperationException">The current tweener is not a Property tweener.</exception>
    public TweenBuilder AsRelative()
    {
        RequireProperty(nameof(AsRelative)).AsRelative();
        return this;
    }

    #endregion    
}

/// <summary>
/// Extension methods for <see cref="TweenBuilder"/> that add transition, ease, and common property shortcuts.
/// </summary>
public static class TweenBuilderExtension
{
    #region Creation

    /// <summary>Creates a <see cref="TweenBuilder"/> bound to this node. Call .Tween afterward to access the native tween.</summary>
    /// <param name="node">Node that owns the tween.</param>
    /// <param name="loops">Number of loops. 0 means infinite.</param>
    public static TweenBuilder BuildTween(this Node node, int loops = 1)
    {
        return new TweenBuilder(node, loops);
    }

    #endregion

    #region Transition Shortcuts

    /// <summary>Sets the current tweener's transition to Linear.</summary>
    public static TweenBuilder Linear(this TweenBuilder builder) => builder.SetTrans(Tween.TransitionType.Linear);

    /// <summary>Sets the current tweener's transition to Sine.</summary>
    public static TweenBuilder Sine(this TweenBuilder builder) => builder.SetTrans(Tween.TransitionType.Sine);

    /// <summary>Sets the current tweener's transition to Back.</summary>
    public static TweenBuilder Back(this TweenBuilder builder) => builder.SetTrans(Tween.TransitionType.Back);

    /// <summary>Sets the current tweener's transition to Bounce.</summary>
    public static TweenBuilder Bounce(this TweenBuilder builder) => builder.SetTrans(Tween.TransitionType.Bounce);

    /// <summary>Sets the current tweener's transition to Circ.</summary>
    public static TweenBuilder Circ(this TweenBuilder builder) => builder.SetTrans(Tween.TransitionType.Circ);

    /// <summary>Sets the current tweener's transition to Spring.</summary>
    public static TweenBuilder Spring(this TweenBuilder builder) => builder.SetTrans(Tween.TransitionType.Spring);

    /// <summary>Sets the current tweener's transition to Quad.</summary>
    public static TweenBuilder Quad(this TweenBuilder builder) => builder.SetTrans(Tween.TransitionType.Quad);

    /// <summary>Sets the current tweener's transition to Quart.</summary>
    public static TweenBuilder Quart(this TweenBuilder builder) => builder.SetTrans(Tween.TransitionType.Quart);

    /// <summary>Sets the current tweener's transition to Expo.</summary>
    public static TweenBuilder Expo(this TweenBuilder builder) => builder.SetTrans(Tween.TransitionType.Expo);

    /// <summary>Sets the current tweener's transition to Quint.</summary>
    public static TweenBuilder Quint(this TweenBuilder builder) => builder.SetTrans(Tween.TransitionType.Quint);

    /// <summary>Sets the current tweener's transition to Elastic.</summary>
    public static TweenBuilder Elastic(this TweenBuilder builder) => builder.SetTrans(Tween.TransitionType.Elastic);

    /// <summary>Sets the current tweener's transition to Cubic.</summary>
    public static TweenBuilder Cubic(this TweenBuilder builder) => builder.SetTrans(Tween.TransitionType.Cubic);

    #endregion

    #region Ease Shortcuts

    /// <summary>Sets the current tweener's ease to In.</summary>
    public static TweenBuilder EaseIn(this TweenBuilder builder) => builder.SetEase(Tween.EaseType.In);

    /// <summary>Sets the current tweener's ease to Out.</summary>
    public static TweenBuilder EaseOut(this TweenBuilder builder) => builder.SetEase(Tween.EaseType.Out);

    /// <summary>Sets the current tweener's ease to OutIn.</summary>
    public static TweenBuilder EaseOutIn(this TweenBuilder builder) => builder.SetEase(Tween.EaseType.OutIn);

    /// <summary>Sets the current tweener's ease to InOut.</summary>
    public static TweenBuilder EaseInOut(this TweenBuilder builder) => builder.SetEase(Tween.EaseType.InOut);

    #endregion

    #region Transform Shortcuts

    /// <summary>Tweens the <c>position</c> property of <paramref name="target"/>.</summary>
    /// <param name="builder"></param>
    /// <param name="target">The object to tween.</param>
    /// <param name="to">The final position.</param>
    /// <param name="duration">Duration in seconds.</param>
    public static TweenBuilder Position(this TweenBuilder builder, GodotObject target, Variant to, double duration) =>
        builder.Property(target, TweenExtension.PropertyPosition, to, duration);

    /// <summary>Tweens the <c>global_position</c> property of <paramref name="target"/>.</summary>
    /// <param name="builder"></param>
    /// <param name="target">The object to tween.</param>
    /// <param name="to">The final global position.</param>
    /// <param name="duration">Duration in seconds.</param>
    public static TweenBuilder GlobalPosition(this TweenBuilder builder, GodotObject target, Variant to, double duration) =>
        builder.Property(target, TweenExtension.PropertyGlobalPosition, to, duration);

    /// <summary>Tweens the <c>scale</c> property of <paramref name="target"/>.</summary>
    /// <param name="builder"></param>
    /// <param name="target">The object to tween.</param>
    /// <param name="to">The final scale.</param>
    /// <param name="duration">Duration in seconds.</param>
    public static TweenBuilder Scale(this TweenBuilder builder, GodotObject target, Variant to, double duration) =>
        builder.Property(target, TweenExtension.PropertyScale, to, duration);

    /// <summary>Tweens the <c>rotation</c> property (radians) of <paramref name="target"/>.</summary>
    /// <param name="builder"></param>
    /// <param name="target">The object to tween.</param>
    /// <param name="to">The final rotation.</param>
    /// <param name="duration">Duration in seconds.</param>
    public static TweenBuilder Rotation(this TweenBuilder builder, GodotObject target, Variant to, double duration) =>
        builder.Property(target, TweenExtension.PropertyRotation, to, duration);

    /// <summary>Tweens the <c>rotation_degrees</c> property of <paramref name="target"/>.</summary>
    /// <param name="builder"></param>
    /// <param name="target">The object to tween.</param>
    /// <param name="to">The final rotation in degrees.</param>
    /// <param name="duration">Duration in seconds.</param>
    public static TweenBuilder RotationDegrees(this TweenBuilder builder, GodotObject target, Variant to, double duration) =>
        builder.Property(target, TweenExtension.PropertyRotationDegrees, to, duration);

    #endregion

    #region Offset Transform Shortcuts

    /// <summary>Tweens the offset transform position of <paramref name="control"/>.</summary>
    /// <param name="builder"></param>
    /// <param name="control">The control to tween.</param>
    /// <param name="to">The final offset position.</param>
    /// <param name="duration">Duration in seconds.</param>
    public static TweenBuilder OffsetPosition(this TweenBuilder builder, Control control, Vector2 to, double duration) =>
        builder.Property(control, TweenExtension.PropertyOffsetTransformPos, to, duration);

    /// <summary>Tweens the offset transform position of <paramref name="control"/> as a ratio.</summary>
    /// <param name="builder"></param>
    /// <param name="control">The control to tween.</param>
    /// <param name="to">The final offset position ratio.</param>
    /// <param name="duration">Duration in seconds.</param>
    public static TweenBuilder OffsetPositionRatio(this TweenBuilder builder, Control control, Vector2 to, double duration) =>
        builder.Property(control, TweenExtension.PropertyOffsetTransformPosRatio, to, duration);

    /// <summary>Tweens the offset transform scale of <paramref name="control"/>.</summary>
    /// <param name="builder"></param>
    /// <param name="control">The control to tween.</param>
    /// <param name="to">The final offset scale.</param>
    /// <param name="duration">Duration in seconds.</param>
    public static TweenBuilder OffsetScale(this TweenBuilder builder, Control control, Vector2 to, double duration) =>
        builder.Property(control, TweenExtension.PropertyOffsetTransformScale, to, duration);

    /// <summary>Tweens the offset transform rotation of <paramref name="control"/>.</summary>
    /// <param name="builder"></param>
    /// <param name="control">The control to tween.</param>
    /// <param name="to">The final offset rotation.</param>
    /// <param name="duration">Duration in seconds.</param>
    public static TweenBuilder OffsetRotation(this TweenBuilder builder, Control control, float to, double duration) =>
        builder.Property(control, TweenExtension.PropertyOffsetTransformRotation, to, duration);

    #endregion

    #region Color Shortcuts

    /// <summary>Tweens the <c>color</c> property of <paramref name="target"/>.</summary>
    /// <param name="builder"></param>
    /// <param name="target">The object to tween.</param>
    /// <param name="to">The final color.</param>
    /// <param name="duration">Duration in seconds.</param>
    public static TweenBuilder Color(this TweenBuilder builder, GodotObject target, Color to, double duration) =>
        builder.Property(target, TweenExtension.PropertyColor, to, duration);

    /// <summary>Tweens the <c>modulate</c> color of <paramref name="item"/>.</summary>
    /// <param name="builder"></param>
    /// <param name="item">The canvas item to tween.</param>
    /// <param name="to">The final modulate color.</param>
    /// <param name="duration">Duration in seconds.</param>
    public static TweenBuilder Modulate(this TweenBuilder builder, CanvasItem item, Color to, double duration) =>
        builder.Property(item, TweenExtension.PropertyModulate, to, duration);

    /// <summary>Tweens the <c>self_modulate</c> color of <paramref name="item"/>.</summary>
    /// <param name="builder"></param>
    /// <param name="item">The canvas item to tween.</param>
    /// <param name="to">The final self modulate color.</param>
    /// <param name="duration">Duration in seconds.</param>
    public static TweenBuilder SelfModulate(this TweenBuilder builder, CanvasItem item, Color to, double duration) =>
        builder.Property(item, TweenExtension.PropertySelfModulate, to, duration);

    /// <summary>Tweens only the alpha of the <c>modulate</c> color of <paramref name="item"/>.</summary>
    /// <param name="builder"></param>
    /// <param name="item">The canvas item to tween.</param>
    /// <param name="alpha">The final alpha, from 0 to 1.</param>
    /// <param name="duration">Duration in seconds.</param>
    public static TweenBuilder ModulateAlpha(this TweenBuilder builder, CanvasItem item, float alpha, double duration) =>
        builder.Property(item, TweenExtension.PropertyModulateAlpha, alpha, duration);

    /// <summary>Tweens only the alpha of the <c>self_modulate</c> color of <paramref name="item"/>.</summary>
    /// <param name="builder"></param>
    /// <param name="item">The canvas item to tween.</param>
    /// <param name="alpha">The final alpha, from 0 to 1.</param>
    /// <param name="duration">Duration in seconds.</param>
    public static TweenBuilder SelfModulateAlpha(this TweenBuilder builder, CanvasItem item, float alpha, double duration) =>
        builder.Property(item, TweenExtension.PropertySelfModulateAlpha, alpha, duration);

    #endregion

    #region Shader Shortcut

    /// <summary>Tweens a shader parameter of <paramref name="material"/>.</summary>
    /// <param name="builder"></param>
    /// <param name="material">The shader material that owns the parameter.</param>
    /// <param name="paramName">Name of the shader parameter, without the <c>shader_parameter/</c> prefix.</param>
    /// <param name="to">The final parameter value.</param>
    /// <param name="duration">Duration in seconds.</param>
    public static TweenBuilder ShaderParam(this TweenBuilder builder, ShaderMaterial material, string paramName, Variant to, double duration) =>
        builder.Property(material, $"shader_parameter/{paramName}", to, duration);

    #endregion
}