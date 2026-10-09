using Godot;

namespace GodotUtilities;

public static class TweenExtension
{
    #region Properties

    private static readonly NodePath PropertyColor = "color";
    private static readonly NodePath PropertyScale = "scale";
    private static readonly NodePath PropertyModulate = "modulate";
    private static readonly NodePath PropertyPosition = "position";
    private static readonly NodePath PropertyRotation = "rotation";
    private static readonly NodePath PropertySelfModulate = "self_modulate";
    private static readonly NodePath PropertyGlobalPosition = "global_position";
    private static readonly NodePath PropertyRotationDegrees = "rotation_degrees";

    private static readonly NodePath PropertyOffsetTransformPos = "offset_transform_position";
    private static readonly NodePath PropertyOffsetTransformRotation = "offset_transform_rotation";
    private static readonly NodePath PropertyOffsetTransformScale = "offset_transform_scale";
    private static readonly NodePath PropertyOffsetTransformPosRatio = "offset_transform_position_ratio";

    private static readonly NodePath PropertyModulateAlpha = "modulate:a";
    private static readonly NodePath PropertySelfModulateAlpha = "self_modulate:a";

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

    #region Additional

    public static CallbackTweener TweenAction(this Tween tween, Action action) =>
        tween.TweenCallback(Callable.From(action));

    public static MethodTweener TweenMethod<[MustBeVariant] T>(this Tween tween, Action<T> action, T from, T to, double duration) =>
        tween.TweenMethod(Callable.From(action), Variant.From(from), Variant.From(to), duration);

    public static PropertyTweener TweenShader(this Tween tween, ShaderMaterial material, string paramName, Variant value, double duration) =>
        tween.TweenProperty(material, $"shader_parameter/{paramName}", value, duration);

    #endregion

    #region Transform

    public static PropertyTweener TweenPosition(this Tween tween, GodotObject target, Variant to, double duration) =>
        tween.TweenProperty(target, PropertyPosition, to, duration);

    public static PropertyTweener TweenGlobalPosition(this Tween tween, GodotObject target, Variant to, double duration) =>
        tween.TweenProperty(target, PropertyGlobalPosition, to, duration);

    public static PropertyTweener TweenScale(this Tween tween, GodotObject target, Variant value, double duration) =>
        tween.TweenProperty(target, PropertyScale, value, duration);

    public static PropertyTweener TweenRotation(this Tween tween, GodotObject target, Variant value, double duration) =>
        tween.TweenProperty(target, PropertyRotation, value, duration);

    public static PropertyTweener TweenRotationDegrees(this Tween tween, GodotObject target, Variant value, double duration) =>
        tween.TweenProperty(target, PropertyRotationDegrees, value, duration);

    #endregion

    #region Offset Transform

    public static PropertyTweener TweenOffsetPosition(this Tween tween, Control control, Vector2 value, double duration) =>
        tween.TweenProperty(control, PropertyOffsetTransformPos, value, duration);
        
    public static PropertyTweener TweenOffsetPositionRatio(this Tween tween, Control control, Vector2 value, double duration) =>
        tween.TweenProperty(control, PropertyOffsetTransformPosRatio, value, duration);

    public static PropertyTweener TweenOffsetScale(this Tween tween, Control control, Vector2 value, double duration) =>
        tween.TweenProperty(control, PropertyOffsetTransformScale, value, duration);

    public static PropertyTweener TweenOffsetRotation(this Tween tween, Control control, float value, double duration) =>
        tween.TweenProperty(control, PropertyOffsetTransformRotation, value, duration);

    #endregion

    #region Colors

    public static PropertyTweener TweenColor(this Tween tween, GodotObject target, Color value, double duration) =>
        tween.TweenProperty(target, PropertyColor, value, duration);

    public static PropertyTweener TweenModulate(this Tween tween, CanvasItem item, Color color, double duration) =>
        tween.TweenProperty(item, PropertyModulate, color, duration);

    public static PropertyTweener TweenSelfModulate(this Tween tween, CanvasItem item, Color color, double duration) =>
        tween.TweenProperty(item, PropertySelfModulate, color, duration);

    public static PropertyTweener TweenModulateAlpha(this Tween tween, CanvasItem item, float alpha, double duration) =>
        tween.TweenProperty(item, PropertyModulateAlpha, alpha, duration);

    public static PropertyTweener TweenSelfModulateAlpha(this Tween tween, CanvasItem item, float alpha, double duration) =>
        tween.TweenProperty(item, PropertySelfModulateAlpha, alpha, duration);

    #endregion
}
