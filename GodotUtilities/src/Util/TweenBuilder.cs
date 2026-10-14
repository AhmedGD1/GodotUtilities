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

    public bool IsValid => !_tween.IsNullOrInvalid();
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
    public TweenBuilder Property(GodotObject obj, NodePath prop, Variant finalVal, double duration)    
    {
        ParallelIfRequired();

        _currentTweener = _tween.TweenProperty(obj, prop, finalVal, duration);
        return this;
    }

    /// <summary>Adds a standalone pause step. Use <see cref="SetDelay"/> to offset a single tweener instead.</summary>
    public TweenBuilder Wait(double duration)
    {
        ParallelIfRequired();

        _currentTweener = _tween.TweenInterval(duration);
        return this;
    }

    public TweenBuilder Method<[MustBeVariant] T>(Action<T> action, T from, T to, double duration)
    {
        ParallelIfRequired();

        _currentTweener = _tween.TweenMethod(action, from, to, duration);
        return this;
    }

    /// <summary>Calls <paramref name="action"/> when the sequence reaches this step.</summary>
    public TweenBuilder Callback(Action action)
    {
        ParallelIfRequired();

        _currentTweener = _tween.TweenAction(action);
        return this;
    }

    /// <summary>Calls <paramref name="callable"/> when the sequence reaches this step.</summary>
    public TweenBuilder Callback(Callable callable)
    {
        ParallelIfRequired();

        _currentTweener = _tween.TweenCallback(callable);
        return this;
    }

    /// <summary>Pauses the sequence at this step until <paramref name="signal"/> is emitted. This does not use C# await.</summary>
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

    /// <summary>Makes the next tweener start after the previous step (Godot's default)
    public TweenBuilder Chain()
    {
        _tween.Chain();
        return this;
    }

    /// <summary>Runs another tween as a step of this one.</summary>
    public TweenBuilder SubTween(Tween tween)
    {
        ParallelIfRequired();

        _currentTweener = _tween.TweenSubtween(tween);
        return this;
    }

    /// <summary>Runs another tween as a step of this one.</summary>
    public TweenBuilder SubTween(TweenBuilder builder)
    {
        return SubTween(builder.Tween);
    }

    #endregion

    #region Common Configuration

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

    public TweenBuilder SetCurveInterpolator(Curve curve)
    {
        RequireProperty(nameof(SetCurveInterpolator)).SetCurveInterpolator(curve);
        return this;
    }

    public TweenBuilder From(Variant value)
    {
        RequireProperty(nameof(From)).From(value);
        return this;
    }

    public TweenBuilder FromCurrent()
    {
        RequireProperty(nameof(FromCurrent)).FromCurrent();
        return this;
    }

    public TweenBuilder AsRelative()
    {
        RequireProperty(nameof(AsRelative)).AsRelative();
        return this;
    }

    #endregion    
}

public static class TweenBuilderExtension
{
    #region Creation

    /// <summary>Creates a <see cref="TweenBuilder"/> bound to this node. Call .Tween afterward to access the native tween.</summary>
    public static TweenBuilder BuildTween(this Node node, int loops = 1)
    {
        return new TweenBuilder(node, loops);
    }

    #endregion

    #region Transition Shortcuts

    public static TweenBuilder Linear(this TweenBuilder builder) => builder.SetTrans(Tween.TransitionType.Linear);
    public static TweenBuilder Sine(this TweenBuilder builder) => builder.SetTrans(Tween.TransitionType.Sine);
    public static TweenBuilder Back(this TweenBuilder builder) => builder.SetTrans(Tween.TransitionType.Back);
    public static TweenBuilder Bounce(this TweenBuilder builder) => builder.SetTrans(Tween.TransitionType.Bounce);
    public static TweenBuilder Circ(this TweenBuilder builder) => builder.SetTrans(Tween.TransitionType.Circ);
    public static TweenBuilder Spring(this TweenBuilder builder) => builder.SetTrans(Tween.TransitionType.Spring);
    public static TweenBuilder Quad(this TweenBuilder builder) => builder.SetTrans(Tween.TransitionType.Quad);
    public static TweenBuilder Quart(this TweenBuilder builder) => builder.SetTrans(Tween.TransitionType.Quart);
    public static TweenBuilder Expo(this TweenBuilder builder) => builder.SetTrans(Tween.TransitionType.Expo);
    public static TweenBuilder Quint(this TweenBuilder builder) => builder.SetTrans(Tween.TransitionType.Quint);
    public static TweenBuilder Elastic(this TweenBuilder builder) => builder.SetTrans(Tween.TransitionType.Elastic);
    public static TweenBuilder Cubic(this TweenBuilder builder) => builder.SetTrans(Tween.TransitionType.Cubic);

    #endregion

    #region Ease Shortcuts

    public static TweenBuilder EaseIn(this TweenBuilder builder) => builder.SetEase(Tween.EaseType.In);
    public static TweenBuilder EaseOut(this TweenBuilder builder) => builder.SetEase(Tween.EaseType.Out);
    public static TweenBuilder EaseOutIn(this TweenBuilder builder) => builder.SetEase(Tween.EaseType.OutIn);
    public static TweenBuilder EaseInOut(this TweenBuilder builder) => builder.SetEase(Tween.EaseType.InOut);

    #endregion

    #region Transform Shortcuts

    public static TweenBuilder Position(this TweenBuilder builder, GodotObject target, Variant to, double duration) =>
        builder.Property(target, TweenExtension.PropertyPosition, to, duration);

    public static TweenBuilder GlobalPosition(this TweenBuilder builder, GodotObject target, Variant to, double duration) =>
        builder.Property(target, TweenExtension.PropertyGlobalPosition, to, duration);

    public static TweenBuilder Scale(this TweenBuilder builder, GodotObject target, Variant to, double duration) =>
        builder.Property(target, TweenExtension.PropertyScale, to, duration);

    public static TweenBuilder Rotation(this TweenBuilder builder, GodotObject target, Variant to, double duration) =>
        builder.Property(target, TweenExtension.PropertyRotation, to, duration);

    public static TweenBuilder RotationDegrees(this TweenBuilder builder, GodotObject target, Variant to, double duration) =>
        builder.Property(target, TweenExtension.PropertyRotationDegrees, to, duration);

    #endregion

    #region Offset Transform Shortcuts

    public static TweenBuilder OffsetPosition(this TweenBuilder builder, Control control, Vector2 to, double duration) =>
        builder.Property(control, TweenExtension.PropertyOffsetTransformPos, to, duration);

    public static TweenBuilder OffsetPositionRatio(this TweenBuilder builder, Control control, Vector2 to, double duration) =>
        builder.Property(control, TweenExtension.PropertyOffsetTransformPosRatio, to, duration);

    public static TweenBuilder OffsetScale(this TweenBuilder builder, Control control, Vector2 to, double duration) =>
        builder.Property(control, TweenExtension.PropertyOffsetTransformScale, to, duration);

    public static TweenBuilder OffsetRotation(this TweenBuilder builder, Control control, float to, double duration) =>
        builder.Property(control, TweenExtension.PropertyOffsetTransformRotation, to, duration);

    #endregion

    #region Color Shortcuts

    public static TweenBuilder Color(this TweenBuilder builder, GodotObject target, Color to, double duration) =>
        builder.Property(target, TweenExtension.PropertyColor, to, duration);

    public static TweenBuilder Modulate(this TweenBuilder builder, CanvasItem item, Color to, double duration) =>
        builder.Property(item, TweenExtension.PropertyModulate, to, duration);

    public static TweenBuilder SelfModulate(this TweenBuilder builder, CanvasItem item, Color to, double duration) =>
        builder.Property(item, TweenExtension.PropertySelfModulate, to, duration);

    public static TweenBuilder ModulateAlpha(this TweenBuilder builder, CanvasItem item, float alpha, double duration) =>
        builder.Property(item, TweenExtension.PropertyModulateAlpha, alpha, duration);

    public static TweenBuilder SelfModulateAlpha(this TweenBuilder builder, CanvasItem item, float alpha, double duration) =>
        builder.Property(item, TweenExtension.PropertySelfModulateAlpha, alpha, duration);

    #endregion

    #region Shader Shortcut

    public static TweenBuilder ShaderParam(this TweenBuilder builder, ShaderMaterial material, string paramName, Variant to, double duration) =>
        builder.Property(material, $"shader_parameter/{paramName}", to, duration);

    #endregion
}

