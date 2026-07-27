using Godot;

namespace GodotUtilities;

public static class TweenExtensions
{
    private const string PROPERTY_SCALE = "scale";
    private const string PROPERTY_POSITION = "position";
    private const string PROPERTY_GLOBAL_POSITION = "global_position";
    private const string PROPERTY_SHADER = "shader_parameter/{0}";

    #region Helpers

    public static Tween Linear(this Tween tween) => tween.SetTrans(Tween.TransitionType.Linear);
    public static Tween Sine(this Tween tween) => tween.SetTrans(Tween.TransitionType.Sine);
    public static Tween Back(this Tween tween) => tween.SetTrans(Tween.TransitionType.Back);
    public static Tween Bounce(this Tween tween) => tween.SetTrans(Tween.TransitionType.Bounce);
    public static Tween Circ(this Tween tween) => tween.SetTrans(Tween.TransitionType.Circ);
    public static Tween Spring(this Tween tween) => tween.SetTrans(Tween.TransitionType.Spring);
    public static Tween Quad(this Tween tween) => tween.SetTrans(Tween.TransitionType.Quad);
    public static Tween Quart(this Tween tween) => tween.SetTrans(Tween.TransitionType.Quart);
    public static Tween Expo(this Tween tween) => tween.SetTrans(Tween.TransitionType.Expo);
    public static Tween Quint(this Tween tween) => tween.SetTrans(Tween.TransitionType.Quint);
    public static Tween Elastic(this Tween tween) => tween.SetTrans(Tween.TransitionType.Elastic);
    public static Tween Cubic(this Tween tween) => tween.SetTrans(Tween.TransitionType.Cubic);

    public static Tween EaseIn(this Tween tween) => tween.SetEase(Tween.EaseType.In);
    public static Tween EaseOut(this Tween tween) => tween.SetEase(Tween.EaseType.Out);
    public static Tween EaseOutIn(this Tween tween) => tween.SetEase(Tween.EaseType.OutIn);
    public static Tween EaseInOut(this Tween tween) => tween.SetEase(Tween.EaseType.InOut);

    public static PropertyTweener Linear(this PropertyTweener t) => t.SetTrans(Tween.TransitionType.Linear);
    public static PropertyTweener Sine(this PropertyTweener t) => t.SetTrans(Tween.TransitionType.Sine);
    public static PropertyTweener Back(this PropertyTweener t) => t.SetTrans(Tween.TransitionType.Back);
    public static PropertyTweener Bounce(this PropertyTweener t) => t.SetTrans(Tween.TransitionType.Bounce);
    public static PropertyTweener Circ(this PropertyTweener t) => t.SetTrans(Tween.TransitionType.Circ);
    public static PropertyTweener Spring(this PropertyTweener t) => t.SetTrans(Tween.TransitionType.Spring);
    public static PropertyTweener Quad(this PropertyTweener t) => t.SetTrans(Tween.TransitionType.Quad);
    public static PropertyTweener Quart(this PropertyTweener t) => t.SetTrans(Tween.TransitionType.Quart);
    public static PropertyTweener Expo(this PropertyTweener t) => t.SetTrans(Tween.TransitionType.Expo);
    public static PropertyTweener Quint(this PropertyTweener t) => t.SetTrans(Tween.TransitionType.Quint);
    public static PropertyTweener Elastic(this PropertyTweener t) => t.SetTrans(Tween.TransitionType.Elastic);
    public static PropertyTweener Cubic(this PropertyTweener t) => t.SetTrans(Tween.TransitionType.Cubic);

    public static PropertyTweener EaseIn(this PropertyTweener t) => t.SetEase(Tween.EaseType.In);
    public static PropertyTweener EaseOut(this PropertyTweener t) => t.SetEase(Tween.EaseType.Out);
    public static PropertyTweener EaseOutIn(this PropertyTweener t) => t.SetEase(Tween.EaseType.OutIn);
    public static PropertyTweener EaseInOut(this PropertyTweener t) => t.SetEase(Tween.EaseType.InOut);

    public static SignalAwaiter WaitToFinish(this Tween tween) => tween.ToSignal(tween, Tween.SignalName.Finished);
    public static SignalAwaiter WaitToFinish(this Tweener tween) => tween.ToSignal(tween, Tweener.SignalName.Finished);

    public static void KillIfValid(this Tween tween)
    {
        if (!tween.IsNullOrInvalid())
            tween.Kill();
    }

    public static Tween Rebuild(this Tween tween, Node node)
    {
        tween.KillIfValid();
        tween = node.CreateTween();
        return tween;
    }

    public static PropertyTweener SetCurveInterpolator(this PropertyTweener tweener, Curve curve) =>
        tweener.SetCustomInterpolator(Callable.From<float, float>(curve.SampleBaked));
    
    #endregion

    #region Main
    
    public static CallbackTweener TweenAction(this Tween tween, Action action) =>
        tween.TweenCallback(Callable.From(action));

    public static MethodTweener TweenMethod<[MustBeVariant] T>(this Tween tween, Action<T> action, T from, T to, double duration) =>
        tween.TweenMethod(Callable.From(action), Variant.From(from), Variant.From(to), duration);

    public static PropertyTweener TweenShader(this Tween tween, ShaderMaterial material, string paramName, Variant value, double duration) =>
        tween.TweenProperty(material, string.Format(PROPERTY_SHADER, paramName), value, duration);

    public static PropertyTweener TweenPosition(this Tween tween, GodotObject target, Variant to, double duration) =>
        tween.TweenProperty(target, PROPERTY_POSITION, to, duration);

    public static PropertyTweener TweenGlobalPosition(this Tween tween, GodotObject target, Variant to, double duration) =>
        tween.TweenProperty(target, PROPERTY_GLOBAL_POSITION, to, duration);

    public static PropertyTweener TweenScale(this Tween tween, GodotObject target, Vector2 value, double duration) =>
        tween.TweenProperty(target, PROPERTY_SCALE, value, duration);

    #endregion
}
