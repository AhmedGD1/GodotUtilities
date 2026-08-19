using Godot;

namespace GodotUtilities;

public static class PropertyTweenerExtension
{
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

    public static PropertyTweener SetCurveInterpolator(this PropertyTweener tweener, Curve curve) =>
        tweener.SetCustomInterpolator(Callable.From<float, float>(curve.SampleBaked));
}
