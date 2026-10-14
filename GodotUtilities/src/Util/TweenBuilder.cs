using Godot;

namespace GodotUtilities;

public static class TweenBuilderExtension
{
    public static TweenBuilder BuildTween(this Node node, int loops = 1)
    {
        return new TweenBuilder(node, loops);
    }
}

public sealed class TweenBuilder(Node node, int loops = 1)
{
    private readonly Tween _tween = node.CreateTween().SetLoops(loops);

    private Tweener _currentTweener;

    public Tween Tween => _tween;

    public bool IsValid => !_tween.IsNullOrInvalid();
    public bool IsRunning => IsValid && _tween.IsRunning();

    #region Constructors

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

    #endregion

    #region Sequence Control

    public TweenBuilder Property(GodotObject obj, NodePath prop, Variant finalVal, double duration)
    {
        _currentTweener = _tween.TweenProperty(obj, prop, finalVal, duration);
        return this;
    }

    public TweenBuilder Interval(double duration)
    {
        _currentTweener = _tween.TweenInterval(duration);
        return this;
    }

    public TweenBuilder Method<[MustBeVariant] T>(Action<T> action, T from, T to, double duration)
    {
        _currentTweener = _tween.TweenMethod(action, from, to, duration);
        return this;
    }

    public TweenBuilder Callback(Action action)
    {
        _currentTweener = _tween.TweenAction(action);
        return this;
    }

    public TweenBuilder Callback(Callable callable)
    {
        _currentTweener = _tween.TweenCallback(callable);
        return this;
    }

    public TweenBuilder Await(Signal signal)
    {
        _currentTweener = _tween.TweenAwait(signal);
        return this;
    }

    public TweenBuilder Parallel()
    {
        _tween.Parallel();
        return this;
    }

    public TweenBuilder Chain()
    {
        _tween.Chain();
        return this;
    }

    public TweenBuilder SubTween(Tween tween)
    {
        _currentTweener = _tween.TweenSubtween(tween);
        return this;
    }

    public TweenBuilder SubBuilder(TweenBuilder builder)
    {
        _currentTweener = _tween.TweenSubtween(builder.Tween);
        return this;
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

    #region Transition Shortcuts

    public TweenBuilder Linear() => SetTrans(Tween.TransitionType.Linear);
    public TweenBuilder Sine() => SetTrans(Tween.TransitionType.Sine);
    public TweenBuilder Back() => SetTrans(Tween.TransitionType.Back);
    public TweenBuilder Bounce() => SetTrans(Tween.TransitionType.Bounce);
    public TweenBuilder Circ() => SetTrans(Tween.TransitionType.Circ);
    public TweenBuilder Spring() => SetTrans(Tween.TransitionType.Spring);
    public TweenBuilder Quad() => SetTrans(Tween.TransitionType.Quad);
    public TweenBuilder Quart() => SetTrans(Tween.TransitionType.Quart);
    public TweenBuilder Expo() => SetTrans(Tween.TransitionType.Expo);
    public TweenBuilder Quint() => SetTrans(Tween.TransitionType.Quint);
    public TweenBuilder Elastic() => SetTrans(Tween.TransitionType.Elastic);
    public TweenBuilder Cubic() => SetTrans(Tween.TransitionType.Cubic);

    #endregion

    #region Ease Shortcuts

    public TweenBuilder EaseIn() => SetEase(Tween.EaseType.In);
    public TweenBuilder EaseOut() => SetEase(Tween.EaseType.Out);
    public TweenBuilder EaseOutIn() => SetEase(Tween.EaseType.OutIn);
    public TweenBuilder EaseInOut() => SetEase(Tween.EaseType.InOut);

    #endregion
}



