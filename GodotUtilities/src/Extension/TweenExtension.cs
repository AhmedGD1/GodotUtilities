using Godot;

namespace GodotUtilities;

public static class TweenExtension
{
    #region Properties
    
    private const string PROPERTY_SCALE = "scale";
    private const string PROPERTY_SHADER = "shader_parameter/{0}";
    private const string PROPERTY_POSITION = "position";
    private const string PROPERTY_ROTATION = "rotation";
    private const string PROPERTY_GLOBAL_POSITION = "global_position";
    private const string PROPERTY_ROTATION_DEGREES = "rotation_degrees";

    #endregion

    #region Helpers

    public static Tween Sine(this Tween tween) => tween.SetTrans(Tween.TransitionType.Sine);
    public static Tween Back(this Tween tween) => tween.SetTrans(Tween.TransitionType.Back);
    public static Tween Circ(this Tween tween) => tween.SetTrans(Tween.TransitionType.Circ);
    public static Tween Quad(this Tween tween) => tween.SetTrans(Tween.TransitionType.Quad);
    public static Tween Expo(this Tween tween) => tween.SetTrans(Tween.TransitionType.Expo);
    public static Tween Cubic(this Tween tween) => tween.SetTrans(Tween.TransitionType.Cubic);
    public static Tween Quint(this Tween tween) => tween.SetTrans(Tween.TransitionType.Quint);
    public static Tween Quart(this Tween tween) => tween.SetTrans(Tween.TransitionType.Quart);
    public static Tween Bounce(this Tween tween) => tween.SetTrans(Tween.TransitionType.Bounce);
    public static Tween Linear(this Tween tween) => tween.SetTrans(Tween.TransitionType.Linear);
    public static Tween Spring(this Tween tween) => tween.SetTrans(Tween.TransitionType.Spring);
    public static Tween Elastic(this Tween tween) => tween.SetTrans(Tween.TransitionType.Elastic);

    public static Tween EaseIn(this Tween tween) => tween.SetEase(Tween.EaseType.In);
    public static Tween EaseOut(this Tween tween) => tween.SetEase(Tween.EaseType.Out);
    public static Tween EaseOutIn(this Tween tween) => tween.SetEase(Tween.EaseType.OutIn);
    public static Tween EaseInOut(this Tween tween) => tween.SetEase(Tween.EaseType.InOut);

    public static SignalAwaiter WaitToFinish(this Tween tween) => tween.ToSignal(tween, Tween.SignalName.Finished);
    public static SignalAwaiter WaitToFinish(this Tweener tween) => tween.ToSignal(tween, Tweener.SignalName.Finished);

    public static void KillIfValid(this Tween tween)
    {
        if (!tween.IsNullOrInvalid())
            tween.Kill();
    }

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

    public static PropertyTweener TweenScale(this Tween tween, GodotObject target, Variant value, double duration) =>
        tween.TweenProperty(target, PROPERTY_SCALE, value, duration);

    public static PropertyTweener TweenRotation(this Tween tween, GodotObject target, Variant value, double duration) =>
        tween.TweenProperty(target, PROPERTY_ROTATION, value, duration);

    public static PropertyTweener TweenRotationDegrees(this Tween tween, GodotObject target, Variant value, double duration) =>
        tween.TweenProperty(target, PROPERTY_ROTATION_DEGREES, value, duration);

    #endregion
}
